namespace org.g14.FeatureFlags;

public interface IFeatureManager
{
    IConfig this[string key] { get; }
}