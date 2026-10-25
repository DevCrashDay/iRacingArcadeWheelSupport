namespace WheelBridge.Core;

/// <summary>
/// Pure conversion functions from raw device values to gamepad values.
/// Kept free of SDL/ViGEm so they can be unit tested on any OS.
/// </summary>
public static class AxisMath
{
    /// <summary>Raw signed 16-bit axis to -1..1.</summary>
    public static double Normalize(int raw) =>
        Math.Clamp(raw < 0 ? raw / 32768.0 : raw / 32767.0, -1.0, 1.0);

    /// <summary>Steering axis to stick deflection in -1..1.</summary>
    public static double Steering(int raw, SteeringConfig cfg)
    {
        var s = Normalize(raw);
        if (cfg.Invert) s = -s;

        // Scale so that half the steering range (one side of lock) reaches full deflection.
        var range = cfg.SteeringRangeDegrees <= 0 ? cfg.WheelRotationDegrees : cfg.SteeringRangeDegrees;
        var degrees = s * cfg.WheelRotationDegrees / 2.0;
        var v = Math.Clamp(degrees / (range / 2.0), -1.0, 1.0);

        var sign = Math.Sign(v);
        var mag = Math.Abs(v);

        var dz = Math.Clamp(cfg.Deadzone, 0, 0.99);
        if (mag <= dz) return 0;
        mag = (mag - dz) / (1 - dz);

        if (cfg.Linearity > 0 && cfg.Linearity != 1.0)
            mag = Math.Pow(mag, cfg.Linearity);

        var ad = Math.Clamp(cfg.AntiDeadzone, 0, 0.99);
        if (ad > 0) mag = ad + (1 - ad) * mag;

        return sign * Math.Min(mag, 1.0);
    }

    /// <summary>Pedal axis to 0..1 press amount.</summary>
    public static double Pedal(int raw, PedalConfig cfg)
    {
        double span = cfg.RawMax - cfg.RawMin;
        if (span == 0) return 0;

        var v = Math.Clamp((raw - cfg.RawMin) / span, 0.0, 1.0);
        if (cfg.Invert) v = 1 - v;

        var dz = Math.Clamp(cfg.Deadzone, 0, 0.99);
        var sat = Math.Clamp(cfg.Saturation, 0, 0.99 - dz);
        if (v <= dz) return 0;
        if (v >= 1 - sat) return 1;
        v = (v - dz) / (1 - dz - sat);

        if (cfg.Gamma > 0 && cfg.Gamma != 1.0)
            v = Math.Pow(v, cfg.Gamma);

        return Math.Clamp(v, 0.0, 1.0);
    }

    /// <summary>-1..1 to Xbox thumbstick value.</summary>
    public static short ToStick(double v) =>
        (short)Math.Round(Math.Clamp(v, -1.0, 1.0) * (v < 0 ? 32768 : 32767));

    /// <summary>0..1 to Xbox trigger value.</summary>
    public static byte ToTrigger(double v) =>
        (byte)Math.Round(Math.Clamp(v, 0.0, 1.0) * 255);
}
