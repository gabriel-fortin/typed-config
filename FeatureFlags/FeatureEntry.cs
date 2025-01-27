using System.ComponentModel;
using Microsoft.Extensions.Configuration;

namespace org.g14.FeatureFlags;

public class FeatureEntry(IConfigurationSection configSection)
{
    private const string IS_ENABLED = "IsEnabled";

    public bool IsEnabled => Get<bool>(IS_ENABLED);

    public T? Get<T>(string key)
    {
        IConfigurationSection entry = configSection.GetSection(key);
        if (!entry.Exists())
        {
            throw new InvalidOperationException(
                $"Config section '{configSection.Path}'" +
                $" does not contain the entry '{key}'.");
        }

        // we checked that it exists so it is never null
        string value = entry.Value!;

        try
        {
            TypeConverter converter = TypeDescriptor.GetConverter(typeof(T));
            return (T?)converter.ConvertFromString(value);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException(
                $"Config entry '{entry.Path}'" +
                $" having value '{value}'" +
                $" cannot be converted to type '{typeof(T)}'.",
                innerException: ex);
        }
    }
}