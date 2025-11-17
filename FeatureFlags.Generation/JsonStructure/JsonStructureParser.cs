using System.Text.Json;

namespace org.g14.FeatureFlags.Generation.JsonStructure;

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
        if (!element.EnumerateArray().Any())
        {
            arr.ItemType = new JsonPrimitiveType(JsonValueKind.Undefined);
            return arr;
        }


        JsonType candidateType = Parse(element.EnumerateArray().First());
        if (element.EnumerateArray().All(x => Parse(x) == candidateType))
        {
            arr.ItemType = candidateType;
            return arr;
        }
        else
        {
            arr.ItemType = new JsonPrimitiveType(JsonValueKind.Undefined);
            return arr;
        }
        // PERF: if checking every item is too expensive,
        // we could refrain from looping over all items and just use candidateType immediately
        // (i.e. assume other items are the same)
    }
}