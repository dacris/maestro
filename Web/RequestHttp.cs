using System.Net;
using System.Text;

namespace Dacris.Maestro.Web
{
    public class RequestHttp : Interaction
    {
        private static readonly HttpClientHandler _httpClientHandler = new HttpClientHandler() { CookieContainer = new CookieContainer() };
        private static readonly HttpClient _httpClient = new HttpClient(_httpClientHandler) { Timeout = Timeout.InfiniteTimeSpan };

        public override void Specify()
        {
            Description = "Sends an HTTP request.";
            InputSpec.AddInputs("url", "method", "content", "header", "bodyPath", "bodyFile", "outputFile");
            InputSpec.StateObjectKey("content")
                .ValueSpec = new ValueSpec
                {
                    ValueType = ValueTypeSpec.Object,
                    ObjectSpecs = [
                        new InputSpec("header")
                    ]
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
            var url = InputState!["url"]!.ToString();
            if (AppState.Instance.IsMock())
            {
                File.Copy(url.Substring(5), InputState!["outputFile"]!.ToString(), true);
                return;
            }
            var method = InputState!["method"]!.ToString();
            using var request = new HttpRequestMessage(HttpMethod.Parse(method), url);
            request.Headers.Clear();
            InputState!["header"]?
                .Where(x => x["key"]!.ToString() != "Content-Type")
                .ToList()
                .ForEach(x =>
                { request.Headers.Add(x["key"]!.ToString(), x["value"]!.ToString()); }
            );
            var bodyPath = InputState!["bodyPath"]?.ToString();
            var bodyFile = InputState!["bodyFile"]?.ToString();
            var innerMultipartContent = (StreamContent?)null;
            using var infs = bodyFile is not null ? File.OpenRead(bodyFile) : (Stream?)null;
            bool multipart = InputState["header"]!.SingleOrDefault(
                            x => x["key"]!.ToString() == "Content-Type")?["value"]?.ToString()
                            == "multipart/form-data";
            if (bodyPath is not null)
            {
                var token = AppState.Instance.StateObject.SelectToken(bodyPath)!;
                request.Content = new StringContent(token.ToString());
            }
            if (infs is not null && !multipart)
            {
                request.Content = new StreamContent(infs);
            }
            else if (infs is not null && multipart)
            {
                request.Content = new MultipartFormDataContent(DateTime.UtcNow.ToFileTime().GetHashCode().ToString());
                innerMultipartContent = new StreamContent(infs);
                ((MultipartFormDataContent)request.Content).Add(innerMultipartContent);
            }
            if (!multipart)
            {
                request.Content?.Headers.Clear();
            }
            innerMultipartContent?.Headers.Remove("Content-Type");
            innerMultipartContent?.Headers.Remove("Content-Disposition");
            if (request.Content is not null && InputState["header"] is not null)
            {
                var token = InputState["header"]!.SingleOrDefault(
                            x => x["key"]!.ToString() == "Content-Type")?["value"];
                if (token is not null && token.ToString() != "multipart/form-data")
                {
                    request.Content.Headers.ContentType = 
                        new System.Net.Http.Headers.MediaTypeHeaderValue(token.ToString());
                }
            }
            if (InputState!["content"]?["header"] is not null && (innerMultipartContent ?? request.Content) is not null)
            {
                InputState!["content"]!["header"]?
                    .ToList()
                    .ForEach(x =>
                    { (innerMultipartContent ?? request.Content)?.Headers.Add(x["key"]!.ToString(), x["value"]!.ToString()); }
                );
            }
            try
            {
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
            finally
            {
                innerMultipartContent?.Dispose();
                request.Content?.Dispose();
            }
        }

        public static async Task<string> GetRawString(HttpRequestMessage request)
        {
            var sb = new StringBuilder();

            var line1 = $"{request.Method} {request.RequestUri} HTTP/{request.Version}";
            sb.AppendLine(line1);

            foreach (var (key, value) in request.Headers)
            foreach (var val in value)
            {
                var header = $"{key}: {val}";
                sb.AppendLine(header);
            }

            if (request.Content?.Headers != null)
            {
                foreach (var (key, value) in request.Content.Headers)
                foreach (var val in value)
                {
                    var header = $"{key}: {val}";
                    sb.AppendLine(header);
                }
            }
            sb.AppendLine();

            var body = await (request.Content?.ReadAsStringAsync() ?? Task.FromResult<string>(string.Empty));
            if (!string.IsNullOrWhiteSpace(body))
                sb.AppendLine(body);

            return sb.ToString();
        }
    }
}
