namespace AIstudentskillexchange.Services.Search
{
    /// <summary>
    /// Tunables for peer search. Bound from the "PeerSearch" section of
    /// appsettings.json when present; every value has a working default so the
    /// module runs without any configuration being added to the shared file.
    /// </summary>
    public class PeerSearchOptions
    {
        public const string SectionName = "PeerSearch";

        /// <summary>Results per page.</summary>
        public int PageSize { get; set; } = 10;

        /// <summary>Upper bound on page size, to keep queries predictable.</summary>
        public int MaxPageSize { get; set; } = 50;

        /// <summary>Minimum characters before the free-text query is applied.</summary>
        public int MinimumQueryLength { get; set; } = 2;

        /// <summary>Skills listed on a result card before "+N more" is shown.</summary>
        public int MaxSkillsPerCard { get; set; } = 8;

        // --- Match strength weights (0-100 total) ---

        /// <summary>Weight for skills they teach that the viewer wants to learn.</summary>
        public int TeachesWhatIWantWeight { get; set; } = 50;

        /// <summary>Weight for skills they want that the viewer can teach.</summary>
        public int WantsWhatICanTeachWeight { get; set; } = 35;

        /// <summary>Weight for goals both students share.</summary>
        public int SharedGoalWeight { get; set; } = 15;
    }
}
