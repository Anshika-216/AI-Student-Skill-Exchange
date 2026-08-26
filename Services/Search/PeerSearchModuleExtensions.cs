using AIstudentskillexchange.Services.Search;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class PeerSearchModuleExtensions
    {
        public static IServiceCollection AddPeerDiscoveryModule(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<PeerSearchOptions>(
                configuration.GetSection(PeerSearchOptions.SectionName));

            services.AddScoped<IPeerSearchService, PeerSearchService>();

            return services;
        }
    }
}
