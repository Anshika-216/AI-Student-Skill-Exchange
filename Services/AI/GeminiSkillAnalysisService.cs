using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace AIstudentskillexchange.Services.AI
{
    public class GeminiSkillAnalysisService : ISkillAnalysisService
    {
        private readonly GeminiClient _client;
        private readonly GeminiOptions _options;
        private readonly IMemoryCache _cache;
        private readonly ILogger<GeminiSkillAnalysisService> _logger;

        public GeminiSkillAnalysisService(
            GeminiClient client,
            IOptions<GeminiOptions> options,
            IMemoryCache cache,
            ILogger<GeminiSkillAnalysisService> logger)
        {
            _client = client;
            _options = options.Value;
            _cache = cache;
            _logger = logger;
        }

        public async Task<SkillAnalysisResult> AnalyseAsync(
            SkillAnalysisRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request.WantsToLearn.Count == 0)
                return new SkillAnalysisResult { FromLlm = false, FallbackReason = "No learning goals to analyse." };

            var cacheKey = BuildCacheKey(request);
            if (_cache.TryGetValue<SkillAnalysisResult>(cacheKey, out var cached) && cached != null)
                return cached;

            SkillAnalysisResult result;

            if (!_client.IsConfigured)
            {
                result = OfflineAnalyse(request, "Gemini API key not configured - using offline skill analysis.");
            }
            else
            {
                var raw = await _client.GenerateJsonAsync(
                    AnalysisSystemInstruction,
                    BuildAnalysisPrompt(request),
                    cancellationToken);

                var parsed = GeminiClient.ParseJson<LlmAnalysisResponse>(raw, _logger);

                result = parsed == null
                    ? OfflineAnalyse(request, "Gemini call failed or returned unusable JSON.")
                    : MapLlmResponse(parsed, request);
            }

            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(Math.Max(1, _options.CacheMinutes)));
            return result;
        }

        private const string AnalysisSystemInstruction =
            "You are the AI Service of a student peer-learning platform. You analyse the skill " +
            "descriptions students write, identify related or similar skills from a fixed catalogue, " +
            "and suggest a short learning path. You must reply with JSON only, no prose and no code " +
            "fences. Only ever use skillId values that appear in the provided catalogue. Never invent " +
            "a skill or an id.";

        private string BuildAnalysisPrompt(SkillAnalysisRequest request)
        {
            var builder = new StringBuilder();

            builder.AppendLine("A student wants to be matched with peer mentors.");
            builder.AppendLine();
            builder.AppendLine("SKILLS THE STUDENT WANTS TO LEARN:");
            foreach (var skill in request.WantsToLearn)
            {
                builder.AppendLine(
                    $"- skillId={skill.SkillId} | name={skill.Name} | category={skill.Category} | " +
                    $"currentLevel={skill.Level} | studentNote=\"{Clean(skill.Description)}\"");
            }

            if (request.CanTeach.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine("SKILLS THE STUDENT CAN ALREADY TEACH:");
                foreach (var skill in request.CanTeach)
                {
                    builder.AppendLine(
                        $"- skillId={skill.SkillId} | name={skill.Name} | category={skill.Category} | " +
                        $"level={skill.Level} | studentNote=\"{Clean(skill.Description)}\"");
                }
            }

            builder.AppendLine();
            builder.AppendLine("SKILL CATALOGUE (the only skills you may reference):");
            foreach (var skill in request.Catalog.Take(_options.MaxCatalogSkills))
            {
                builder.AppendLine(
                    $"- skillId={skill.SkillId} | name={skill.Name} | category={skill.Category} | " +
                    $"description=\"{Clean(skill.Description)}\"");
            }

            builder.AppendLine();
            builder.AppendLine("TASKS:");
            builder.AppendLine("1. For each skill the student wants to learn, list catalogue skills that are related or similar (same field, prerequisite, or commonly learned together). Exclude the wanted skill itself. Give each a similarity between 0 and 1 and a short reason.");
            builder.AppendLine("2. Extract up to 10 lowercase topic keywords from the student's notes.");
            builder.AppendLine("3. Suggest an ordered learning path of at most 5 steps toward the student's goals. Set skillId when a step maps to a catalogue skill, otherwise null.");
            builder.AppendLine();
            builder.AppendLine("Reply with exactly this JSON shape:");
            builder.AppendLine("""
            {
              "relatedSkills": [
                { "forSkillId": 1, "matches": [ { "skillId": 2, "similarity": 0.8, "reason": "..." } ] }
              ],
              "keywords": ["..."],
              "learningPath": [ { "order": 1, "title": "...", "detail": "...", "skillId": 2 } ]
            }
            """);

            return builder.ToString();
        }

        private SkillAnalysisResult MapLlmResponse(LlmAnalysisResponse response, SkillAnalysisRequest request)
        {
            var validIds = request.Catalog.Select(c => c.SkillId).ToHashSet();
            var names = request.Catalog.ToDictionary(c => c.SkillId, c => c.Name);
            var wantedIds = request.WantsToLearn.Select(w => w.SkillId).ToHashSet();

            var result = new SkillAnalysisResult { FromLlm = true };

            foreach (var group in response.RelatedSkills ?? new List<LlmRelatedGroup>())
            {
                if (!wantedIds.Contains(group.ForSkillId))
                    continue;

                var matches = new List<RelatedSkill>();
                foreach (var match in group.Matches ?? new List<LlmRelatedMatch>())
                {
                    if (!validIds.Contains(match.SkillId) || match.SkillId == group.ForSkillId)
                        continue;
                    if (wantedIds.Contains(match.SkillId))
                        continue;

                    matches.Add(new RelatedSkill
                    {
                        SkillId = match.SkillId,
                        SkillName = names.GetValueOrDefault(match.SkillId, $"Skill #{match.SkillId}"),
                        Similarity = Math.Clamp(match.Similarity, 0, 1),
                        Reason = string.IsNullOrWhiteSpace(match.Reason)
                            ? "Related to a skill on your learning list."
                            : match.Reason.Trim()
                    });
                }

                if (matches.Count > 0)
                {
                    result.RelatedSkills[group.ForSkillId] = matches
                        .OrderByDescending(m => m.Similarity)
                        .ToList();
                }
            }

            result.Keywords = (response.Keywords ?? new List<string>())
                .Where(k => !string.IsNullOrWhiteSpace(k))
                .Select(k => k.Trim().ToLowerInvariant())
                .Distinct()
                .Take(10)
                .ToList();

            result.LearningPath = (response.LearningPath ?? new List<LlmLearningStep>())
                .Where(s => !string.IsNullOrWhiteSpace(s.Title))
                .OrderBy(s => s.Order)
                .Take(5)
                .Select((s, index) => new LearningPathStep
                {
                    Order = index + 1,
                    Title = s.Title.Trim(),
                    Detail = s.Detail?.Trim() ?? string.Empty,
                    SkillId = s.SkillId.HasValue && validIds.Contains(s.SkillId.Value) ? s.SkillId : null
                })
                .ToList();

            if (result.RelatedSkills.Count == 0 && result.LearningPath.Count == 0)
                return OfflineAnalyse(request, "Gemini returned no usable matches.");

            return result;
        }

        private static SkillAnalysisResult OfflineAnalyse(SkillAnalysisRequest request, string reason)
        {
            var result = new SkillAnalysisResult { FromLlm = false, FallbackReason = reason };
            var wantedIds = request.WantsToLearn.Select(w => w.SkillId).ToHashSet();

            foreach (var wanted in request.WantsToLearn)
            {
                var wantedTokens = Tokenise($"{wanted.Name} {wanted.Category} {wanted.Description}");
                var matches = new List<RelatedSkill>();

                foreach (var candidate in request.Catalog)
                {
                    if (candidate.SkillId == wanted.SkillId || wantedIds.Contains(candidate.SkillId))
                        continue;

                    var candidateTokens = Tokenise($"{candidate.Name} {candidate.Category} {candidate.Description}");
                    var similarity = Jaccard(wantedTokens, candidateTokens);

                    var sameCategory = !string.IsNullOrWhiteSpace(wanted.Category)
                        && string.Equals(wanted.Category, candidate.Category, StringComparison.OrdinalIgnoreCase);

                    if (sameCategory)
                        similarity = Math.Max(similarity, 0.45);

                    if (similarity < 0.30)
                        continue;

                    matches.Add(new RelatedSkill
                    {
                        SkillId = candidate.SkillId,
                        SkillName = candidate.Name,
                        Similarity = Math.Round(Math.Min(similarity, 0.9), 2),
                        Reason = sameCategory
                            ? $"Also a {candidate.Category} skill, like {wanted.Name}."
                            : $"Shares topics with {wanted.Name}."
                    });
                }

                if (matches.Count > 0)
                {
                    result.RelatedSkills[wanted.SkillId] = matches
                        .OrderByDescending(m => m.Similarity)
                        .Take(5)
                        .ToList();
                }
            }

            result.Keywords = request.WantsToLearn
                .SelectMany(w => Tokenise($"{w.Name} {w.Description}"))
                .Distinct()
                .Take(10)
                .ToList();

            result.LearningPath = request.WantsToLearn
                .OrderBy(w => w.Level)
                .Take(5)
                .Select((w, index) => new LearningPathStep
                {
                    Order = index + 1,
                    Title = $"Build up {w.Name}",
                    Detail = $"Start at {w.Level} level and pair with a peer who teaches {w.Name}.",
                    SkillId = w.SkillId
                })
                .ToList();

            return result;
        }

        private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "and", "the", "for", "with", "that", "this", "from", "want", "learn", "learning",
            "teach", "teaching", "skill", "skills", "basic", "basics", "using", "how", "can"
        };

        private static HashSet<string> Tokenise(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            return text
                .Split(new[] { ' ', ',', '.', '-', '/', '(', ')', ':', ';', '\n', '\r', '\t' },
                       StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim().ToLowerInvariant())
                .Where(t => t.Length > 2 && !StopWords.Contains(t))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        private static double Jaccard(HashSet<string> left, HashSet<string> right)
        {
            if (left.Count == 0 || right.Count == 0)
                return 0;

            var intersection = left.Count(right.Contains);
            var union = left.Count + right.Count - intersection;
            return union == 0 ? 0 : (double)intersection / union;
        }

        public async Task<Dictionary<string, string>> ExplainMatchesAsync(
            IReadOnlyList<MatchExplanationRequest> matches,
            CancellationToken cancellationToken = default)
        {
            var empty = new Dictionary<string, string>();

            if (matches.Count == 0 || !_options.GenerateMatchExplanations || !_client.IsConfigured)
                return empty;

            var shortlist = matches.Take(Math.Max(1, _options.ExplanationCount)).ToList();

            var builder = new StringBuilder();
            builder.AppendLine("Write one short, encouraging sentence for each recommended peer mentor,");
            builder.AppendLine("addressed to the student, saying why this mentor is a good match.");
            builder.AppendLine("Do not invent facts. Use only what is listed. Max 25 words each.");
            builder.AppendLine();

            foreach (var match in shortlist)
            {
                builder.AppendLine($"mentorId={match.MentorId}");
                builder.AppendLine($"  name: {match.MentorName}");
                builder.AppendLine($"  matchScore: {match.MatchScore:0.#}");
                builder.AppendLine($"  teachesFromYourList: {Join(match.DirectSkills)}");
                builder.AppendLine($"  relatedSkillsTheyTeach: {Join(match.RelatedSkills)}");
                builder.AppendLine($"  wantsToLearnFromYou: {Join(match.ReciprocalSkills)}");
                builder.AppendLine($"  averageRating: {(match.AverageRating.HasValue ? match.AverageRating.Value.ToString("0.0") : "none yet")}");
                builder.AppendLine($"  completedSessions: {match.CompletedSessions}");
                builder.AppendLine();
            }

            builder.AppendLine("""Reply as JSON: { "explanations": [ { "mentorId": "...", "text": "..." } ] }""");

            var raw = await _client.GenerateJsonAsync(
                "You write short, factual match explanations for a student peer-learning platform. JSON only.",
                builder.ToString(),
                cancellationToken);

            var parsed = GeminiClient.ParseJson<LlmExplanationResponse>(raw, _logger);
            if (parsed?.Explanations == null)
                return empty;

            var allowed = shortlist.Select(m => m.MentorId).ToHashSet();

            return parsed.Explanations
                .Where(e => !string.IsNullOrWhiteSpace(e.MentorId)
                            && !string.IsNullOrWhiteSpace(e.Text)
                            && allowed.Contains(e.MentorId))
                .GroupBy(e => e.MentorId)
                .ToDictionary(g => g.Key, g => g.First().Text.Trim());
        }

        private static string Join(List<string> values) =>
            values.Count == 0 ? "none" : string.Join(", ", values);

        private static string Clean(string? value) =>
            string.IsNullOrWhiteSpace(value)
                ? "no description given"
                : value.Replace("\"", "'").Replace("\n", " ").Trim();

        private static string BuildCacheKey(SkillAnalysisRequest request)
        {
            var wants = string.Join(",", request.WantsToLearn
                .OrderBy(w => w.SkillId)
                .Select(w => $"{w.SkillId}:{w.Level}:{w.Description?.GetHashCode()}"));
            var teaches = string.Join(",", request.CanTeach.OrderBy(t => t.SkillId).Select(t => t.SkillId));
            return $"skill-analysis:{request.LearnerId}:{wants}|{teaches}|{request.Catalog.Count}";
        }

        private class LlmAnalysisResponse
        {
            [JsonPropertyName("relatedSkills")]
            public List<LlmRelatedGroup>? RelatedSkills { get; set; }

            [JsonPropertyName("keywords")]
            public List<string>? Keywords { get; set; }

            [JsonPropertyName("learningPath")]
            public List<LlmLearningStep>? LearningPath { get; set; }
        }

        private class LlmRelatedGroup
        {
            [JsonPropertyName("forSkillId")]
            public int ForSkillId { get; set; }

            [JsonPropertyName("matches")]
            public List<LlmRelatedMatch>? Matches { get; set; }
        }

        private class LlmRelatedMatch
        {
            [JsonPropertyName("skillId")]
            public int SkillId { get; set; }

            [JsonPropertyName("similarity")]
            public double Similarity { get; set; }

            [JsonPropertyName("reason")]
            public string? Reason { get; set; }
        }

        private class LlmLearningStep
        {
            [JsonPropertyName("order")]
            public int Order { get; set; }

            [JsonPropertyName("title")]
            public string Title { get; set; } = string.Empty;

            [JsonPropertyName("detail")]
            public string? Detail { get; set; }

            [JsonPropertyName("skillId")]
            public int? SkillId { get; set; }
        }

        private class LlmExplanationResponse
        {
            [JsonPropertyName("explanations")]
            public List<LlmExplanation>? Explanations { get; set; }
        }

        private class LlmExplanation
        {
            [JsonPropertyName("mentorId")]
            public string MentorId { get; set; } = string.Empty;

            [JsonPropertyName("text")]
            public string Text { get; set; } = string.Empty;
        }
    }
}
