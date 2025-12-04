using System.Text.Json;
using NUnit.Framework;
using org.g14.FeatureFlags.Generation.JsonParsing;
using org.g14.FeatureFlags.Generation.JsonParsing.Models;

namespace org.g14.FeatureFlags.Generation.Tests;

/** Tests the JSON structure parsing logic:
    - Primitive types (string, number, true, false)
    - Empty objects
    - Objects with single/multiple properties
    - Empty arrays (returns Undefined type)
    - Arrays with primitive items
    - Arrays of objects
    - 2D arrays (arrays of arrays)
    - Arrays of empty arrays
    - Nested objects
    - Complex nested structures
*/

[TestFixture]
public class JsonStructureParserTests
{
    [Test]
    public void Parse_WithStringValue_ReturnsPrimitiveStringType()
    {
        // Arrange
        JsonDocument jsonDoc = JsonDocument.Parse(@"""hello world""");
        JsonElement element = jsonDoc.RootElement;

        // Act
        JsonType result = JsonStructureParser.Parse(element);

        // Assert
        Assert.That(result, Is.InstanceOf<JsonPrimitiveType>());
        JsonPrimitiveType primitive = (JsonPrimitiveType)result;
        Assert.That(primitive.Kind, Is.EqualTo(JsonValueKind.String));
    }

    [Test]
    public void Parse_WithNumberValue_ReturnsPrimitiveNumberType()
    {
        // Arrange
        JsonDocument jsonDoc = JsonDocument.Parse("42");
        JsonElement element = jsonDoc.RootElement;

        // Act
        JsonType result = JsonStructureParser.Parse(element);

        // Assert
        Assert.That(result, Is.InstanceOf<JsonPrimitiveType>());
        JsonPrimitiveType primitive = (JsonPrimitiveType)result;
        Assert.That(primitive.Kind, Is.EqualTo(JsonValueKind.Number));
    }

    [Test]
    public void Parse_WithTrueValue_ReturnsPrimitiveTrueType()
    {
        // Arrange
        JsonDocument jsonDoc = JsonDocument.Parse("true");
        JsonElement element = jsonDoc.RootElement;

        // Act
        JsonType result = JsonStructureParser.Parse(element);

        // Assert
        Assert.That(result, Is.InstanceOf<JsonPrimitiveType>());
        JsonPrimitiveType primitive = (JsonPrimitiveType)result;
        Assert.That(primitive.Kind, Is.EqualTo(JsonValueKind.True));
    }

    [Test]
    public void Parse_WithFalseValue_ReturnsPrimitiveFalseType()
    {
        // Arrange
        JsonDocument jsonDoc = JsonDocument.Parse("false");
        JsonElement element = jsonDoc.RootElement;

        // Act
        JsonType result = JsonStructureParser.Parse(element);

        // Assert
        Assert.That(result, Is.InstanceOf<JsonPrimitiveType>());
        JsonPrimitiveType primitive = (JsonPrimitiveType)result;
        Assert.That(primitive.Kind, Is.EqualTo(JsonValueKind.False));
    }

    [Test]
    public void Parse_WithEmptyObject_ReturnsObjectTypeWithNoProperties()
    {
        // Arrange
        JsonDocument jsonDoc = JsonDocument.Parse("{}");
        JsonElement element = jsonDoc.RootElement;

        // Act
        JsonType result = JsonStructureParser.Parse(element);

        // Assert
        Assert.That(result, Is.InstanceOf<JsonObjectType>());
        JsonObjectType obj = (JsonObjectType)result;
        Assert.That(obj.Properties, Is.Empty);
    }

    [Test]
    public void Parse_WithObjectWithSingleProperty_ReturnsObjectTypeWithOneProperty()
    {
        // Arrange
        JsonDocument jsonDoc = JsonDocument.Parse(@"{""name"": ""John""}");
        JsonElement element = jsonDoc.RootElement;

        // Act
        JsonType result = JsonStructureParser.Parse(element);

        // Assert
        Assert.That(result, Is.InstanceOf<JsonObjectType>());
        JsonObjectType obj = (JsonObjectType)result;
        Assert.That(obj.Properties, Has.Count.EqualTo(1));
        Assert.That(obj.Properties.ContainsKey("name"), Is.True);
        Assert.That(obj.Properties["name"], Is.InstanceOf<JsonPrimitiveType>());
        JsonPrimitiveType nameProp = (JsonPrimitiveType)obj.Properties["name"];
        Assert.That(nameProp.Kind, Is.EqualTo(JsonValueKind.String));
    }

