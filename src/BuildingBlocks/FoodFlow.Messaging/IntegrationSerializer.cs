using System.Text.Json;
using System.Text.Json.Serialization;

namespace FoodFlow.Messaging;

public static class IntegrationSerializer
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);

    public static string Serialize(object value, Type type) => JsonSerializer.Serialize(value, type, Options);

    public static object Deserialize(string json, Type type) =>
        JsonSerializer.Deserialize(json, type, Options)
        ?? throw new PermanentMessagingException($"Payload could not be deserialized as {type.FullName}.");
}
