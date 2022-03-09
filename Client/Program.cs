using System.Diagnostics;
using System.Net;

var allTimings = new Dictionary<string, List<TimeSpan>>();

foreach (var proxyVersion in new[] { HttpVersion.Version20 })
//foreach (var proxyVersion in new[] { HttpVersion.Version11, HttpVersion.Version20 })
{
    foreach (var backendVersion in new[] { HttpVersion.Version11 })
    //foreach (var backendVersion in new[] { HttpVersion.Version11, HttpVersion.Version20 })
    {
        for (var bodySize = 1024 * 1024 * 4; bodySize <= 1024 * 1024 * 8; bodySize *= 2)
        {
            var timingKey = $"{proxyVersion}-{backendVersion} {bodySize / 1024,5} kB";
            Console.WriteLine(timingKey);

            var body = new byte[bodySize];
            Random.Shared.NextBytes(body);

            var timings = new List<TimeSpan>();
            allTimings.Add(timingKey, timings);

            for (int retry = 1; retry <= 100; retry++)
            {
                using var handler = new SocketsHttpHandler();
                handler.UseCookies = false;
                handler.SslOptions.RemoteCertificateValidationCallback = delegate { return true; };

                using var client = new HttpClient(handler);

                const string Proxy = "https://10.0.0.4";

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

                start.Stop();

                timings.Add(start.Elapsed);

                //Console.WriteLine($"{retry} - Sent {bodySize / 1024} kB in {start.ElapsedMilliseconds:N2} ms (HTTP {proxyVersion}-{backendVersion})");
                //Console.WriteLine(responseString);
            }

            //Console.WriteLine();
        }
    }
}

foreach (var (key, timings) in allTimings)
{
    timings.Sort();

    var percentiles = new[] { 50, 75, 90, 95 };

    Console.WriteLine($"{key}: {string.Join(' ', percentiles.Select(p => $"P{p}={(int)timings[(int)(timings.Count * (p / 100.0))].TotalMilliseconds,-5}"))}");
}