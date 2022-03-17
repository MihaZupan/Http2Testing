var builder = WebApplication.CreateBuilder(args);

//builder.WebHost.ConfigureKestrel(options =>
//{
//    options.Limits.MaxRequestBodySize = 1024 * 1024 * 1024 * 5L; // 5 GB
//    options.Limits.Http2.InitialStreamWindowSize = 1 * 1024 * 1024;
//    options.Limits.Http2.InitialConnectionWindowSize = 1 * 1024 * 1024;
//});

var app = builder.Build();

app.Map("/", (HttpContext context) =>
{
    return context.Request.Path;
});

app.Run();

record ResponseModel(string Method, string Path, IHeaderDictionary Headers, string Message);