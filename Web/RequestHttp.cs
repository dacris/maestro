using System.Net;

namespace Dacris.Maestro.Web
{
    public class RequestHttp : Interaction
    {
        private static readonly HttpClientHandler _httpClientHandler = new HttpClientHandler() { CookieContainer = new CookieContainer() };
        private static readonly HttpClient _httpClient = new HttpClient(_httpClientHandler) { Timeout = Timeout.InfiniteTimeSpan };

        public override void Specify()
        {
            InputSpec.AddInputs("url", "method", "contentHeaders", "header", "bodyPath", "bodyFile", "outputFile");
            InputSpec.StateObjectKey("contentHeaders")
                .ValueSpec = new ValueSpec
                {
                    ValueType = ValueTypeSpec.Array,
                    InnerSpec = new ValueSpec
                    {
                        ValueType = ValueTypeSpec.String
                    }
                };
            InputSpec.AddTimeout();
            InputSpec.AddRetry();
        }

        public override async Task RunAsync()
        {
            await RetryAsync(RunHttpRequestAsync, (object?)null);
        }

        private async Task RunHttpRequestAsync(object? param1)
        {
            var contentHeaders = new[] { "Content-Type" };
            var url = InputState!["url"]!.ToString();
            if (AppState.Instance.IsMock())
            {
                File.Copy(url.Substring(5), InputState!["outputFile"]!.ToString(), true);
                return;
            }
            var method = InputState!["method"]!.ToString();
            using var request = new HttpRequestMessage(HttpMethod.Parse(method), url);
            request.Headers.Clear();
            var contentHeadersFromJson = InputState!["contentHeaders"];
            if (contentHeadersFromJson is not null)
            {
                contentHeaders = contentHeadersFromJson.Select(x => x.ToString()).ToArray();
            }
            InputState!["header"]?
                .Where(x => !contentHeaders.Contains(x["key"]!.ToString()))
                .ToList()
                .ForEach(x =>
                { request.Headers.Add(x["key"]!.ToString(), x["value"]!.ToString()); }
            );
            var bodyPath = InputState!["bodyPath"]?.ToString();
            var bodyFile = InputState!["bodyFile"]?.ToString();
            using var infs = bodyFile is not null ? File.OpenRead(bodyFile) : (Stream?)null;
            if (bodyPath is not null)
            {
                var token = AppState.Instance.StateObject.SelectToken(bodyPath)!;
                request.Content = new StringContent(token.ToString());
            }
            if (infs is not null)
            {
                request.Content = new StreamContent(infs);
            }
            request.Content?.Headers.Clear();
            if (InputState!["header"] is not null && request.Content is not null)
            {
                InputState!["header"]?
                    .Where(x => contentHeaders.Contains(x["key"]!.ToString()))
                    .ToList()
                    .ForEach(x =>
                    { request.Content.Headers.Add(x["key"]!.ToString(), x["value"]!.ToString()); }
                );
            }

            using var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromMilliseconds(Math.Max(30000, double.Parse(InputState!["timeout"]?.ToString() ?? "60000"))));
            using var response = await _httpClient.SendAsync(request, cts.Token);

            response.EnsureSuccessStatusCode();
            using var stream = await response.Content.ReadAsStreamAsync();
            string outputFile = InputState!["outputFile"]!.ToString();
            if (File.Exists(outputFile))
                File.Delete(outputFile);
            using var fs = File.OpenWrite(outputFile);
            await stream.CopyToAsync(fs);
        }
    }
}
