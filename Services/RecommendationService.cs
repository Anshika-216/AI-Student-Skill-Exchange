using AIstudentskillexchange.Data;
using AIstudentskillexchange.Models;
using AIstudentskillexchange.Models.ViewModels;
using AIstudentskillexchange.Services.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AIstudentskillexchange.Services
{
    public class RecommendationService : IRecommendationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ISkillAnalysisService _skillAnalysis;
        private readonly RecommendationOptions _options;
        private readonly ILogger<RecommendationService> _logger;

        public RecommendationService(
            ApplicationDbContext context,
            ISkillAnalysisService skillAnalysis,
            IOptions<RecommendationOptions> options,
            ILogger<RecommendationService> logger)
        {
            _context = context;
            _skillAnalysis = skillAnalysis;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<RecommendationsViewModel> GetRecommendationsAsync(
            string learnerId,
            int? skillId = null,
            CancellationToken cancellationToken = default)
        {
            var model = new RecommendationsViewModel { FilterSkillId = skillId };

            if (string.IsNullOrWhiteSpace(learnerId))
                return model;

            var profile = await LoadLearnerProfileAsync(learnerId, skillId, cancellationToken);

            model.LearnerWishlist = profile.FullWishlistSkills;

            if (profile.Wishlist.Count == 0)
            {
                model.SuggestedSkills = await GetSuggestedSkillsAsync(learnerId, cancellationToken: cancellationToken);
                return model;
            }

            var analysis = await RunAnalysisAsync(profile, cancellationToken);

            model.AnalysisFromLlm = analysis.FromLlm;
            model.AnalysisNotice = analysis.FallbackReason;
            model.Keywords = analysis.Keywords;
            model.LearningPath = analysis.LearningPath;

            var recommendations = await RankMentorsAsync(profile, analysis, null, cancellationToken);

            await AttachAiExplanationsAsync(recommendations, cancellationToken);

            model.Recommendations = recommendations;
            model.SuggestedSkills = await GetSuggestedSkillsAsync(learnerId, cancellationToken: cancellationToken);

            return model;
        }

        public async Task<List<MentorRecommendationViewModel>> GetMentorRecommendationsAsync(
            string learnerId,
            int? skillId = null,
            int? maxResults = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(learnerId))
                return new List<MentorRecommendationViewModel>();

            var profile = await LoadLearnerProfileAsync(learnerId, skillId, cancellationToken);
            if (profile.Wishlist.Count == 0)
                return new List<MentorRecommendationViewModel>();

            var analysis = await RunAnalysisAsync(profile, cancellationToken);
            return await RankMentorsAsync(profile, analysis, maxResults, cancellationToken);
        }

        public async Task<List<SuggestedSkillViewModel>> GetSuggestedSkillsAsync(
            string learnerId,
            int take = 6,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(learnerId))
                return new List<SuggestedSkillViewModel>();

            var ownSkillIds = await _context.StudentSkills
                .AsNoTracking()
                .Where(ss => ss.StudentId == learnerId)
                .Select(ss => ss.SkillId)
                .Distinct()
                .ToListAsync(cancellationToken);

            var stats = await _context.Skills
                .AsNoTracking()
                .Where(s => !ownSkillIds.Contains(s.Id))
                .Select(s => new SuggestedSkillViewModel
                {
                    SkillId = s.Id,
                    SkillName = s.Name,
                    Category = s.Category,
                    AvailableMentors = s.StudentSkills
                        .Count(ss => ss.Type == SkillType.ToTeach && ss.StudentId != learnerId),
                    LearnersInterested = s.StudentSkills
                        .Count(ss => ss.Type == SkillType.ToLearn && ss.StudentId != learnerId)
                })
                .Where(s => s.AvailableMentors > 0)
                .ToListAsync(cancellationToken);

            foreach (var skill in stats)
            {
                skill.Reason = skill.LearnersInterested > 0
                    ? $"{skill.AvailableMentors} peer(s) teach this and {skill.LearnersInterested} student(s) are learning it."
                    : $"{skill.AvailableMentors} peer(s) are ready to teach this right now.";
            }

            return stats
                .OrderByDescending(s => s.AvailableMentors)
                .ThenByDescending(s => s.LearnersInterested)
                .ThenBy(s => s.SkillName)
                .Take(take)
                .ToList();
        }

        private sealed class LearnerProfile
        {
            public string LearnerId { get; init; } = string.Empty;

            public List<StudentSkill> Wishlist { get; init; } = new();

            public List<Skill> FullWishlistSkills { get; init; } = new();

            public List<StudentSkill> Teachables { get; init; } = new();

            public Dictionary<int, ProficiencyLevel> WishlistLevels { get; init; } = new();

            public Dictionary<int, string> WishlistNames { get; init; } = new();

            public HashSet<int> TeachableIds { get; init; } = new();
        }

        private async Task<LearnerProfile> LoadLearnerProfileAsync(
            string learnerId,
            int? skillId,
            CancellationToken cancellationToken)
        {
            var learnerSkills = await _context.StudentSkills
                .AsNoTracking()
                .Include(ss => ss.Skill)
                .Where(ss => ss.StudentId == learnerId)
                .ToListAsync(cancellationToken);

            var allGoals = learnerSkills.Where(ss => ss.Type == SkillType.ToLearn).ToList();

            var fullWishlistSkills = allGoals
                .Where(ss => ss.Skill != null)
                .Select(ss => ss.Skill!)
                .DistinctBy(s => s.Id)
                .OrderBy(s => s.Name)
                .ToList();

            if (skillId.HasValue && fullWishlistSkills.All(s => s.Id != skillId.Value))
                skillId = null;

            var wishlist = skillId.HasValue
                ? allGoals.Where(ss => ss.SkillId == skillId.Value).ToList()
                : allGoals;

            var teachables = learnerSkills.Where(ss => ss.Type == SkillType.ToTeach).ToList();

            return new LearnerProfile
            {
                LearnerId = learnerId,
                Wishlist = wishlist,
                FullWishlistSkills = fullWishlistSkills,
                Teachables = teachables,
                WishlistLevels = wishlist
                    .GroupBy(ss => ss.SkillId)
                    .ToDictionary(g => g.Key, g => g.Min(ss => ss.Level)),
                WishlistNames = wishlist
                    .GroupBy(ss => ss.SkillId)
                    .ToDictionary(g => g.Key, g => g.First().Skill?.Name ?? $"Skill #{g.Key}"),
                TeachableIds = teachables.Select(ss => ss.SkillId).ToHashSet()
            };
        }

        private async Task<SkillAnalysisResult> RunAnalysisAsync(
            LearnerProfile profile,
            CancellationToken cancellationToken)
        {
            var catalog = await _context.Skills
                .AsNoTracking()
                .OrderBy(s => s.Name)
                .Select(s => new CatalogSkill
                {
                    SkillId = s.Id,
                    Name = s.Name,
                    Category = s.Category
                })
                .ToListAsync(cancellationToken);

            var request = new SkillAnalysisRequest
            {
                LearnerId = profile.LearnerId,
                Catalog = catalog,
                WantsToLearn = profile.Wishlist.Select(ToAnalysisInput).ToList(),
                CanTeach = profile.Teachables.Select(ToAnalysisInput).ToList()
            };

            return await _skillAnalysis.AnalyseAsync(request, cancellationToken);
        }

        private static AnalysedSkillInput ToAnalysisInput(StudentSkill skill) => new()
        {
            SkillId = skill.SkillId,
            Name = skill.Skill?.Name ?? $"Skill #{skill.SkillId}",
            Category = skill.Skill?.Category ?? string.Empty,
            Level = skill.Level.ToString()
        };

        private sealed record RelatedLink(RelatedSkill Related, int GoalSkillId, string GoalSkillName);

        private async Task<List<MentorRecommendationViewModel>> RankMentorsAsync(
            LearnerProfile profile,
            SkillAnalysisResult analysis,
            int? maxResults,
            CancellationToken cancellationToken)
        {
            var goalIds = profile.WishlistLevels.Keys.ToHashSet();

            var relatedLinks = new Dictionary<int, RelatedLink>();
            foreach (var (goalSkillId, matches) in analysis.RelatedSkills)
            {
                if (!goalIds.Contains(goalSkillId))
                    continue;

                var goalName = profile.WishlistNames.GetValueOrDefault(goalSkillId, $"Skill #{goalSkillId}");

                foreach (var match in matches)
                {
                    if (match.Similarity < _options.MinimumRelatedSimilarity)
                        continue;
                    if (goalIds.Contains(match.SkillId))
                        continue;

                    if (!relatedLinks.TryGetValue(match.SkillId, out var existing)
                        || match.Similarity > existing.Related.Similarity)
                    {
                        relatedLinks[match.SkillId] = new RelatedLink(match, goalSkillId, goalName);
                    }
                }
            }

            var searchIds = goalIds.Concat(relatedLinks.Keys).Distinct().ToList();

            var candidateSkills = await _context.StudentSkills
                .AsNoTracking()
                .Include(ss => ss.Skill)
                .Include(ss => ss.Student)
                .Where(ss => ss.Type == SkillType.ToTeach
                             && ss.StudentId != profile.LearnerId
                             && searchIds.Contains(ss.SkillId))
                .ToListAsync(cancellationToken);

            if (candidateSkills.Count == 0)
                return new List<MentorRecommendationViewModel>();

            var mentorIds = candidateSkills.Select(ss => ss.StudentId).Distinct().ToList();

            var mentorWants = await _context.StudentSkills
                .AsNoTracking()
                .Include(ss => ss.Skill)
                .Where(ss => ss.Type == SkillType.ToLearn && mentorIds.Contains(ss.StudentId))
                .ToListAsync(cancellationToken);

            var wantsByMentor = mentorWants
                .GroupBy(ss => ss.StudentId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var openRequests = (await _context.LearningRequests
                .AsNoTracking()
                .Where(lr => lr.SenderId == profile.LearnerId
                             && mentorIds.Contains(lr.ReceiverId)
                             && lr.Status != RequestStatus.Rejected)
                .Select(lr => lr.ReceiverId)
                .Distinct()
                .ToListAsync(cancellationToken))
                .ToHashSet();

            var feedbackRows = await _context.Feedbacks
                .AsNoTracking()
                .Where(f => f.Session != null
                            && f.Session.Request != null
                            && mentorIds.Contains(f.Session.Request.ReceiverId)
                            && f.ReviewerId != f.Session.Request.ReceiverId)
                .Select(f => new { MentorId = f.Session!.Request!.ReceiverId, f.Rating })
                .ToListAsync(cancellationToken);

            var ratings = feedbackRows
                .GroupBy(f => f.MentorId)
                .ToDictionary(
                    g => g.Key,
                    g => (Average: g.Average(f => f.Rating), Count: g.Count()));

            var completedRows = await _context.LearningSessions
                .AsNoTracking()
                .Where(s => s.Status == SessionStatus.Completed
                            && s.Request != null
                            && mentorIds.Contains(s.Request.ReceiverId))
                .Select(s => s.Request!.ReceiverId)
                .ToListAsync(cancellationToken);

            var completedCounts = completedRows
                .GroupBy(id => id)
                .ToDictionary(g => g.Key, g => g.Count());

            var results = new List<MentorRecommendationViewModel>();

            foreach (var group in candidateSkills.GroupBy(ss => ss.StudentId))
            {
                var mentorId = group.Key;
                var mentor = group.First().Student;
                if (mentor == null)
                    continue;

                (double Average, int Count)? rating =
                    ratings.TryGetValue(mentorId, out var r) ? r : null;

                var recommendation = BuildRecommendation(
                    mentorId,
                    mentor,
                    group.ToList(),
                    profile,
                    relatedLinks,
                    wantsByMentor.GetValueOrDefault(mentorId) ?? new List<StudentSkill>(),
                    rating,
                    completedCounts.GetValueOrDefault(mentorId),
                    openRequests.Contains(mentorId));

                if (recommendation.MatchScore >= _options.MinimumScoreThreshold)
                    results.Add(recommendation);
            }

            var take = maxResults ?? _options.MaxResults;

            _logger.LogInformation(
                "Recommendations for {LearnerId}: {Goals} goal(s), {Related} AI-related skill(s), " +
                "{Candidates} candidate(s), {Kept} above threshold. LLM analysis: {FromLlm}.",
                profile.LearnerId, goalIds.Count, relatedLinks.Count, mentorIds.Count,
                results.Count, analysis.FromLlm);

            return results
                .OrderByDescending(x => x.MatchScore)
                .ThenByDescending(x => x.AverageRating ?? 0)
                .ThenBy(x => x.FullName)
                .Take(take)
                .ToList();
        }

        private MentorRecommendationViewModel BuildRecommendation(
            string mentorId,
            ApplicationUser mentor,
            List<StudentSkill> teachSkills,
            LearnerProfile profile,
            Dictionary<int, RelatedLink> relatedLinks,
            List<StudentSkill> mentorWants,
            (double Average, int Count)? rating,
            int completedSessions,
            bool hasOpenRequest)
        {
            var model = new MentorRecommendationViewModel
            {
                MentorId = mentorId,
                FullName = string.IsNullOrWhiteSpace(mentor.FullName) ? "Unnamed student" : mentor.FullName,
                Bio = mentor.Bio,
                AverageRating = rating?.Average,
                RatingCount = rating?.Count ?? 0,
                CompletedSessions = completedSessions,
                HasOpenRequest = hasOpenRequest
            };

            foreach (var teach in teachSkills.DistinctBy(ss => ss.SkillId))
            {
                var skillName = teach.Skill?.Name ?? $"Skill #{teach.SkillId}";
                var category = teach.Skill?.Category ?? string.Empty;

                if (profile.WishlistLevels.TryGetValue(teach.SkillId, out var learnerLevel))
                {
                    model.DirectMatches.Add(new MatchedSkillViewModel
                    {
                        SkillId = teach.SkillId,
                        SkillName = skillName,
                        Category = category,
                        MentorLevel = teach.Level,
                        LearnerLevel = learnerLevel,
                        IsDirectMatch = true,
                        Similarity = 1.0
                    });
                }
                else if (relatedLinks.TryGetValue(teach.SkillId, out var link))
                {
                    model.RelatedMatches.Add(new MatchedSkillViewModel
                    {
                        SkillId = teach.SkillId,
                        SkillName = skillName,
                        Category = category,
                        MentorLevel = teach.Level,
                        LearnerLevel = profile.WishlistLevels.GetValueOrDefault(
                            link.GoalSkillId, ProficiencyLevel.Beginner),
                        IsDirectMatch = false,
                        Similarity = link.Related.Similarity,
                        RelatedToSkillName = link.GoalSkillName,
                        RelationReason = link.Related.Reason
                    });
                }
            }

            var goalCount = Math.Max(1, profile.WishlistLevels.Count);

            var skillMatch = Math.Min(1.0, (double)model.DirectMatches.Count / goalCount);

            var relatedMatch = Math.Min(1.0, model.RelatedMatches.Sum(m => m.Similarity) / goalCount);

            var levelSource = model.DirectMatches.Count > 0 ? model.DirectMatches : model.RelatedMatches;
            var proficiency = levelSource.Count == 0
                ? 0
                : levelSource.Average(m => ScoreLevelGap(m.LevelGap));

            model.ReciprocalSkills = mentorWants
                .Where(ss => profile.TeachableIds.Contains(ss.SkillId))
                .Select(ss => ss.Skill?.Name ?? $"Skill #{ss.SkillId}")
                .Distinct()
                .ToList();

            var reciprocity = model.ReciprocalSkills.Count switch
            {
                0 => 0.0,
                1 => 0.8,
                _ => 1.0
            };

            var ratingScore = rating.HasValue
                ? Math.Clamp(rating.Value.Average / 5.0, 0, 1)
                : _options.NewMentorRatingBaseline;

            var saturation = Math.Max(1, _options.ExperienceSaturationSessions);
            var experience = Math.Min(1.0, (double)completedSessions / saturation);

            model.Breakdown = new ScoreBreakdown
            {
                SkillMatch = skillMatch,
                RelatedSkillMatch = relatedMatch,
                Proficiency = proficiency,
                Reciprocity = reciprocity,
                Rating = ratingScore,
                Experience = experience
            };

            var weightTotal = _options.SkillMatchWeight
                              + _options.RelatedSkillWeight
                              + _options.ProficiencyWeight
                              + _options.ReciprocityWeight
                              + _options.RatingWeight
                              + _options.ExperienceWeight;

            if (weightTotal <= 0)
                weightTotal = 1;

            var weighted = skillMatch * _options.SkillMatchWeight
                           + relatedMatch * _options.RelatedSkillWeight
                           + proficiency * _options.ProficiencyWeight
                           + reciprocity * _options.ReciprocityWeight
                           + ratingScore * _options.RatingWeight
                           + experience * _options.ExperienceWeight;

            var score = weighted / weightTotal * 100.0;

            if (hasOpenRequest)
                score *= _options.ExistingRequestPenalty;

            model.MatchScore = Math.Round(Math.Clamp(score, 0, 100), 1);
            model.Reasons = BuildReasons(model);

            return model;
        }

        private static double ScoreLevelGap(int gap) => gap switch
        {
            >= 2 => 1.00,
            1 => 0.85,
            0 => 0.50,
            _ => 0.15
        };

        private static List<string> BuildReasons(MentorRecommendationViewModel model)
        {
            var reasons = new List<string>();

            if (model.DirectMatches.Count > 0)
            {
                var names = string.Join(", ", model.DirectMatches.Select(m => m.SkillName));
                reasons.Add(model.DirectMatches.Count == 1
                    ? $"Teaches {names}, which is on your learning list."
                    : $"Covers {model.DirectMatches.Count} skills from your list: {names}.");
            }

            foreach (var related in model.RelatedMatches.OrderByDescending(m => m.Similarity).Take(3))
            {
                var because = string.IsNullOrWhiteSpace(related.RelationReason)
                    ? $"related to {related.RelatedToSkillName}"
                    : related.RelationReason;
                reasons.Add($"Teaches {related.SkillName} - {because}.");
            }

            var strongest = model.AllMatches.OrderByDescending(m => m.LevelGap).FirstOrDefault();
            if (strongest != null && strongest.LevelGap > 0)
            {
                reasons.Add($"{strongest.MentorLevel} in {strongest.SkillName} while you are at {strongest.LearnerLevel} level.");
            }
            else if (strongest != null && strongest.LevelGap == 0)
            {
                reasons.Add($"At the same {strongest.MentorLevel} level as you in {strongest.SkillName} - good for peer practice.");
            }

            if (model.IsMutualExchange)
            {
                reasons.Add($"Two-way exchange: they want to learn {string.Join(", ", model.ReciprocalSkills)} from you.");
            }

            reasons.Add(model.AverageRating.HasValue
                ? $"Rated {model.AverageRating.Value:0.0}/5 across {model.RatingCount} review(s)."
                : "New mentor - no reviews yet.");

            if (model.CompletedSessions > 0)
            {
                reasons.Add($"Has completed {model.CompletedSessions} session(s) as a teacher.");
            }

            if (model.HasOpenRequest)
            {
                reasons.Add("You already have a request open with this student.");
            }

            return reasons;
        }

        private async Task AttachAiExplanationsAsync(
            List<MentorRecommendationViewModel> recommendations,
            CancellationToken cancellationToken)
        {
            if (recommendations.Count == 0)
                return;

            var requests = recommendations
                .Select(r => new MatchExplanationRequest
                {
                    MentorId = r.MentorId,
                    MentorName = r.FullName,
                    MatchScore = r.MatchScore,
                    DirectSkills = r.DirectMatches.Select(m => m.SkillName).ToList(),
                    RelatedSkills = r.RelatedMatches.Select(m => m.SkillName).ToList(),
                    ReciprocalSkills = r.ReciprocalSkills,
                    AverageRating = r.AverageRating,
                    CompletedSessions = r.CompletedSessions
                })
                .ToList();

            var explanations = await _skillAnalysis.ExplainMatchesAsync(requests, cancellationToken);

            foreach (var recommendation in recommendations)
            {
                if (explanations.TryGetValue(recommendation.MentorId, out var text))
                    recommendation.AiExplanation = text;
            }
        }
    }
}
