using Microsoft.Extensions.Configuration;

namespace BARQ.Application.Services
{
    public interface IFeatureFlagService
    {
        bool IsEnabled(string flagName);
    }

    public sealed class FeatureFlagService : IFeatureFlagService
    {
        private readonly IConfiguration _cfg;
        public FeatureFlagService(IConfiguration cfg) => _cfg = cfg;
        public bool IsEnabled(string flagName) => _cfg.GetValue<bool>($"Features:{flagName}", false);
    }
}
