# iRacing Arcade Wheel Support

Community-made steering wheel and pedal support for **iRacing Arcade** (PC / Steam).

iRacing Arcade only supports gamepads and keyboard, and the developers have said wheel
support will not be added. This project fills that gap.

> Unofficial fan project. Not affiliated with or endorsed by iRacing.com Motorsport
> Simulations or Original Fire Games. No game files are included or modified.

## How it works

**Wheel Bridge** (Phase 1, this repo today) is a small Windows app that:

1. reads your wheel, pedals and buttons (any DirectInput device: Logitech, Thrustmaster,
   Fanatec, Moza, Simagic, separate pedal boxes...) through SDL2,
2. converts them using a JSON profile (steering range, curves, deadzones, pedal calibration),
3. feeds the game a **virtual Xbox 360 controller** through the ViGEmBus driver.

The game just sees a gamepad, so nothing is injected into it and it is safe to use online.

| Game control | Comes from |
| --- | --- |
| Steering (left stick X) | Wheel axis, scaled by `steeringRangeDegrees` |
| Throttle (right trigger) | Throttle pedal |
| Brake (left trigger) | Brake pedal |
| A/B/X/Y, bumpers, Start/Back, D-pad | Any wheel buttons / hat |

A later **Phase 2** may add an in-game plugin for tighter steering and force feedback.

## Install

1. Install the [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) (x64).
2. Install [ViGEmBus](https://github.com/nefarius/ViGEmBus/releases) and reboot.
3. Optional but recommended: install [HidHide](https://github.com/nefarius/HidHide/releases)
   and hide your wheel from `iRacingArcade.exe`, so the game only sees the virtual pad.
4. Download the latest `WheelBridge` build from the Actions/Releases page and unzip it.

## Set up your wheel

```text
WheelBridge list       # shows device names
WheelBridge monitor    # live axis/button numbers - turn the wheel, press each pedal
```

Copy `profiles/default.json` to e.g. `profiles/my-wheel.json` and fill in:

- `device`: any part of your wheel's name from `list` (e.g. `"G29"`). Pedals can be a different device.
- `axis` numbers for steering, throttle and brake from `monitor`.
- `rawMin` / `rawMax`: the pedal value when released and fully pressed (from `monitor`).
- `wheelRotationDegrees`: what your wheel driver is set to (e.g. 900).
- `steeringRangeDegrees`: how far you want to turn for full lock in game. 180 to 360 suits this game.

Then run:

```text
WheelBridge run profiles\my-wheel.json
```

Start the bridge before the game, then pick the controller in game as usual.

### Tuning

| Setting | What it does |
| --- | --- |
| `steering.deadzone` | Ignore small movements around centre |
| `steering.antiDeadzone` | Skip past the game's own stick deadzone (try 0.05 to 0.15 if the car ignores small inputs) |
| `steering.linearity` | Above 1 = calmer near centre, below 1 = sharper |
| pedal `deadzone` / `saturation` | Ignore the first / last bit of pedal travel |
| pedal `gamma` | Above 1 = finer control at light pressure (good for brakes) |

Please share working profiles for your wheel in `profiles/` via a pull request.

## Build from source

```text
dotnet build WheelBridge.sln
dotnet test WheelBridge.sln
dotnet run --project src/WheelBridge -- monitor
```

## Project layout

| Path | Contents |
| --- | --- |
| `src/WheelBridge.Core` | Profile model and axis math (no Windows dependencies, unit tested) |
| `src/WheelBridge` | The app: SDL2 device reading, ViGEm virtual pad, main loop |
| `tests/WheelBridge.Core.Tests` | Unit tests for the axis math and profile parsing |
| `profiles/` | Mapping profiles |

## License

MIT, see [LICENSE](LICENSE).
