using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using WheelBridge.Core;

namespace WheelBridge;

/// <summary>A virtual Xbox 360 controller exposed through the ViGEmBus driver.</summary>
public sealed class VirtualPad : IDisposable
{
    private readonly ViGEmClient _client;
    private readonly IXbox360Controller _pad;

    public VirtualPad()
    {
        try
        {
            _client = new ViGEmClient();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Could not connect to ViGEmBus. Install it from https://github.com/nefarius/ViGEmBus/releases and reboot.", ex);
        }

        _pad = _client.CreateXbox360Controller();
        _pad.AutoSubmitReport = false;
        _pad.Connect();
    }

    public void SetSteering(short value) => _pad.SetAxisValue(Xbox360Axis.LeftThumbX, value);
    public void SetThrottle(byte value) => _pad.SetSliderValue(Xbox360Slider.RightTrigger, value);
    public void SetBrake(byte value) => _pad.SetSliderValue(Xbox360Slider.LeftTrigger, value);

    public void SetButton(PadButton button, bool pressed) => _pad.SetButtonState(Map(button), pressed);

    public void Submit() => _pad.SubmitReport();

    private static Xbox360Button Map(PadButton b) => b switch
    {
        PadButton.A => Xbox360Button.A,
        PadButton.B => Xbox360Button.B,
        PadButton.X => Xbox360Button.X,
        PadButton.Y => Xbox360Button.Y,
        PadButton.LeftShoulder => Xbox360Button.LeftShoulder,
        PadButton.RightShoulder => Xbox360Button.RightShoulder,
        PadButton.Back => Xbox360Button.Back,
        PadButton.Start => Xbox360Button.Start,
        PadButton.Guide => Xbox360Button.Guide,
        PadButton.LeftThumb => Xbox360Button.LeftThumb,
        PadButton.RightThumb => Xbox360Button.RightThumb,
        PadButton.Up => Xbox360Button.Up,
        PadButton.Down => Xbox360Button.Down,
        PadButton.Left => Xbox360Button.Left,
        PadButton.Right => Xbox360Button.Right,
        _ => throw new ArgumentOutOfRangeException(nameof(b), b, null),
    };

    public void Dispose()
    {
        _pad.Disconnect();
        _client.Dispose();
    }
}
