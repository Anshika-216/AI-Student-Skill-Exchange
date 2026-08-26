namespace AIstudentskillexchange.Services.Search
{
    public class PeerSearchOptions
    {
        public const string SectionName = "PeerSearch";

        public int PageSize { get; set; } = 10;

        public int MaxPageSize { get; set; } = 50;

        public int MinimumQueryLength { get; set; } = 2;

        public int MaxSkillsPerCard { get; set; } = 8;

        public int TeachesWhatIWantWeight { get; set; } = 50;

        public int WantsWhatICanTeachWeight { get; set; } = 35;

        public int SharedGoalWeight { get; set; } = 15;
    }
}
