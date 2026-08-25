namespace AIstudentskillexchange.Services.AI
{
    /// <summary>
    /// Settings for the Gemini LLM calls made by the AI Service.
    ///
    /// The API key comes from the Google AI Studio free tier. Never commit it:
    /// put it in user secrets or an environment variable instead, e.g.
    ///   dotnet user-secrets set "Gemini:ApiKey" "YOUR_KEY"
    /// or set the env var  Gemini__ApiKey=YOUR_KEY
    /// </summary>
    public class GeminiOptions
    {
        public const string SectionName = "Gemini";

        /// <summary>Free-tier API key from https://aistudio.google.com/apikey </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>Free-tier friendly model. Flash models have the highest free quota.</summary>
        public string Model { get; set; } = "gemini-2.0-flash";

        public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta";

        /// <summary>Master switch. When false the module runs entirely on the offline fallback.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>Seconds to wait for the LLM before falling back to the offline analyser.</summary>
        public int TimeoutSeconds { get; set; } = 15;

        /// <summary>
        /// How long an analysis result is cached. Free-tier quota is limited, so
        /// results are reused rather than re-requested on every page load.
        /// </summary>
        public int CacheMinutes { get; set; } = 60;

        /// <summary>Skills sent to the model in one analysis call (keeps the prompt small).</summary>
        public int MaxCatalogSkills { get; set; } = 60;

        /// <summary>Ask the LLM to write the explanation text for the top N matches.</summary>
        public bool GenerateMatchExplanations { get; set; } = true;

        /// <summary>How many top matches get an LLM-written explanation.</summary>
        public int ExplanationCount { get; set; } = 5;

        public bool IsConfigured => Enabled && !string.IsNullOrWhiteSpace(ApiKey);
    }
}
