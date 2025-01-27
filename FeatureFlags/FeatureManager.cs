using Microsoft.Extensions.Configuration;

namespace org.g14.FeatureFlags;

public class FeatureManager(IConfiguration configuration) : IFeatureManager
{
    private readonly IConfigurationSection _featureRoot =
        configuration.GetRequiredSection("FeatureFlags");

    public IConfig this[string key]
    {
        get
        {
            string[] entryPathItems = key.Split('.');
            string configPath = string.Join(ConfigurationPath.KeyDelimiter, entryPathItems);
            IConfigurationSection configSection = _featureRoot.GetRequiredSection(configPath);
            return new Config(configSection);
        }
    }
}