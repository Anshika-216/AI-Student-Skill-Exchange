namespace AIstudentskillexchange.Services.AI
{
    public class GeminiOptions
    {
        public const string SectionName = "Gemini";

        public string ApiKey { get; set; } = string.Empty;

        public string Model { get; set; } = "gemini-flash-lite-latest";

        public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta";

        public bool Enabled { get; set; } = true;

        public int TimeoutSeconds { get; set; } = 30;

        public int CacheMinutes { get; set; } = 60;

        public int MaxCatalogSkills { get; set; } = 60;

        public bool GenerateMatchExplanations { get; set; } = true;

        public int ExplanationCount { get; set; } = 5;

        public bool IsConfigured => Enabled && !string.IsNullOrWhiteSpace(ApiKey);
    }
}
