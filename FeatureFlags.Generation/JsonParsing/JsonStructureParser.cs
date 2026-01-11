using System.Text.Json;
using org.g14.FeatureFlags.Generation.JsonParsing.Models;

namespace org.g14.FeatureFlags.Generation.JsonParsing;

public static class JsonStructureParser
{
    public static JsonType Parse(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => ParseObject(element),
            JsonValueKind.Array => ParseArray(element),
            _ => new JsonPrimitiveType(element.ValueKind)
        };
    }

    private static JsonObjectType ParseObject(JsonElement element)
    {
        var obj = new JsonObjectType();

        foreach (var prop in element.EnumerateObject())
        {
            obj.Properties[prop.Name] = Parse(prop.Value);
        }

        return obj;
    }

    private static JsonArrayType ParseArray(JsonElement element)
    {
        var arr = new JsonArrayType();

        // If empty, treat as "array of any"
        if (element.GetArrayLength() == 0)
        {
            arr.ItemType = new JsonPrimitiveType(JsonValueKind.Undefined);
            return arr;
        }

        JsonType sampledType = Parse(element.EnumerateArray().First());
        arr.ItemType = sampledType;
        return arr;
    }
}