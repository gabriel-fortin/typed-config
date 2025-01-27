namespace org.g14.FeatureFlags;

public interface IConfig
{
    bool IsEnabled { get; }
    
    T? Get<T>(string key);
}