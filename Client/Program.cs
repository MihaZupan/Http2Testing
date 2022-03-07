using System.Diagnostics;
using System.Net;

var handler = new SocketsHttpHandler();
handler.UseCookies = false;
handler.SslOptions.RemoteCertificateValidationCallback = delegate { return true; };

handler.InitialHttp2StreamWindowSize *= 16;

var client = new HttpClient(handler);

foreach (var proxyVersion in new[] { HttpVersion.Version11, HttpVersion.Version20 })
    foreach (var backendVersion in new[] { HttpVersion.Version11, HttpVersion.Version20 })
    {
        for (var bodySize = 1024; bodySize <= 1024 * 1024 * 8; bodySize *= 2)
        {
            var body = new byte[bodySize];
            Random.Shared.NextBytes(body);

            for (int retry = 1; retry <= 2; retry++)
            {
                const string Proxy = "https://20.223.132.213";
                //const string Proxy = "https://localhost:5001";

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