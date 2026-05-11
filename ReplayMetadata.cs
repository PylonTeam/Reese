using System;
using System.Text;
using System.Text.Json;

namespace Reese;

/// <summary>Embedded JSON in v2 .reese files (see ReplayFile header).</summary>
public sealed class ReplayMetadata
{
    public int FormatVersion { get; set; } = 2;
    public string CreatedUtc { get; set; } = string.Empty;
    public string WorldName { get; set; } = string.Empty;
    public string[] ModNames { get; set; } = [];
    public int TickRate { get; set; } = 60;
    public uint DurationTicks { get; set; }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };


    public static ReplayMetadata FromJsonBytes(byte[] raw)
    {
        try
        {
            string s = Encoding.UTF8.GetString(raw).TrimEnd('\0', ' ', '\r', '\n', '\t');
            if (string.IsNullOrWhiteSpace(s))
                return new ReplayMetadata();
            return JsonSerializer.Deserialize<ReplayMetadata>(s, JsonOptions) ?? new ReplayMetadata();
        }
        catch
        {
            return new ReplayMetadata();
        }
    }
}
