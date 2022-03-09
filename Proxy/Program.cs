using System.Net;
using Yarp.ReverseProxy.Forwarder;
using Yarp.Telemetry.Consumption;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 1024 * 1024 * 1024 * 5L; // 5 GB
    options.Limits.Http2.InitialStreamWindowSize = 1 * 1024 * 1024;
    options.Limits.Http2.InitialConnectionWindowSize = 1 * 1024 * 1024;
});

builder.Services.AddReverseProxy();
builder.Services.AddTelemetryConsumer<RequestContentTelemetry>();

var app = builder.Build();

var handler = new SocketsHttpHandler()
{
    UseProxy = false,
    AllowAutoRedirect = false,
    AutomaticDecompression = DecompressionMethods.None,
    UseCookies = false,
    ActivityHeadersPropagator = null
};
handler.SslOptions.RemoteCertificateValidationCallback = delegate { return true; };

var httpClient = new HttpMessageInvoker(handler);

app.Map("/", async (HttpContext context, IHttpForwarder forwarder) =>
{
    if (!context.Request.Headers.TryGetValue("X-Backend-Http-Version", out var version) || !Version.TryParse(version, out var httpVersion))
    {
        await context.Response.WriteAsync("Specify X-Backend-Http-Version");
        return;
    }

    var config = new ForwarderRequestConfig
    {
        Version = httpVersion,
        VersionPolicy = HttpVersionPolicy.RequestVersionExact
    };

    const string Backend = "https://10.2.0.4";

    var error = await forwarder.SendAsync(context, Backend, httpClient, config);

    if (error != ForwarderError.None && !context.Response.HasStarted)
    {
        var errorFeature = context.GetForwarderErrorFeature();
        var errorMessage = $"{errorFeature?.Error}: {errorFeature?.Exception}";
        Console.WriteLine(errorMessage);
        await context.Response.WriteAsync(errorMessage);
    }
});

app.Run();

public sealed class RequestContentTelemetry : IForwarderTelemetryConsumer
{
    private readonly ILogger<RequestContentTelemetry> _logger;

    public RequestContentTelemetry(ILogger<RequestContentTelemetry> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void OnContentTransferred(DateTime timestamp, bool isRequest, long contentLength, long iops, TimeSpan readTime, TimeSpan writeTime, TimeSpan firstReadTime)
    {
        if (isRequest)
        {
            _logRequestContentTransferred(_logger, contentLength, iops, readTime, writeTime, firstReadTime, null);
        }
    }

    private static readonly Action<ILogger, long, long, TimeSpan, TimeSpan, TimeSpan, Exception?> _logRequestContentTransferred = LoggerMessage.Define<long, long, TimeSpan, TimeSpan, TimeSpan>(
        LogLevel.Information,
        new EventId(1, "RequestContentTransferred"),
        "Transferred {contentLength} request bytes in {iops} iops (readTime = {readTime}, writeTime = {writeMs}, firstReadTime = {firstReadTime})");
}
