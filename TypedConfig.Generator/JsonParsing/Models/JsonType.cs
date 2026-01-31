using System.Text.Json;

namespace org.g14.TypedConfig.Generator.JsonParsing.Models;

public abstract record JsonType
{
}

public record JsonObjectType : JsonType
{
    public Dictionary<string, JsonType> Properties { get; } = new();
}

public record JsonArrayType : JsonType
{
    public JsonType ItemType { get; set; } = null!;
}

public record JsonPrimitiveType : JsonType
{
    public JsonValueKind Kind { get; }

    public JsonPrimitiveType(JsonValueKind kind)
    {
        Kind = kind;
    }
}