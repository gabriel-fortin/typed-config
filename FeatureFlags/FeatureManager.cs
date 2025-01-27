using Microsoft.Extensions.Configuration;

namespace org.g14.FeatureFlags;

public class FeatureManager(IConfiguration configuration)
{
    private readonly IConfigurationSection _featureRoot =
        configuration.GetRequiredSection("FeatureFlags");

    public FeatureEntry this[string key]
    {
        get
        {
            string[] entryPathItems = key.Split('.');
            string configPath = string.Join(ConfigurationPath.KeyDelimiter, entryPathItems);
            IConfigurationSection configSection = _featureRoot.GetRequiredSection(configPath);
            return new FeatureEntry(configSection);
        }
    }
}