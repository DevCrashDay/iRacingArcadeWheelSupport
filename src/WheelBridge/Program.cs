using WheelBridge;
using WheelBridge.Core;

const string Usage = """
    iRacing Arcade Wheel Bridge

    Usage:
      WheelBridge list                 List connected wheels, pedals and controllers
      WheelBridge monitor [name]       Show live axis/button values (to find axis numbers)
      WheelBridge run [profile.json]   Start the bridge (default: profiles/default.json)
    """;

var command = args.Length > 0 ? args[0].ToLowerInvariant() : "run";

try
{
    switch (command)
    {
        case "list":
            using (var devices = new DeviceManager())
            {
                if (devices.Devices.Count == 0) Console.WriteLine("No devices found.");
            }
            return 0;

        case "monitor":
            Monitor(args.Length > 1 ? args[1] : null);
            return 0;

        case "run":
            Run(args.Length > 1 ? args[1] : Path.Combine(AppContext.BaseDirectory, "profiles", "default.json"));
            return 0;

        default:
            Console.WriteLine(Usage);
            return command is "help" or "-h" or "--help" ? 0 : 1;
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}

static void Run(string profilePath)
{
    var profile = Profile.Load(profilePath);
    Console.WriteLine($"Profile: {profile.Name} ({profilePath})");

    using var devices = new DeviceManager();
    WarnMissing(devices, profile.Steering.Device, "steering");
    WarnMissing(devices, profile.Throttle.Device, "throttle");
    WarnMissing(devices, profile.Brake.Device, "brake");

    using var pad = new VirtualPad();
    Console.WriteLine("Virtual Xbox controller connected. Start iRacing Arcade. Press Ctrl+C to stop.");

    using var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };
    new Bridge(profile, devices, pad).Run(cts.Token);
}

static void WarnMissing(DeviceManager devices, string name, string role)
{
    if (devices.Find(name) is null)
        Console.WriteLine($"Warning: no device matching \"{name}\" for {role}. Edit the profile or run 'WheelBridge list'.");
}

static void Monitor(string? filter)
{
    using var devices = new DeviceManager();
    using var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };
    Console.WriteLine("Move each axis and press each button. Ctrl+C to stop.\n");
    var top = Console.CursorTop;

    while (!cts.IsCancellationRequested)
    {
        devices.Update();
        Console.SetCursorPosition(0, top);
        foreach (var d in devices.Devices.Where(d => filter is null || d.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)))
        {
            Console.WriteLine($"{d.Name}".PadRight(Console.BufferWidth - 1));
            for (var a = 0; a < d.Axes; a++)
                Console.WriteLine($"  axis {a,2}: {DeviceManager.Axis(d, a),7}".PadRight(40));
            var down = Enumerable.Range(0, d.Buttons).Where(b => DeviceManager.Button(d, b));
            Console.WriteLine($"  buttons down: {string.Join(", ", down)}".PadRight(Console.BufferWidth - 1));
            for (var h = 0; h < d.Hats; h++)
                Console.WriteLine($"  hat {h}: {DeviceManager.Hat(d, h)}".PadRight(40));
            Console.WriteLine();
        }
        Thread.Sleep(50);
    }
}
