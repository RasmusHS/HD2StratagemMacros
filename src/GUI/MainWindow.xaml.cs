using GUI.Models;
using GUI.Services;
using GUI.Views;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;

namespace GUI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly StratagemService _stratagemService;
    private readonly InputService _inputService;

    // Current stratagem assigned to each slot (null = empty)
    private readonly Stratagem?[] _slots = new Stratagem?[4];

    // Low-level keyboard hook
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private IntPtr _hookHandle = IntPtr.Zero;
    private NativeMethods.LowLevelKeyboardProc? _hookProc;

    public MainWindow()
    {
        InitializeComponent();
        _stratagemService = new StratagemService();
        _inputService = new InputService();
        _stratagemService.Load();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        InstallHook();
    }

    protected override void OnClosed(EventArgs e)
    {
        UninstallHook();
        base.OnClosed(e);
    }

    // ─── Slot click handler ────────────────────────────────────────────────

    private void Slot_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button btn) return;
        int slotIndex = int.Parse(btn.Tag.ToString()!) - 1;

        var picker = new StratagemPickerWindow(_stratagemService)
        {
            Owner = this
        };

        if (picker.ShowDialog() == true && picker.SelectedStratagem != null)
            AssignSlot(slotIndex, picker.SelectedStratagem);
    }

    private void ClearAll_Click(object sender, RoutedEventArgs e)
    {
        for (int i = 0; i < 4; i++)
            ClearSlot(i);
        StatusText.Text = "All slots cleared";
    }

    // ─── Slot clear handler ────────────────────────────────────────────────

    private void ClearSlot_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem item) return;
        int slotIndex = int.Parse(item.Tag.ToString()!) - 1;
        ClearSlot(slotIndex);
    }

    private void ClearSlot(int slotIndex)
    {
        _slots[slotIndex] = null;

        var (iconView, emptyText, nameText) = slotIndex switch
        {
            0 => (Slot1Icon, Slot1Empty, Slot1Name),
            1 => (Slot2Icon, Slot2Empty, Slot2Name),
            2 => (Slot3Icon, Slot3Empty, Slot3Name),
            3 => (Slot4Icon, Slot4Empty, Slot4Name),
            _ => throw new ArgumentOutOfRangeException()
        };

        iconView.Visibility = Visibility.Collapsed;
        emptyText.Visibility = Visibility.Visible;
        nameText.Text = "Empty";
        StatusText.Text = $"Slot {slotIndex + 1} cleared";
    }

    // ─── Slot assignment ───────────────────────────────────────────────────

    private void AssignSlot(int slotIndex, Stratagem stratagem)
    {
        _slots[slotIndex] = stratagem;

        // Resolve controls for this slot
        var (iconView, emptyText, nameText) = slotIndex switch
        {
            0 => (Slot1Icon, Slot1Empty, Slot1Name),
            1 => (Slot2Icon, Slot2Empty, Slot2Name),
            2 => (Slot3Icon, Slot3Empty, Slot3Name),
            3 => (Slot4Icon, Slot4Empty, Slot4Name),
            _ => throw new ArgumentOutOfRangeException()
        };

        // Load SVG directly via file path
        iconView.Source = new Uri(stratagem.IconPath, UriKind.Absolute);
        iconView.Visibility = Visibility.Visible;
        emptyText.Visibility = Visibility.Collapsed;

        nameText.Text = stratagem.Name;
        StatusText.Text = $"Slot {slotIndex + 1} set to {stratagem.Name}";
    }

    // ─── Global numpad hotkey hook ─────────────────────────────────────────

    private void InstallHook()
    {
        _hookProc = HookCallback;
        _hookHandle = NativeMethods.SetWindowsHookEx(
            WH_KEYBOARD_LL,
            _hookProc,
            NativeMethods.GetModuleHandle(null),
            0);
    }

    private void UninstallHook()
    {
        if (_hookHandle != IntPtr.Zero)
            NativeMethods.UnhookWindowsHookEx(_hookHandle);
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == WM_KEYDOWN)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            int slotIndex = vkCode switch
            {
                0x61 => 0, // Numpad1
                0x62 => 1, // Numpad2
                0x63 => 2, // Numpad3
                0x64 => 3, // Numpad4
                _ => -1
            };

            if (slotIndex >= 0 && _slots[slotIndex] != null)
            {
                var inputs = _slots[slotIndex]!.Inputs;
                // Fire and forget - don't block the hook thread
                Task.Run(() => _inputService.ExecuteStratagem(inputs));
            }
        }

        return NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }
}

// ─── P/Invoke declarations ─────────────────────────────────────────────────

internal static class NativeMethods
{
    public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn,
        IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
        IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr GetModuleHandle(string? lpModuleName);
}