    [Test]
    public void Parse_WithObjectWithMultipleProperties_ReturnsObjectTypeWithAllProperties()
    {
        // Arrange
        JsonDocument jsonDoc = JsonDocument.Parse(@"{""name"": ""John"", ""age"": 30, ""active"": true}");
        JsonElement element = jsonDoc.RootElement;

        // Act
        JsonType result = JsonStructureParser.Parse(element);

        // Assert
        Assert.That(result, Is.InstanceOf<JsonObjectType>());
        JsonObjectType obj = (JsonObjectType)result;
        Assert.That(obj.Properties, Has.Count.EqualTo(3));
        
        Assert.That(obj.Properties.ContainsKey("name"), Is.True);
        Assert.That(obj.Properties["name"], Is.InstanceOf<JsonPrimitiveType>());
        Assert.That(((JsonPrimitiveType)obj.Properties["name"]).Kind, Is.EqualTo(JsonValueKind.String));
        
        Assert.That(obj.Properties.ContainsKey("age"), Is.True);
        Assert.That(obj.Properties["age"], Is.InstanceOf<JsonPrimitiveType>());
        Assert.That(((JsonPrimitiveType)obj.Properties["age"]).Kind, Is.EqualTo(JsonValueKind.Number));
        
        Assert.That(obj.Properties.ContainsKey("active"), Is.True);
        Assert.That(obj.Properties["active"], Is.InstanceOf<JsonPrimitiveType>());
        Assert.That(((JsonPrimitiveType)obj.Properties["active"]).Kind, Is.EqualTo(JsonValueKind.True));
    }

    [Test]
    public void Parse_WithEmptyArray_ReturnsArrayTypeWithUndefinedItemType()
    {
        // Arrange
        JsonDocument jsonDoc = JsonDocument.Parse("[]");
        JsonElement element = jsonDoc.RootElement;

        // Act
        JsonType result = JsonStructureParser.Parse(element);

        // Assert
        Assert.That(result, Is.InstanceOf<JsonArrayType>());
        JsonArrayType arr = (JsonArrayType)result;
        Assert.That(arr.ItemType, Is.InstanceOf<JsonPrimitiveType>());
        JsonPrimitiveType itemType = (JsonPrimitiveType)arr.ItemType;
        Assert.That(itemType.Kind, Is.EqualTo(JsonValueKind.Undefined));
    }

    [Test]
    public void Parse_WithArrayOfNumbers_ReturnsArrayTypeWithNumberItemType()
    {
        // Arrange
        JsonDocument jsonDoc = JsonDocument.Parse("[1, 2, 3]");
        JsonElement element = jsonDoc.RootElement;

        // Act
        JsonType result = JsonStructureParser.Parse(element);

        // Assert
        Assert.That(result, Is.InstanceOf<JsonArrayType>());
        JsonArrayType arr = (JsonArrayType)result;
        Assert.That(arr.ItemType, Is.InstanceOf<JsonPrimitiveType>());
        JsonPrimitiveType itemType = (JsonPrimitiveType)arr.ItemType;
        Assert.That(itemType.Kind, Is.EqualTo(JsonValueKind.Number));
    }

    [Test]
    public void Parse_WithArrayOfStrings_ReturnsArrayTypeWithStringItemType()
    {
        // Arrange
        JsonDocument jsonDoc = JsonDocument.Parse(@"[""a"", ""b"", ""c""]");
        JsonElement element = jsonDoc.RootElement;

        // Act
        JsonType result = JsonStructureParser.Parse(element);

        // Assert
        Assert.That(result, Is.InstanceOf<JsonArrayType>());
        JsonArrayType arr = (JsonArrayType)result;
        Assert.That(arr.ItemType, Is.InstanceOf<JsonPrimitiveType>());
        JsonPrimitiveType itemType = (JsonPrimitiveType)arr.ItemType;
        Assert.That(itemType.Kind, Is.EqualTo(JsonValueKind.String));
    }

    [Test]
    public void Parse_WithArrayOfObjects_ReturnsArrayTypeWithObjectItemType()
    {
        // Arrange
        JsonDocument jsonDoc = JsonDocument.Parse(@"[{""id"": 1}, {""id"": 2}]");
        JsonElement element = jsonDoc.RootElement;

        // Act
        JsonType result = JsonStructureParser.Parse(element);

        // Assert
        Assert.That(result, Is.InstanceOf<JsonArrayType>());
        JsonArrayType arr = (JsonArrayType)result;
        Assert.That(arr.ItemType, Is.InstanceOf<JsonObjectType>());
        JsonObjectType itemType = (JsonObjectType)arr.ItemType;
        Assert.That(itemType.Properties, Has.Count.EqualTo(1));
        Assert.That(itemType.Properties.ContainsKey("id"), Is.True);
    }

