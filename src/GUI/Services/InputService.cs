using InputSimulatorStandard;
using InputSimulatorStandard.Native;

namespace GUI.Services;

public class InputService
{
    private readonly InputSimulator _sim = new();

    private static readonly Dictionary<string, VirtualKeyCode> KeyMap = new()
    {
        { "Up",    VirtualKeyCode.UP    },
        { "Down",  VirtualKeyCode.DOWN  },
        { "Left",  VirtualKeyCode.LEFT  },
        { "Right", VirtualKeyCode.RIGHT }
    };

    public async Task ExecuteStratagem(List<string> inputs, int delayMs = 15)
    {
        // Hold LCtrl to open stratagem menu
        _sim.Keyboard.KeyDown(VirtualKeyCode.LCONTROL);
        await Task.Delay(50);
        _sim.Keyboard.KeyUp(VirtualKeyCode.LCONTROL);
        await Task.Delay(100);

        foreach (var input in inputs)
        {
            if (KeyMap.TryGetValue(input, out var key))
            {
                _sim.Keyboard.KeyDown(key);
                await Task.Delay(delayMs);
                _sim.Keyboard.KeyUp(key);
                await Task.Delay(delayMs);
            }
        }
    }

    public async Task ExecuteMaxCharge(int holdMs)
    {
        _sim.Mouse.LeftButtonDown();
        await Task.Delay(holdMs);
        _sim.Mouse.LeftButtonUp();
    }
}
