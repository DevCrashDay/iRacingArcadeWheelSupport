using SDL2;

namespace WheelBridge;

/// <summary>
/// Opens every joystick SDL can see (wheels, pedals, shifters) and exposes their raw state.
/// </summary>
public sealed class DeviceManager : IDisposable
{
    public sealed record Device(int InstanceId, string Name, IntPtr Handle, int Axes, int Buttons, int Hats);

    private readonly Dictionary<int, Device> _devices = new();

    public IReadOnlyCollection<Device> Devices => _devices.Values;

    public DeviceManager()
    {
        // The game window has focus while we run, so input must keep flowing in the background.
        SDL.SDL_SetHint(SDL.SDL_HINT_JOYSTICK_ALLOW_BACKGROUND_EVENTS, "1");

        if (SDL.SDL_Init(SDL.SDL_INIT_JOYSTICK | SDL.SDL_INIT_HAPTIC) != 0)
            throw new InvalidOperationException($"SDL_Init failed: {SDL.SDL_GetError()}");

        Refresh();
    }

    /// <summary>Opens newly connected devices and drops removed ones.</summary>
    public void Refresh()
    {
        foreach (var dev in _devices.Values.Where(d => SDL.SDL_JoystickGetAttached(d.Handle) != SDL.SDL_bool.SDL_TRUE).ToList())
        {
            SDL.SDL_JoystickClose(dev.Handle);
            _devices.Remove(dev.InstanceId);
            Console.WriteLine($"Disconnected: {dev.Name}");
        }

        for (var i = 0; i < SDL.SDL_NumJoysticks(); i++)
        {
            var id = SDL.SDL_JoystickGetDeviceInstanceID(i);
            if (_devices.ContainsKey(id)) continue;

            var handle = SDL.SDL_JoystickOpen(i);
            if (handle == IntPtr.Zero) continue;

            var dev = new Device(
                id,
                SDL.SDL_JoystickName(handle) ?? $"Joystick {i}",
                handle,
                SDL.SDL_JoystickNumAxes(handle),
                SDL.SDL_JoystickNumButtons(handle),
                SDL.SDL_JoystickNumHats(handle));
            _devices[id] = dev;
            Console.WriteLine($"Connected: {dev.Name} ({dev.Axes} axes, {dev.Buttons} buttons, {dev.Hats} hats)");
        }
    }

    /// <summary>Pumps SDL and handles hot-plug. Call once per tick before reading values.</summary>
    public void Update()
    {
        var changed = false;
        while (SDL.SDL_PollEvent(out var e) == 1)
        {
            if (e.type is SDL.SDL_EventType.SDL_JOYDEVICEADDED or SDL.SDL_EventType.SDL_JOYDEVICEREMOVED)
                changed = true;
        }
        if (changed) Refresh();
        SDL.SDL_JoystickUpdate();
    }

    public Device? Find(string nameFragment) =>
        _devices.Values.FirstOrDefault(d => d.Name.Contains(nameFragment, StringComparison.OrdinalIgnoreCase));

    public static int Axis(Device d, int axis) =>
        axis >= 0 && axis < d.Axes ? SDL.SDL_JoystickGetAxis(d.Handle, axis) : 0;

    public static bool Button(Device d, int button) =>
        button >= 0 && button < d.Buttons && SDL.SDL_JoystickGetButton(d.Handle, button) != 0;

    public static byte Hat(Device d, int hat) =>
        hat >= 0 && hat < d.Hats ? SDL.SDL_JoystickGetHat(d.Handle, hat) : SDL.SDL_HAT_CENTERED;

    public void Dispose()
    {
        foreach (var dev in _devices.Values) SDL.SDL_JoystickClose(dev.Handle);
        _devices.Clear();
        SDL.SDL_Quit();
    }
}