    [Test]
    public void Parse_WithArrayOfArrays_ReturnsNestedArrayType()
    {
        // Arrange
        JsonDocument jsonDoc = JsonDocument.Parse("[[1, 2], [3, 4]]");
        JsonElement element = jsonDoc.RootElement;

        // Act
        JsonType result = JsonStructureParser.Parse(element);

        // Assert
        Assert.That(result, Is.InstanceOf<JsonArrayType>());
        JsonArrayType outerArr = (JsonArrayType)result;
        Assert.That(outerArr.ItemType, Is.InstanceOf<JsonArrayType>());
        JsonArrayType innerArr = (JsonArrayType)outerArr.ItemType;
        Assert.That(innerArr.ItemType, Is.InstanceOf<JsonPrimitiveType>());
        JsonPrimitiveType primitiveType = (JsonPrimitiveType)innerArr.ItemType;
        Assert.That(primitiveType.Kind, Is.EqualTo(JsonValueKind.Number));
    }

    [Test]
    public void Parse_WithArrayOfEmptyArrays_ReturnsNestedArrayWithUndefinedInnerType()
    {
        // Arrange
        JsonDocument jsonDoc = JsonDocument.Parse("[[], []]");
        JsonElement element = jsonDoc.RootElement;

        // Act
        JsonType result = JsonStructureParser.Parse(element);

        // Assert
        Assert.That(result, Is.InstanceOf<JsonArrayType>());
        JsonArrayType outerArr = (JsonArrayType)result;
        Assert.That(outerArr.ItemType, Is.InstanceOf<JsonArrayType>());
        JsonArrayType innerArr = (JsonArrayType)outerArr.ItemType;
        Assert.That(innerArr.ItemType, Is.InstanceOf<JsonPrimitiveType>());
        JsonPrimitiveType primitiveType = (JsonPrimitiveType)innerArr.ItemType;
        Assert.That(primitiveType.Kind, Is.EqualTo(JsonValueKind.Undefined));
    }

    [Test]
    public void Parse_WithNestedObjects_ReturnsObjectTypeWithNestedObjectProperty()
    {
        // Arrange
        JsonDocument jsonDoc = JsonDocument.Parse(@"{""user"": {""name"": ""John"", ""age"": 30}}");
        JsonElement element = jsonDoc.RootElement;

        // Act
        JsonType result = JsonStructureParser.Parse(element);

        // Assert
        Assert.That(result, Is.InstanceOf<JsonObjectType>());
        JsonObjectType obj = (JsonObjectType)result;
        Assert.That(obj.Properties, Has.Count.EqualTo(1));
        Assert.That(obj.Properties.ContainsKey("user"), Is.True);
        Assert.That(obj.Properties["user"], Is.InstanceOf<JsonObjectType>());
        
        JsonObjectType userObj = (JsonObjectType)obj.Properties["user"];
        Assert.That(userObj.Properties, Has.Count.EqualTo(2));
        Assert.That(userObj.Properties.ContainsKey("name"), Is.True);
        Assert.That(userObj.Properties.ContainsKey("age"), Is.True);
    }

    [Test]
    public void Parse_WithComplexNestedStructure_ParsesCorrectly()
    {
        // Arrange
        string json = @"{
            ""config"": {
                ""enabled"": true,
                ""options"": [
                    {""name"": ""opt1"", ""value"": 1},
                    {""name"": ""opt2"", ""value"": 2}
                ]
            }
        }";
        JsonDocument jsonDoc = JsonDocument.Parse(json);
        JsonElement element = jsonDoc.RootElement;

        // Act
        JsonType result = JsonStructureParser.Parse(element);

        // Assert
        Assert.That(result, Is.InstanceOf<JsonObjectType>());
        JsonObjectType root = (JsonObjectType)result;
        Assert.That(root.Properties.ContainsKey("config"), Is.True);
        
        JsonObjectType config = (JsonObjectType)root.Properties["config"];
        Assert.That(config.Properties, Has.Count.EqualTo(2));
        Assert.That(config.Properties.ContainsKey("enabled"), Is.True);
        Assert.That(config.Properties.ContainsKey("options"), Is.True);
        
        JsonArrayType options = (JsonArrayType)config.Properties["options"];
        Assert.That(options.ItemType, Is.InstanceOf<JsonObjectType>());
        
        JsonObjectType optionItem = (JsonObjectType)options.ItemType;
        Assert.That(optionItem.Properties, Has.Count.EqualTo(2));
        Assert.That(optionItem.Properties.ContainsKey("name"), Is.True);
        Assert.That(optionItem.Properties.ContainsKey("value"), Is.True);
    }
}
