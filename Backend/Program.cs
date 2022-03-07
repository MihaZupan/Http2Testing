using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.Http2.InitialStreamWindowSize *= 8;
    options.Limits.Http2.InitialConnectionWindowSize = 16 * 1024 * 1024;
});

var app = builder.Build();

app.Map("/", async (HttpContext context) =>
{
    var start = Stopwatch.StartNew();

    var buffer = new byte[4096];
    var totalRead = 0;
    var read = 0;
    var body = context.Request.Body;
    while ((read = await body.ReadAsync(buffer)) > 0) totalRead += read;

    return new ResponseModel(
        context.Request.Method,
        context.Request.Path,
        context.Request.Headers,
        $"Received {totalRead} bytes in {start.ElapsedMilliseconds:N2} ms");
});

app.Run();

record ResponseModel(string Method, string Path, IHeaderDictionary Headers, string Message);