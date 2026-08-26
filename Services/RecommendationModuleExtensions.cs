using AIstudentskillexchange.Services.AI;

namespace AIstudentskillexchange.Services
{
    /// <summary>
    /// Everything the AI Recommendation Module needs to register itself.
    ///
    /// Kept in the module's own file on purpose: wiring this up costs exactly one
    /// line in Program.cs, so the module can be added or removed without anyone
    /// else's registrations being touched or reordered.
    ///
    /// Usage in Program.cs:
    ///     builder.Services.AddAiRecommendationModule(builder.Configuration);
    /// </summary>
    public static class RecommendationModuleExtensions
    {
        public static IServiceCollection AddAiRecommendationModule(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Both option types carry working defaults, so the module runs even
            // when appsettings.json has no matching sections at all.
            services.Configure<RecommendationOptions>(
                configuration.GetSection(RecommendationOptions.SectionName));
            services.Configure<GeminiOptions>(
                configuration.GetSection(GeminiOptions.SectionName));

            // AI Service: Gemini (free tier) with a deterministic offline fallback.
            // Analysis results are cached so the free-tier quota is not spent on repeats.
            services.AddMemoryCache();
            services.AddHttpClient<GeminiClient>();
            services.AddScoped<ISkillAnalysisService, GeminiSkillAnalysisService>();
            services.AddScoped<IRecommendationService, RecommendationService>();

            return services;
        }
    }
}
