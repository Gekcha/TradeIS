using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using TradeIS.Models;

public class TradePointConverter : JsonConverter<TradePoint>
{
    public override TradePoint Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (!root.TryGetProperty("Type", out var typeProp))
            throw new Exception("TradePoint missing Type field");

        var type = typeProp.GetString();

        TradePoint result = type switch
        {
            "Shop" => JsonSerializer.Deserialize<Shop>(root.GetRawText(), options),
            "Kiosk" => JsonSerializer.Deserialize<Kiosk>(root.GetRawText(), options),
            "Stall" => JsonSerializer.Deserialize<Stall>(root.GetRawText(), options),
            "DepartmentStore" => JsonSerializer.Deserialize<DepartmentStore>(root.GetRawText(), options),
            _ => throw new Exception($"Unknown TradePoint type: {type}")
        };

        // ===== ЗАЩИТА ОТ NULL =====
        if (result is Shop shop)
            shop.Halls ??= new List<Hall>();

        if (result is DepartmentStore ds)
            ds.Halls ??= new List<Hall>();

        return result!;
    }

    public override void Write(Utf8JsonWriter writer, TradePoint value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WriteString("Type", value.GetType().Name);
        writer.WriteNumber("Id", value.Id);
        writer.WriteString("Name", value.Name);
        writer.WriteNumber("Size", value.Size);
        writer.WriteNumber("Rent", value.Rent);
        writer.WriteNumber("Utilities", value.Utilities);
        writer.WriteNumber("Counters", value.Counters);

        if (value is Shop shop)
        {
            writer.WritePropertyName("Halls");
            JsonSerializer.Serialize(writer, shop.Halls, options);
        }

        if (value is DepartmentStore ds)
        {
            writer.WritePropertyName("Halls");
            JsonSerializer.Serialize(writer, ds.Halls, options);
        }

        writer.WriteEndObject();
    }
}