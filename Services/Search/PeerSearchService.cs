using AIstudentskillexchange.Data;
using AIstudentskillexchange.Models;
using AIstudentskillexchange.Models.ViewModels.PeerSearch;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AIstudentskillexchange.Services.Search
{
    /// <summary>
    /// Peer Discovery and Skill Matching Module.
    ///
    /// Search flow:
    ///   1. Load the viewer's own skill profile (what they teach, what they want).
    ///   2. Build a filtered IQueryable over students, applying every criterion
    ///      in SQL so paging counts stay correct.
    ///   3. Sort and take one page.
    ///   4. Load the skills of just that page of students.
    ///   5. Work out the match type and strength for each result in memory.
    ///
    /// Only step 5 happens in memory, and only for one page of students, so the
    /// cost does not grow with the size of the student body.
    /// </summary>
    public class PeerSearchService : IPeerSearchService
    {
        private readonly ApplicationDbContext _context;
        private readonly PeerSearchOptions _options;
        private readonly ILogger<PeerSearchService> _logger;

        public PeerSearchService(
            ApplicationDbContext context,
            IOptions<PeerSearchOptions> options,
            ILogger<PeerSearchService> logger)
        {
            _context = context;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<PeerSearchViewModel> SearchAsync(
            string viewerId,
            PeerSearchCriteria criteria,
            CancellationToken cancellationToken = default)
        {
            criteria ??= new PeerSearchCriteria();

            var pageSize = Math.Clamp(_options.PageSize, 1, Math.Max(1, _options.MaxPageSize));
            var page = Math.Max(1, criteria.Page);

            var model = new PeerSearchViewModel
            {
                Criteria = criteria,
                Page = page,
                PageSize = pageSize
            };

            if (string.IsNullOrWhiteSpace(viewerId))
                return model;

            // ---- Step 1: the viewer's own profile --------------------------------
            var viewerSkills = await _context.StudentSkills
                .AsNoTracking()
                .Where(ss => ss.StudentId == viewerId)
                .Select(ss => new { ss.SkillId, ss.Type })
                .ToListAsync(cancellationToken);

            var myGoalIds = viewerSkills
                .Where(s => s.Type == SkillType.ToLearn)
                .Select(s => s.SkillId)
                .ToHashSet();

            var myTeachIds = viewerSkills
                .Where(s => s.Type == SkillType.ToTeach)
                .Select(s => s.SkillId)
                .ToHashSet();

            model.ViewerGoalCount = myGoalIds.Count;
            model.ViewerTeachCount = myTeachIds.Count;
            model.ViewerHasNoProfile = viewerSkills.Count == 0;

            // ---- Filter dropdown sources ----------------------------------------
            model.AllSkills = await _context.Skills
                .AsNoTracking()
                .OrderBy(s => s.Name)
                .ToListAsync(cancellationToken);

            model.AllCategories = model.AllSkills
                .Where(s => !string.IsNullOrWhiteSpace(s.Category))
                .Select(s => s.Category)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c)
                .ToList();

            // ---- Step 2: build the filtered query --------------------------------
            var query = BuildQuery(viewerId, criteria, myGoalIds, myTeachIds);

            model.TotalResults = await query.CountAsync(cancellationToken);

            if (model.TotalResults == 0)
                return model;

            // Clamp the page to what actually exists, so a stale URL still works.
            var totalPages = Math.Max(1, (int)Math.Ceiling(model.TotalResults / (double)pageSize));
            if (page > totalPages)
            {
                page = totalPages;
                model.Page = page;
                criteria.Page = page;
            }

            // ---- Step 3: sort and take one page ----------------------------------
            var ordered = ApplySort(query, criteria.Sort, myGoalIds, myTeachIds);

            var pageOfStudents = await ordered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new { u.Id, u.FullName, u.Bio })
                .ToListAsync(cancellationToken);

            var pageIds = pageOfStudents.Select(u => u.Id).ToList();

            // ---- Step 4: load skills for just this page --------------------------
            var skillRows = await _context.StudentSkills
                .AsNoTracking()
                .Include(ss => ss.Skill)
                .Where(ss => pageIds.Contains(ss.StudentId))
                .ToListAsync(cancellationToken);

            var skillsByStudent = skillRows
                .GroupBy(ss => ss.StudentId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // ---- Step 5: classify each result ------------------------------------
            foreach (var student in pageOfStudents)
            {
                var skills = skillsByStudent.GetValueOrDefault(student.Id) ?? new List<StudentSkill>();

                model.Results.Add(BuildResult(
                    student.Id,
                    student.FullName,
                    student.Bio,
                    skills,
                    myGoalIds,
                    myTeachIds));
            }

            // BestMatch cannot be fully ordered in SQL because match strength blends
            // three counts, so the page is re-sorted here for a stable final order.
            if (criteria.Sort == PeerSortOrder.BestMatch)
            {
                model.Results = model.Results
                    .OrderByDescending(r => r.MatchStrength)
                    .ThenByDescending(r => r.MatchType)
                    .ThenBy(r => r.FullName)
                    .ToList();
            }

            _logger.LogInformation(
                "Peer search by {ViewerId}: {Total} result(s), page {Page}/{TotalPages}.",
                viewerId, model.TotalResults, page, totalPages);

            return model;
        }

        public async Task<PeerResultViewModel?> GetPeerProfileAsync(
            string viewerId,
            string peerId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(peerId) || peerId == viewerId)
                return null;

            var peer = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == peerId)
                .Select(u => new { u.Id, u.FullName, u.Bio })
                .FirstOrDefaultAsync(cancellationToken);

            if (peer == null)
                return null;

            var viewerSkills = await _context.StudentSkills
                .AsNoTracking()
                .Where(ss => ss.StudentId == viewerId)
                .Select(ss => new { ss.SkillId, ss.Type })
                .ToListAsync(cancellationToken);

            var myGoalIds = viewerSkills
                .Where(s => s.Type == SkillType.ToLearn).Select(s => s.SkillId).ToHashSet();
            var myTeachIds = viewerSkills
                .Where(s => s.Type == SkillType.ToTeach).Select(s => s.SkillId).ToHashSet();

            var peerSkills = await _context.StudentSkills
                .AsNoTracking()
                .Include(ss => ss.Skill)
                .Where(ss => ss.StudentId == peerId)
                .ToListAsync(cancellationToken);

            return BuildResult(peer.Id, peer.FullName, peer.Bio, peerSkills, myGoalIds, myTeachIds);
        }

        // =========================================================================
        // Query building
        // =========================================================================

        /// <summary>
        /// Applies every search criterion as SQL. Kept as one IQueryable so the
        /// total count and the page come from the same filter definition.
        /// </summary>
        private IQueryable<ApplicationUser> BuildQuery(
            string viewerId,
            PeerSearchCriteria criteria,
            HashSet<int> myGoalIds,
            HashSet<int> myTeachIds)
        {
            // A student is only discoverable once they have listed at least one
            // skill - an empty profile is not a useful search result.
            var query = _context.Users
                .AsNoTracking()
                .Where(u => u.Id != viewerId)
                .Where(u => u.StudentSkills.Any());

            // Free text: name, bio, or the name of any skill on their profile.
            var text = criteria.Query?.Trim();
            if (!string.IsNullOrWhiteSpace(text) && text.Length >= _options.MinimumQueryLength)
            {
                query = query.Where(u =>
                    u.FullName.Contains(text)
                    || (u.Bio != null && u.Bio.Contains(text))
                    || u.StudentSkills.Any(ss => ss.Skill != null && ss.Skill.Name.Contains(text)));
            }

            // Skill / category / level filters, optionally narrowed to teach-or-learn.
            var type = criteria.SkillType;

            if (criteria.SkillId.HasValue)
            {
                var skillId = criteria.SkillId.Value;
                query = query.Where(u => u.StudentSkills.Any(ss =>
                    ss.SkillId == skillId && (type == null || ss.Type == type)));
            }

            if (!string.IsNullOrWhiteSpace(criteria.Category))
            {
                var category = criteria.Category;
                query = query.Where(u => u.StudentSkills.Any(ss =>
                    ss.Skill != null
                    && ss.Skill.Category == category
                    && (type == null || ss.Type == type)));
            }

            if (criteria.Level.HasValue)
            {
                var level = criteria.Level.Value;
                query = query.Where(u => u.StudentSkills.Any(ss =>
                    ss.Level == level && (type == null || ss.Type == type)));
            }

            // A skill type on its own means "anyone who teaches / wants anything".
            if (type.HasValue
                && !criteria.SkillId.HasValue
                && string.IsNullOrWhiteSpace(criteria.Category)
                && !criteria.Level.HasValue)
            {
                query = query.Where(u => u.StudentSkills.Any(ss => ss.Type == type));
            }

            // Matching shortcuts.
            if (criteria.OnlyMatchingMyGoals)
            {
                query = myGoalIds.Count == 0
                    ? query.Where(u => false)
                    : query.Where(u => u.StudentSkills.Any(ss =>
                        ss.Type == SkillType.ToTeach && myGoalIds.Contains(ss.SkillId)));
            }

            if (criteria.OnlyWantingMySkills)
            {
                query = myTeachIds.Count == 0
                    ? query.Where(u => false)
                    : query.Where(u => u.StudentSkills.Any(ss =>
                        ss.Type == SkillType.ToLearn && myTeachIds.Contains(ss.SkillId)));
            }

            return query;
        }

        /// <summary>
        /// Orders the query in SQL. BestMatch approximates the final order using
        /// counts that SQL can compute; the page is refined in memory afterwards.
        /// </summary>
        private static IQueryable<ApplicationUser> ApplySort(
            IQueryable<ApplicationUser> query,
            PeerSortOrder sort,
            HashSet<int> myGoalIds,
            HashSet<int> myTeachIds) => sort switch
            {
                PeerSortOrder.Name =>
                    query.OrderBy(u => u.FullName),

                PeerSortOrder.MostSkillsTaught =>
                    query.OrderByDescending(u => u.StudentSkills.Count(ss => ss.Type == SkillType.ToTeach))
                         .ThenBy(u => u.FullName),

                _ =>
                    query.OrderByDescending(u => u.StudentSkills
                             .Count(ss => ss.Type == SkillType.ToTeach && myGoalIds.Contains(ss.SkillId)))
                         .ThenByDescending(u => u.StudentSkills
                             .Count(ss => ss.Type == SkillType.ToLearn && myTeachIds.Contains(ss.SkillId)))
                         .ThenBy(u => u.FullName)
            };

        // =========================================================================
        // Skill matching
        // =========================================================================

        /// <summary>
        /// Works out how one peer relates to the viewer and how strong that
        /// overlap is. Deliberately simple arithmetic so the result is easy to
        /// explain to a student and easy to test.
        /// </summary>
        private PeerResultViewModel BuildResult(
            string studentId,
            string fullName,
            string? bio,
            List<StudentSkill> peerSkills,
            HashSet<int> myGoalIds,
            HashSet<int> myTeachIds)
        {
            var result = new PeerResultViewModel
            {
                StudentId = studentId,
                FullName = string.IsNullOrWhiteSpace(fullName) ? "Unnamed student" : fullName,
                Bio = bio,
                TotalSkillsTaught = peerSkills.Count(ss => ss.Type == SkillType.ToTeach),
                TotalGoals = peerSkills.Count(ss => ss.Type == SkillType.ToLearn)
            };

            foreach (var skill in peerSkills)
            {
                var view = ToSkillView(skill);

                if (skill.Type == SkillType.ToTeach)
                {
                    if (myGoalIds.Contains(skill.SkillId))
                    {
                        view.IsMatch = true;
                        result.TeachesWhatIWant.Add(view);
                    }
                    else
                    {
                        result.OtherSkills.Add(view);
                    }
                }
                else // ToLearn
                {
                    if (myTeachIds.Contains(skill.SkillId))
                    {
                        view.IsMatch = true;
                        result.WantsWhatICanTeach.Add(view);
                    }
                    else if (myGoalIds.Contains(skill.SkillId))
                    {
                        view.IsMatch = true;
                        result.SharedGoals.Add(view);
                    }
                }
            }

            result.MatchType = ClassifyMatch(result);
            result.MatchStrength = ScoreMatch(result, myGoalIds.Count, myTeachIds.Count);

            return result;
        }

        private static PeerSkillViewModel ToSkillView(StudentSkill skill) => new()
        {
            SkillId = skill.SkillId,
            SkillName = skill.Skill?.Name ?? $"Skill #{skill.SkillId}",
            Category = skill.Skill?.Category ?? string.Empty,
            Level = skill.Level,
            Type = skill.Type
        };

        /// <summary>
        /// Exchange partner beats mentor beats learner beats study buddy, because
        /// a two-way match is the most valuable outcome for a skill exchange.
        /// </summary>
        private static PeerMatchType ClassifyMatch(PeerResultViewModel result)
        {
            var canTeachMe = result.TeachesWhatIWant.Count > 0;
            var wantsMySkills = result.WantsWhatICanTeach.Count > 0;

            if (canTeachMe && wantsMySkills) return PeerMatchType.ExchangePartner;
            if (canTeachMe) return PeerMatchType.Mentor;
            if (wantsMySkills) return PeerMatchType.Learner;
            if (result.SharedGoals.Count > 0) return PeerMatchType.StudyBuddy;

            return PeerMatchType.None;
        }

        /// <summary>
        /// Blends the three overlap counts into a 0-100 figure. Each component is
        /// expressed as a fraction of what the viewer actually listed, so a peer
        /// covering both of your two goals scores higher than one covering two of
        /// your ten.
        /// </summary>
        private int ScoreMatch(PeerResultViewModel result, int myGoalCount, int myTeachCount)
        {
            double score = 0;

            if (myGoalCount > 0)
            {
                var covered = Math.Min(1.0, (double)result.TeachesWhatIWant.Count / myGoalCount);
                score += covered * _options.TeachesWhatIWantWeight;

                var shared = Math.Min(1.0, (double)result.SharedGoals.Count / myGoalCount);
                score += shared * _options.SharedGoalWeight;
            }

            if (myTeachCount > 0)
            {
                var wanted = Math.Min(1.0, (double)result.WantsWhatICanTeach.Count / myTeachCount);
                score += wanted * _options.WantsWhatICanTeachWeight;
            }

            return (int)Math.Round(Math.Clamp(score, 0, 100));
        }
    }
}
