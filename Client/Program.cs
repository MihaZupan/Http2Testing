using System.Diagnostics;
using System.Net;

var allTimings = new Dictionary<string, List<TimeSpan>>();

foreach (var proxyVersion in new[] { HttpVersion.Version20 })
//foreach (var proxyVersion in new[] { HttpVersion.Version11, HttpVersion.Version20 })
{
    foreach (var backendVersion in new[] { HttpVersion.Version11 })
    //foreach (var backendVersion in new[] { HttpVersion.Version11, HttpVersion.Version20 })
    {
        for (var bodySize = 1024 * 1024; bodySize <= 1024 * 1024 * 32; bodySize *= 2)
        {
            var timingKey = $"{proxyVersion}-{backendVersion} {bodySize / 1024} kB";
            Console.WriteLine(timingKey);

            var body = new byte[bodySize];
            Random.Shared.NextBytes(body);

            var timings = new List<TimeSpan>();
            allTimings.Add(timingKey, timings);

            for (int retry = 1; retry <= 50; retry++)
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

    var p50 = (int)timings[(int)(timings.Count * 0.50)].TotalMilliseconds;
    var p70 = (int)timings[(int)(timings.Count * 0.70)].TotalMilliseconds;
    var p90 = (int)timings[(int)(timings.Count * 0.90)].TotalMilliseconds;

    Console.WriteLine($"{key,20}: P50={p50,3} P70={p70,3} P90={p90,3}");
}