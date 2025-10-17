using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using SharpDX.DirectInput;

Console.Title = "Controller Input Reader";

var directInput = new DirectInput();

Guid joystickGuid = Guid.Empty;

foreach (var deviceInstance in directInput.GetDevices(DeviceType.Gamepad, DeviceEnumerationFlags.AllDevices))
{
    joystickGuid = deviceInstance.InstanceGuid;
    break;
}

if (joystickGuid == Guid.Empty)
{
    foreach (var deviceInstance in directInput.GetDevices(DeviceType.Joystick, DeviceEnumerationFlags.AllDevices))
    {
        joystickGuid = deviceInstance.InstanceGuid;
        break;
    }
}

if (joystickGuid == Guid.Empty)
{
    Console.WriteLine("No gamepad or joystick was detected. Connect a controller and restart the application.");
    return;
}

using var joystick = new Joystick(directInput, joystickGuid);
Console.WriteLine($"Detected controller: {joystick.Information.ProductName} ({joystickGuid})");

try
{
    joystick.Properties.BufferSize = 128;
    joystick.Acquire();
}
catch (SharpDX.SharpDXException ex)
{
    Console.WriteLine($"Failed to acquire controller: {ex.Message}");
    return;
}

var objectNames = joystick.GetObjects()
    .Where(o => o.ObjectId.Flags.HasFlag(DeviceObjectTypeFlags.Axis) ||
                o.ObjectId.Flags.HasFlag(DeviceObjectTypeFlags.Button) ||
                o.ObjectId.Flags.HasFlag(DeviceObjectTypeFlags.PointOfViewController))
    .GroupBy(o => (JoystickOffset)o.Offset)
    .ToImmutableDictionary(
        group => group.Key,
        group => group.Select(o => string.IsNullOrWhiteSpace(o.Name) ? o.ObjectType.ToString() : o.Name)
            .First());

Console.WriteLine("Listening for controller input. Press Ctrl+C to exit.\n");

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    Environment.Exit(0);
};

while (true)
{
    try
    {
        joystick.Poll();
        var bufferedData = joystick.GetBufferedData();

        if (bufferedData.Length == 0)
        {
            var state = joystick.GetCurrentState();
            PrintState(state);
        }
        else
        {
            foreach (var data in bufferedData)
            {
                var name = objectNames.TryGetValue(data.Offset, out var displayName)
                    ? displayName
                    : data.Offset.ToString();

                Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} | {name,-25} : {data.Value}");
            }
        }
    }
    catch (SharpDX.SharpDXException ex)
    {
        Console.WriteLine($"Input read error: {ex.Message}");
        break;
    }

    Thread.Sleep(20);
}

return;

static void PrintState(JoystickState state)
{
    var axes = new List<string>();

    void AddAxis(string name, int value)
    {
        axes.Add($"{name}: {value}");
    }

    AddAxis("X", state.X);
    AddAxis("Y", state.Y);
    AddAxis("Z", state.Z);
    AddAxis("RotationX", state.RotationX);
    AddAxis("RotationY", state.RotationY);
    AddAxis("RotationZ", state.RotationZ);

    if (state.Sliders is { Length: > 0 })
    {
        for (var i = 0; i < state.Sliders.Length; i++)
        {
            AddAxis($"Slider {i}", state.Sliders[i]);
        }
    }

    var nonDefaultAxes = axes.Where(a => !a.EndsWith(": 0")).ToArray();
    Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} | Axes: {(nonDefaultAxes.Length == 0 ? "(no movement)" : string.Join(", ", nonDefaultAxes))}");

    var pressedButtons = state.Buttons
        .Select((pressed, index) => (pressed, index))
        .Where(pair => pair.pressed)
        .Select(pair => $"Button {pair.index}")
        .ToArray();

    Console.WriteLine($"Buttons: {(pressedButtons.Length == 0 ? "(none pressed)" : string.Join(", ", pressedButtons))}");

    var povs = state.PointOfViewControllers
        .Select((value, index) => value >= 0 ? $"POV {index}: {value}" : $"POV {index}: Neutral")
        .ToArray();

    if (povs.Length > 0)
    {
        Console.WriteLine($"POVs: {string.Join(", ", povs)}");
    }

    Console.WriteLine(new string('-', 60));
}
