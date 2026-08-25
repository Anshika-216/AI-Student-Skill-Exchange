using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace AIstudentskillexchange.Services.AI
{
    /// <summary>
    /// Thin wrapper over the Gemini generateContent REST endpoint.
    ///
    /// Uses the Google AI Studio free tier. The model is asked to reply with
    /// JSON only (responseMimeType = application/json) so the result can be
    /// deserialised straight into our own types.
    /// </summary>
    public class GeminiClient
    {
        private readonly HttpClient _http;
        private readonly GeminiOptions _options;
        private readonly ILogger<GeminiClient> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public GeminiClient(HttpClient http, IOptions<GeminiOptions> options, ILogger<GeminiClient> logger)
        {
            _http = http;
            _options = options.Value;
            _logger = logger;
        }

        public bool IsConfigured => _options.IsConfigured;

        /// <summary>
        /// Sends a prompt and returns the raw JSON text the model produced,
        /// or null if the call failed for any reason.
        /// </summary>
        public async Task<string?> GenerateJsonAsync(
            string systemInstruction,
            string prompt,
            CancellationToken cancellationToken = default)
        {
            if (!_options.IsConfigured)
                return null;

            var payload = new
            {
                system_instruction = new
                {
                    parts = new[] { new { text = systemInstruction } }
                },
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[] { new { text = prompt } }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.2,
                    responseMimeType = "application/json"
                }
            };

            var url = $"{_options.BaseUrl.TrimEnd('/')}/models/{_options.Model}:generateContent";

            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, _options.TimeoutSeconds)));

                using var message = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(payload),
                        Encoding.UTF8,
                        "application/json")
                };
                message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                message.Headers.Add("x-goog-api-key", _options.ApiKey);

                using var response = await _http.SendAsync(message, timeout.Token);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync(timeout.Token);
                    _logger.LogWarning(
                        "Gemini call failed with {StatusCode}. Falling back to offline analysis. {Error}",
                        (int)response.StatusCode, Truncate(error, 400));
                    return null;
                }

                var body = await response.Content.ReadAsStringAsync(timeout.Token);
                return ExtractText(body);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Gemini call timed out after {Seconds}s. Falling back to offline analysis.",
                    _options.TimeoutSeconds);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gemini call threw. Falling back to offline analysis.");
                return null;
            }
        }

        /// <summary>
        /// Digs the generated text out of the generateContent response envelope.
        /// </summary>
        private static string? ExtractText(string responseBody)
        {
            using var document = JsonDocument.Parse(responseBody);

            if (!document.RootElement.TryGetProperty("candidates", out var candidates)
                || candidates.GetArrayLength() == 0)
            {
                return null;
            }

            var first = candidates[0];
            if (!first.TryGetProperty("content", out var content)
                || !content.TryGetProperty("parts", out var parts))
            {
                return null;
            }

            var builder = new StringBuilder();
            foreach (var part in parts.EnumerateArray())
            {
                if (part.TryGetProperty("text", out var text))
                    builder.Append(text.GetString());
            }

            var result = builder.ToString().Trim();
            return string.IsNullOrWhiteSpace(result) ? null : result;
        }

        /// <summary>
        /// Deserialises a model reply, tolerating a stray markdown code fence.
        /// </summary>
        public static T? ParseJson<T>(string? raw, ILogger logger) where T : class
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            var cleaned = raw.Trim();
            if (cleaned.StartsWith("```"))
            {
                var firstNewline = cleaned.IndexOf('\n');
                if (firstNewline >= 0)
                    cleaned = cleaned[(firstNewline + 1)..];
                if (cleaned.EndsWith("```"))
                    cleaned = cleaned[..^3];
                cleaned = cleaned.Trim();
            }

            try
            {
                return JsonSerializer.Deserialize<T>(cleaned, JsonOptions);
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "Could not parse the model reply as JSON: {Snippet}", Truncate(cleaned, 300));
                return null;
            }
        }

        private static string Truncate(string value, int max) =>
            value.Length <= max ? value : value[..max] + "...";
    }
}
