using System.Text.Json;
using System.Text.Json.Serialization;

namespace WheelBridge.Core;

/// <summary>
/// A mapping profile: which physical device axis/button drives which virtual gamepad control.
/// Devices are matched by a case-insensitive substring of the name SDL reports
/// (run <c>WheelBridge list</c> to see the names).
/// </summary>
public sealed class Profile
{
    public string Name { get; set; } = "Default";

    /// <summary>How often the bridge reads devices and sends a gamepad report.</summary>
    public int UpdateRateHz { get; set; } = 500;

    public SteeringConfig Steering { get; set; } = new();
    public PedalConfig Throttle { get; set; } = new() { Axis = 1 };
    public PedalConfig Brake { get; set; } = new() { Axis = 2 };

    public List<ButtonBinding> Buttons { get; set; } = new();
    public HatBinding? DPad { get; set; }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static Profile Load(string path) =>
        JsonSerializer.Deserialize<Profile>(File.ReadAllText(path), JsonOptions)
        ?? throw new InvalidDataException($"Profile '{path}' is empty.");

    public static Profile Parse(string json) =>
        JsonSerializer.Deserialize<Profile>(json, JsonOptions)
        ?? throw new InvalidDataException("Profile JSON is empty.");

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);
}

public abstract class AxisSource
{
    /// <summary>Substring of the device name, e.g. "G29" or "Fanatec".</summary>
    public string Device { get; set; } = "";

    /// <summary>Zero-based SDL axis index on that device.</summary>
    public int Axis { get; set; }

    public bool Invert { get; set; }
}

public sealed class SteeringConfig : AxisSource
{
    /// <summary>Full lock-to-lock rotation of the physical wheel as set in its driver (e.g. 900).</summary>
    public double WheelRotationDegrees { get; set; } = 900;

    /// <summary>Lock-to-lock rotation that maps to full stick deflection (e.g. 270).</summary>
    public double SteeringRangeDegrees { get; set; } = 270;

    /// <summary>Fraction of output (0..1) around centre treated as zero.</summary>
    public double Deadzone { get; set; }

    /// <summary>Minimum output (0..1) once outside the deadzone, to cancel the game's own stick deadzone.</summary>
    public double AntiDeadzone { get; set; }

    /// <summary>Response curve exponent: 1 = linear, &gt;1 = less sensitive near centre.</summary>
    public double Linearity { get; set; } = 1.0;
}

public sealed class PedalConfig : AxisSource
{
    /// <summary>Raw SDL value with the pedal released (before invert).</summary>
    public int RawMin { get; set; } = short.MinValue;

    /// <summary>Raw SDL value with the pedal fully pressed (before invert).</summary>
    public int RawMax { get; set; } = short.MaxValue;

    /// <summary>Fraction of travel (0..1) at the start ignored.</summary>
    public double Deadzone { get; set; } = 0.02;

    /// <summary>Fraction of travel (0..1) at the end that already counts as 100%.</summary>
    public double Saturation { get; set; }

    /// <summary>Response curve exponent: 1 = linear, &gt;1 = finer control at light pressure.</summary>
    public double Gamma { get; set; } = 1.0;
}

public enum PadButton
{
    A, B, X, Y,
    LeftShoulder, RightShoulder,
    Back, Start, Guide,
    LeftThumb, RightThumb,
    Up, Down, Left, Right,
}

public sealed class ButtonBinding
{
    public string Device { get; set; } = "";
    public int Button { get; set; }
    public PadButton Target { get; set; }
}

public sealed class HatBinding
{
    public string Device { get; set; } = "";
    public int Hat { get; set; }
}
