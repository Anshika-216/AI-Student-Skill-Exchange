using AIstudentskillexchange.Services.AI;

namespace AIstudentskillexchange.Services
{
    public static class RecommendationModuleExtensions
    {
        public static IServiceCollection AddAiRecommendationModule(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<RecommendationOptions>(
                configuration.GetSection(RecommendationOptions.SectionName));
            services.Configure<GeminiOptions>(
                configuration.GetSection(GeminiOptions.SectionName));

            services.AddMemoryCache();
            services.AddHttpClient<GeminiClient>();
            services.AddScoped<ISkillAnalysisService, GeminiSkillAnalysisService>();
            services.AddScoped<IRecommendationService, RecommendationService>();

            return services;
        }
    }
}
