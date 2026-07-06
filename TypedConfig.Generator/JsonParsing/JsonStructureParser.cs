using Newtonsoft.Json.Linq;
using org.g14.TypedConfig.Generator.JsonParsing.Models;

namespace org.g14.TypedConfig.Generator.JsonParsing;

public static class JsonStructureParser
{
    public static JsonType Parse(JToken token)
    {
        return token.Type switch
        {
            JTokenType.Object => ParseObject((JObject)token),
            JTokenType.Array => ParseArray((JArray)token),
            _ => new JsonPrimitiveType(token.Type)
        };
    }

    private static JsonObjectType ParseObject(JObject obj)
    {
        var result = new JsonObjectType();

        foreach (var prop in obj.Properties())
        {
            result.Properties[prop.Name] = Parse(prop.Value);
        }

        return result;
    }

    private static JsonArrayType ParseArray(JArray array)
    {
        var arr = new JsonArrayType();

        // If empty, treat as "array of any"
        if (!array.Any())
        {
            arr.ItemType = new JsonPrimitiveType(JTokenType.Undefined);
            return arr;
        }

        JsonType sampledType = Parse(array.First());
        arr.ItemType = sampledType;
        return arr;
    }
}