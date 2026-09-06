using System.Text.Json;
using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Common;

namespace ProgressiveBotSystem.Helpers;

public class MongoIdDictionaryKeyConverter : JsonConverter<MongoId>
{
    public override MongoId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var str = reader.GetString();
        return str == null ? throw new JsonException("Expected string for MongoId") : new MongoId(str);
    }

    public override void Write(Utf8JsonWriter writer, MongoId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }

    public override MongoId ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var str = reader.GetString();
        return str == null ? throw new JsonException("Expected string for MongoId dictionary key") : new MongoId(str);
    }

    public override void WriteAsPropertyName(Utf8JsonWriter writer, MongoId value, JsonSerializerOptions options)
    {
        writer.WritePropertyName(value.ToString());
    }
}
