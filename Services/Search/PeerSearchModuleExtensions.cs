using AIstudentskillexchange.Services.Search;

// Declared in the Microsoft.Extensions.DependencyInjection namespace, which is the
// standard convention for service-registration extension methods and is already
// imported implicitly by the web SDK. That means Program.cs needs NO extra using
// directive for this module - which keeps the merge footprint of this feature
// branch down to a single self-contained block, with no shared using-block edit
// that would collide with another member's branch.
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Registration for the Peer Discovery and Skill Matching Module.
    ///
    /// Usage in Program.cs:
    ///     builder.Services.AddPeerDiscoveryModule(builder.Configuration);
    /// </summary>
    public static class PeerSearchModuleExtensions
    {
        public static IServiceCollection AddPeerDiscoveryModule(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // PeerSearchOptions carries working defaults, so the module runs even
            // when appsettings.json has no "PeerSearch" section at all.
            services.Configure<PeerSearchOptions>(
                configuration.GetSection(PeerSearchOptions.SectionName));

            services.AddScoped<IPeerSearchService, PeerSearchService>();

            return services;
        }
    }
}
