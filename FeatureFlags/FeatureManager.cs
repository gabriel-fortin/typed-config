namespace org.g14.FeatureFlags;

public class FeatureManager
{
    public FeatureEntry this[string s]
    {
        get => throw new NotImplementedException();
    }
}

public class FeatureEntry()
{
    public bool IsEnabled
    {
        get => throw new NotImplementedException();
    }
}