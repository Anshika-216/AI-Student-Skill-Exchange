using AIstudentskillexchange.Models;

namespace AIstudentskillexchange.Models.ViewModels.PeerSearch
{
    public enum PeerMatchType
    {
        None = 0,

        Learner = 1,

        StudyBuddy = 2,

        Mentor = 3,

        ExchangePartner = 4
    }

    public class PeerSkillViewModel
    {
        public int SkillId { get; set; }
        public string SkillName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public ProficiencyLevel Level { get; set; }
        public SkillType Type { get; set; }

        public bool IsMatch { get; set; }
    }

    public class PeerResultViewModel
    {
        public string StudentId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Bio { get; set; }

        public PeerMatchType MatchType { get; set; }

        public List<PeerSkillViewModel> TeachesWhatIWant { get; set; } = new();

        public List<PeerSkillViewModel> WantsWhatICanTeach { get; set; } = new();

        public List<PeerSkillViewModel> SharedGoals { get; set; } = new();

        public List<PeerSkillViewModel> OtherSkills { get; set; } = new();

        public int TotalSkillsTaught { get; set; }
        public int TotalGoals { get; set; }

        public int MatchStrength { get; set; }

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

    public enum PeerSortOrder
    {
        BestMatch = 0,
        Name = 1,
        MostSkillsTaught = 2
    }

    public class PeerSearchCriteria
    {
        public string? Query { get; set; }

        public int? SkillId { get; set; }

        public string? Category { get; set; }

        public ProficiencyLevel? Level { get; set; }

        public SkillType? SkillType { get; set; }

        public bool OnlyMatchingMyGoals { get; set; }

        public bool OnlyWantingMySkills { get; set; }

        public PeerSortOrder Sort { get; set; } = PeerSortOrder.BestMatch;

        public int Page { get; set; } = 1;

        public bool IsEmpty =>
            string.IsNullOrWhiteSpace(Query)
            && SkillId == null
            && string.IsNullOrWhiteSpace(Category)
            && Level == null
            && SkillType == null
            && !OnlyMatchingMyGoals
            && !OnlyWantingMySkills;
    }

    public class PeerSearchViewModel
    {
        public PeerSearchCriteria Criteria { get; set; } = new();

        public List<PeerResultViewModel> Results { get; set; } = new();

        public List<Skill> AllSkills { get; set; } = new();
        public List<string> AllCategories { get; set; } = new();

        public int TotalResults { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public int TotalPages => PageSize <= 0
            ? 1
            : Math.Max(1, (int)Math.Ceiling(TotalResults / (double)PageSize));

        public bool HasPreviousPage => Page > 1;
        public bool HasNextPage => Page < TotalPages;

        public bool ViewerHasNoProfile { get; set; }

        public int ViewerGoalCount { get; set; }
        public int ViewerTeachCount { get; set; }
    }
}
