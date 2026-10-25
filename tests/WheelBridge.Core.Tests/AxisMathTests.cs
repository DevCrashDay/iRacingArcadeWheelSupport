using WheelBridge.Core;
using Xunit;

namespace WheelBridge.Core.Tests;

public class AxisMathTests
{
    [Theory]
    [InlineData(0, 0.0)]
    [InlineData(short.MaxValue, 1.0)]
    [InlineData(short.MinValue, -1.0)]
    public void Normalize_maps_full_range(int raw, double expected) =>
        Assert.Equal(expected, AxisMath.Normalize(raw), 6);

    [Fact]
    public void Steering_reaches_full_lock_at_half_the_steering_range()
    {
        var cfg = new SteeringConfig { WheelRotationDegrees = 900, SteeringRangeDegrees = 270 };
        // 135 degrees of a 450 degree half-rotation = 0.3 of raw travel.
        var raw = (int)(0.3 * short.MaxValue);
        Assert.Equal(1.0, AxisMath.Steering(raw, cfg), 3);
        Assert.Equal(0.5, AxisMath.Steering(raw / 2, cfg), 2);
        Assert.Equal(1.0, AxisMath.Steering(short.MaxValue, cfg), 6);
        Assert.Equal(-1.0, AxisMath.Steering(short.MinValue, cfg), 6);
    }

    [Fact]
    public void Steering_invert_flips_direction()
    {
        var cfg = new SteeringConfig { WheelRotationDegrees = 270, SteeringRangeDegrees = 270, Invert = true };
        Assert.Equal(-1.0, AxisMath.Steering(short.MaxValue, cfg), 6);
    }

    [Fact]
    public void Steering_deadzone_and_antideadzone()
    {
        var cfg = new SteeringConfig { WheelRotationDegrees = 270, SteeringRangeDegrees = 270, Deadzone = 0.1, AntiDeadzone = 0.2 };
        Assert.Equal(0.0, AxisMath.Steering((int)(0.05 * short.MaxValue), cfg), 6);
        // Just outside the deadzone jumps to the anti-deadzone floor.
        Assert.InRange(AxisMath.Steering((int)(0.11 * short.MaxValue), cfg), 0.2, 0.21);
        Assert.Equal(1.0, AxisMath.Steering(short.MaxValue, cfg), 6);
    }

    [Fact]
    public void Pedal_calibration_invert_and_deadzone()
    {
        // Typical combined-range pedal: released = +32767, pressed = -32768.
        var cfg = new PedalConfig { RawMin = short.MinValue, RawMax = short.MaxValue, Invert = true, Deadzone = 0.05 };
        Assert.Equal(0.0, AxisMath.Pedal(short.MaxValue, cfg), 6);
        Assert.Equal(1.0, AxisMath.Pedal(short.MinValue, cfg), 6);
        Assert.Equal(0.0, AxisMath.Pedal(short.MaxValue - 1000, cfg), 6);
        Assert.InRange(AxisMath.Pedal(0, cfg), 0.47, 0.48);
    }

    [Fact]
    public void Pedal_saturation_and_gamma()
    {
        var cfg = new PedalConfig { RawMin = 0, RawMax = 1000, Deadzone = 0, Saturation = 0.1, Gamma = 2 };
        Assert.Equal(1.0, AxisMath.Pedal(950, cfg), 6);
        // 225 / 900 usable travel = 0.25, squared = 0.0625.
        Assert.Equal(0.0625, AxisMath.Pedal(225, cfg), 6);
    }

    [Theory]
    [InlineData(1.0, short.MaxValue)]
    [InlineData(-1.0, short.MinValue)]
    [InlineData(0.0, 0)]
    public void ToStick_covers_full_range(double v, short expected) =>
        Assert.Equal(expected, AxisMath.ToStick(v));

    [Fact]
    public void ToTrigger_clamps()
    {
        Assert.Equal(255, AxisMath.ToTrigger(2));
        Assert.Equal(0, AxisMath.ToTrigger(-1));
    }

    [Fact]
    public void Default_profile_parses()
    {
        var profile = Profile.Load(Path.Combine(AppContext.BaseDirectory, "profiles", "default.json"));
        Assert.Equal(270, profile.Steering.SteeringRangeDegrees);
        Assert.NotEmpty(profile.Buttons);
    }
}
