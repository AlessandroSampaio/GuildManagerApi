using System.Text.Json;
using System.Text.Json.Serialization;

namespace GuildManagerApi.Application.Converters;

public class JsonAproximatedRankingConverter : JsonConverter<Int32>
{
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if(reader.TokenType == JsonTokenType.String)
        {
            string? value = reader.GetString();
            int result = 0;
            if (value?.StartsWith('~') ?? false)
            {
                var convertedValue = int.Parse(value[1..]);
                return convertedValue;
            }
            Console.WriteLine($"Parsing value {value} to {typeToConvert} with result {result}");
            return result;
        }else if(reader.TokenType == JsonTokenType.Number)
        {
            reader.TryGetInt32(out int intValue);
            return intValue;
        }
        return 0;
       
    }

    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());

}
