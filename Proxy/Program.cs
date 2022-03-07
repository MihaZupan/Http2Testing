using System.Net;
using Yarp.ReverseProxy.Forwarder;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy();

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
        await context.Response.WriteAsync("Specify X-Http-Version");
        return;
    }

    var config = new ForwarderRequestConfig
    {
        Version = httpVersion,
        VersionPolicy = HttpVersionPolicy.RequestVersionExact
    };

    const string Backend = "https://51.12.208.10";
    //const string Backend = "https://localhost:5000";

    var error = await forwarder.SendAsync(context, Backend, httpClient, config);

    if (error != ForwarderError.None && !context.Response.HasStarted)
    {
        var errorFeature = context.GetForwarderErrorFeature();

        await context.Response.WriteAsync($"{errorFeature?.Error}: {errorFeature?.Exception}");
    }
});

app.Run();
