using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.WebUtilities;

namespace DonationApi.DTOs;

/// <summary>
/// Position of the last donation the client has seen: (CreatedAt, Id) + the sort it belongs to.
/// Sent to the client as an opaque Base64Url string, e.g. "eyJ0Ijo2Mzg5...".
/// Ticks keep the full datetime2(7) precision, so no row is skipped or repeated.
/// </summary>
public readonly record struct DonationCursor(
    [property: JsonPropertyName("t")] long Ticks,
    [property: JsonPropertyName("i")] int Id,
    [property: JsonPropertyName("s")] DonationSort Sort)
{
    [JsonIgnore]
    public DateTime CreatedAt => new(Ticks, DateTimeKind.Utc);

    public static string Encode(DateTime createdAt, int id, DonationSort sort) =>
        WebEncoders.Base64UrlEncode(
            JsonSerializer.SerializeToUtf8Bytes(new DonationCursor(createdAt.Ticks, id, sort)));

    public static bool TryDecode(string? value, out DonationCursor cursor)
    {
        cursor = default;
        if (string.IsNullOrWhiteSpace(value)) return false;

        try
        {
            cursor = JsonSerializer.Deserialize<DonationCursor>(WebEncoders.Base64UrlDecode(value));
            return cursor.Id > 0
                && cursor.Ticks > DateTime.MinValue.Ticks
                && cursor.Ticks < DateTime.MaxValue.Ticks
                && Enum.IsDefined(cursor.Sort);
        }
        catch (Exception ex) when (ex is FormatException or JsonException)
        {
            return false;
        }
    }
}
