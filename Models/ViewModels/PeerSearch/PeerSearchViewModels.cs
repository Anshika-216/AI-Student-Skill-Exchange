using AIstudentskillexchange.Models;

namespace AIstudentskillexchange.Models.ViewModels.PeerSearch
{
    /// <summary>
    /// How a peer relates to the student doing the searching. This is the
    /// "skill matching" half of the module: the search finds people, this
    /// classifies what each of them is actually useful for.
    /// </summary>
    public enum PeerMatchType
    {
        /// <summary>No skill overlap in either direction.</summary>
        None = 0,

        /// <summary>They want to learn something the viewer teaches.</summary>
        Learner = 1,

        /// <summary>Both want to learn the same thing - useful as study partners.</summary>
        StudyBuddy = 2,

        /// <summary>They teach something the viewer wants to learn.</summary>
        Mentor = 3,

        /// <summary>Teaches what the viewer wants AND wants what the viewer teaches.</summary>
        ExchangePartner = 4
    }

    /// <summary>
    /// One skill shown on a peer's search result card.
    /// </summary>
    public class PeerSkillViewModel
    {
        public int SkillId { get; set; }
        public string SkillName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public ProficiencyLevel Level { get; set; }
        public SkillType Type { get; set; }

        /// <summary>True when this skill overlaps with the viewer's own profile.</summary>
        public bool IsMatch { get; set; }
    }

    /// <summary>
    /// A single student in the search results, with the matching already worked out.
    /// </summary>
    public class PeerResultViewModel
    {
        public string StudentId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Bio { get; set; }

        public PeerMatchType MatchType { get; set; }

        /// <summary>Their teachable skills that the viewer wants to learn.</summary>
        public List<PeerSkillViewModel> TeachesWhatIWant { get; set; } = new();

        /// <summary>Their learning goals that the viewer can teach.</summary>
        public List<PeerSkillViewModel> WantsWhatICanTeach { get; set; } = new();

        /// <summary>Goals both students share - candidates for studying together.</summary>
        public List<PeerSkillViewModel> SharedGoals { get; set; } = new();

        /// <summary>Everything else they teach, for context.</summary>
        public List<PeerSkillViewModel> OtherSkills { get; set; } = new();

        public int TotalSkillsTaught { get; set; }
        public int TotalGoals { get; set; }

        /// <summary>Simple, explainable 0-100 overlap strength. Not an AI score.</summary>
        public int MatchStrength { get; set; }

        /// <summary>Short label describing the relationship, e.g. "Exchange partner".</summary>
        public string MatchLabel => MatchType switch
        {
            PeerMatchType.ExchangePartner => "Exchange partner",
            PeerMatchType.Mentor => "Can teach you",
            PeerMatchType.Learner => "Wants to learn from you",
            PeerMatchType.StudyBuddy => "Study buddy",
            _ => "No direct overlap"
        };

        public string MatchBadgeCss => MatchType switch
        {
            PeerMatchType.ExchangePartner => "bg-success",
            PeerMatchType.Mentor => "bg-primary",
            PeerMatchType.Learner => "bg-info text-dark",
            PeerMatchType.StudyBuddy => "bg-warning text-dark",
            _ => "bg-secondary"
        };
    }

    /// <summary>
    /// How the results should be ordered.
    /// </summary>
    public enum PeerSortOrder
    {
        /// <summary>Strongest skill overlap first.</summary>
        BestMatch = 0,
        Name = 1,
        MostSkillsTaught = 2
    }

    /// <summary>
    /// The search criteria, bound straight from the query string so that every
    /// search is a shareable, bookmarkable URL.
    /// </summary>
    public class PeerSearchCriteria
    {
        /// <summary>Free text matched against name, bio and skill name.</summary>
        public string? Query { get; set; }

        /// <summary>Restrict to peers who have this specific skill.</summary>
        public int? SkillId { get; set; }

        /// <summary>Restrict to a skill category, e.g. Programming.</summary>
        public string? Category { get; set; }

        /// <summary>Restrict to peers teaching (or wanting) at this level.</summary>
        public ProficiencyLevel? Level { get; set; }

        /// <summary>Whether the skill filters apply to skills they teach or want to learn.</summary>
        public SkillType? SkillType { get; set; }

        /// <summary>Only show peers who teach something on the viewer's learning list.</summary>
        public bool OnlyMatchingMyGoals { get; set; }

        /// <summary>Only show peers who want to learn something the viewer teaches.</summary>
        public bool OnlyWantingMySkills { get; set; }

        public PeerSortOrder Sort { get; set; } = PeerSortOrder.BestMatch;

        public int Page { get; set; } = 1;

        /// <summary>True when nothing has been narrowed down at all.</summary>
        public bool IsEmpty =>
            string.IsNullOrWhiteSpace(Query)
            && SkillId == null
            && string.IsNullOrWhiteSpace(Category)
            && Level == null
            && SkillType == null
            && !OnlyMatchingMyGoals
            && !OnlyWantingMySkills;
    }

    /// <summary>
    /// Everything the peer discovery page needs.
    /// </summary>
    public class PeerSearchViewModel
    {
        public PeerSearchCriteria Criteria { get; set; } = new();

        public List<PeerResultViewModel> Results { get; set; } = new();

        // --- Filter dropdown sources ---
        public List<Skill> AllSkills { get; set; } = new();
        public List<string> AllCategories { get; set; } = new();

        // --- Paging ---
        public int TotalResults { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public int TotalPages => PageSize <= 0
            ? 1
            : Math.Max(1, (int)Math.Ceiling(TotalResults / (double)PageSize));

        public bool HasPreviousPage => Page > 1;
        public bool HasNextPage => Page < TotalPages;

        /// <summary>True when the viewer has no skills on their profile at all.</summary>
        public bool ViewerHasNoProfile { get; set; }

        public int ViewerGoalCount { get; set; }
        public int ViewerTeachCount { get; set; }
    }
}
