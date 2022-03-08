using System.Diagnostics;
using System.Net;

var handler = new SocketsHttpHandler();
handler.UseCookies = false;
handler.SslOptions.RemoteCertificateValidationCallback = delegate { return true; };

var client = new HttpClient(handler);

foreach (var proxyVersion in new[] { HttpVersion.Version11, HttpVersion.Version20 })
{
    foreach (var backendVersion in new[] { HttpVersion.Version11, HttpVersion.Version20 })
    {
        for (var bodySize = 1024 * 256; bodySize <= 1024 * 1024 * 16; bodySize *= 2)
        {
            var body = new byte[bodySize];
            Random.Shared.NextBytes(body);

            for (int retry = 1; retry <= 2; retry++)
            {
                const string Proxy = "https://20.223.132.213";

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    Content = new ByteArrayContent(body),
                    Version = proxyVersion,
                    VersionPolicy = HttpVersionPolicy.RequestVersionExact,
                    RequestUri = new Uri(Proxy)
                };

                request.Headers.Add("X-Backend-Http-Version", backendVersion.ToString());

                var start = Stopwatch.StartNew();

                using var response = await client.SendAsync(request);

                string responseString = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"{retry} - Sent {bodySize / 1024} kB in {start.ElapsedMilliseconds:N2} ms (HTTP {proxyVersion}-{backendVersion})");
                //Console.WriteLine(responseString);
            }

            Console.WriteLine();
        }
    }
}