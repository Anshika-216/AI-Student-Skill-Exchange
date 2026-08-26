using Microsoft.EntityFrameworkCore;
using AIstudentskillexchange.Models;

namespace AIstudentskillexchange.Data
{
    /// <summary>
    /// Seeds the shared skill catalogue.
    ///
    /// Every module in the app keys off the Skills table: peer search filters by
    /// it, the recommendation scorer matches on it, and a learning request needs
    /// a SkillId. With an empty table each of those returns nothing, so a fresh
    /// clone of the repo looks broken. This gives the app something to work with
    /// on first run.
    ///
    /// Only ever inserts skills that are missing, so it is safe to run on every
    /// startup and will not disturb rows added later.
    /// </summary>
    public static class SkillCatalogueSeeder
    {
        private static readonly (string Name, string Category)[] DefaultSkills =
        [
            ("C#", "Programming"),
            ("Python", "Programming"),
            ("JavaScript", "Programming"),
            ("Java", "Programming"),
            ("C++", "Programming"),
            ("SQL", "Programming"),
            ("HTML & CSS", "Web Development"),
            ("React", "Web Development"),
            ("ASP.NET Core", "Web Development"),
            ("Node.js", "Web Development"),
            ("Machine Learning", "Data Science"),
            ("Data Analysis", "Data Science"),
            ("Statistics", "Data Science"),
            ("Power BI", "Data Science"),
            ("UI/UX Design", "Design"),
            ("Figma", "Design"),
            ("Graphic Design", "Design"),
            ("Video Editing", "Design"),
            ("English", "Language"),
            ("Hindi", "Language"),
            ("Spanish", "Language"),
            ("German", "Language"),
            ("Public Speaking", "Soft Skills"),
            ("Technical Writing", "Soft Skills"),
            ("Resume Building", "Soft Skills"),
            ("Time Management", "Soft Skills"),
            ("Git & GitHub", "Tools"),
            ("Docker", "Tools"),
            ("Linux", "Tools"),
            ("Excel", "Tools")
        ];

        public static async Task SeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
        {
            var existing = await context.Skills
                .Select(s => s.Name)
                .ToListAsync(cancellationToken);

            var existingNames = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);

            var missing = DefaultSkills
                .Where(s => !existingNames.Contains(s.Name))
                .Select(s => new Skill { Name = s.Name, Category = s.Category })
                .ToList();

            if (missing.Count == 0)
                return;

            context.Skills.AddRange(missing);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
