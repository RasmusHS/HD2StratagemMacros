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

    static readonly Dictionary<string, int> WeaponCharge = new()
    {
        { "Railgun", 2515 }, // ms to max charge railgun
        { "Epoch", 2515 } // ms to max charge epoch
    };

    private int? _activeChargeMs = null;    

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
    /// <summary>
    /// Handles the click event for a slot button, allowing the user to select a stratagem from a picker dialog.
    /// </summary>
    /// <remarks>If the user selects a stratagem from the dialog, it assigns the selected stratagem to the
    /// corresponding slot based on the button's tag value.</remarks>
    /// <param name="sender">The source of the event, typically the button that was clicked.</param>
    /// <param name="e">The event data associated with the click event.</param>
    private void Slot_Click(object sender, RoutedEventArgs e)
    {
        // Determine which slot button was clicked based on the sender's Tag property, which should be set to the slot number (1-4).
        if (sender is not System.Windows.Controls.Button btn) return;
        int slotIndex = int.Parse(btn.Tag.ToString()!) - 1;

        // Open the StratagemPickerWindow to allow the user to select a stratagem for the clicked slot. The picker is initialized with the stratagem service and set to be owned by the main window.
        var picker = new StratagemPickerWindow(_stratagemService)
        {
            Owner = this
        };

        // Show the picker dialog and, if the user selects a stratagem (i.e., the dialog returns true and SelectedStratagem is not null), assign the selected stratagem to the corresponding slot using the AssignSlot method.
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

        // After clearing a slot, check if any of the remaining assigned stratagems are charge weapons and update the charge binding accordingly.
        // This ensures that if a charge weapon was cleared from a slot, Numpad5 will no longer trigger the charge macro unless another slot still contains a charge weapon.
        UpdateChargeBinding();

    }

    // ─── Slot assignment ───────────────────────────────────────────────────
    /// <summary>
    /// Assigns the specified stratagem to the designated slot and updates the corresponding user interface elements to
    /// reflect the assignment.
    /// </summary>
    /// <remarks>This method updates the visual representation of the slot, including the icon and name, based
    /// on the assigned stratagem. The status text is also updated to indicate the current assignment.</remarks>
    /// <param name="slotIndex">The zero-based index of the slot to assign the stratagem to. Must be within the valid range of available slots.</param>
    /// <param name="stratagem">The stratagem to assign to the specified slot. This object provides the icon and name to display in the user
    /// interface.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the value of slotIndex is outside the range of valid slot indices.</exception>
    private void AssignSlot(int slotIndex, Stratagem stratagem)
    {
        _slots[slotIndex] = stratagem; // Update internal state with the assigned stratagem

        // Resolve controls for this slot
        // This pattern matching switch expression selects the appropriate UI elements (icon view, empty text, and name text) based on the slot index.
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

        // Update the name text with the stratagem's name
        nameText.Text = stratagem.Name;
        StatusText.Text = $"Slot {slotIndex + 1} set to {stratagem.Name}";

        // After assigning a stratagem to a slot, check if any of the assigned stratagems are charge weapons and update the charge binding accordingly
        if (WeaponCharge.ContainsKey(stratagem.Name))
            UpdateChargeBinding();
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

    /// <summary>
    /// Processes keyboard hook events to detect numpad key presses and triggers associated macros for stratagems or
    /// charge weapons as appropriate.
    /// </summary>
    /// <remarks>This method enables users to activate stratagem macros using numpad keys 1-4, and to trigger
    /// a charge macro with numpad 5 when a charge weapon is active. If no charge weapon is active, numpad 5 is ignored.
    /// The method is intended to be used as a low-level keyboard hook callback.</remarks>
    /// <param name="nCode">The hook code that indicates the type of keyboard event. A value greater than or equal to zero means the event
    /// should be processed.</param>
    /// <param name="wParam">The message identifier for the keyboard event. Typically checked for WM_KEYDOWN to determine if a key was
    /// pressed.</param>
    /// <param name="lParam">A pointer to a structure containing information about the keyboard event, including the virtual key code.</param>
    /// <returns>A handle to the result of the next hook procedure in the chain, allowing other hooks to process the event.</returns>
    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        // Check if a key was pressed and if it's one of the numpad keys (1-5)
        if (nCode >= 0 && wParam == WM_KEYDOWN)
        {
            // Read the virtual key code from the lParam
            int vkCode = Marshal.ReadInt32(lParam);
            int slotIndex = (_activeChargeMs.HasValue) ? vkCode switch // If a charge weapon is active, Numpad5 should trigger the charge macro, so we map it to a special index (e.g., 4) that indicates the charge macro should be executed instead of a regular slot.
            {
                0x61 => 0, // Numpad1
                0x62 => 1, // Numpad2
                0x63 => 2, // Numpad3
                0x64 => 3, // Numpad4
                0x65 => 4, // Numpad5 (for charge weapons like the railgun/epoch)
                _ => -1
            } : vkCode switch // If no charge weapon is active, Numpad5 should not trigger any action, so we can ignore it by mapping it to -1 along with any other non-numpad keys.
            {
                0x61 => 0, // Numpad1
                0x62 => 1, // Numpad2
                0x63 => 2, // Numpad3
                0x64 => 3, // Numpad4
                _ => -1
            };

            // If a charge weapon is active and Numpad5 is pressed, execute the charge macro with the appropriate charge time
            // We check if _activeChargeMs has a value to determine if a charge weapon is currently active, and if so, we execute the charge macro when Numpad5 is pressed (indicated by slotIndex == 4).
            if (slotIndex == 4 && _activeChargeMs != null)
            {
                // Fire and forget - don't block the hook thread
                Task.Run(() => _inputService.ExecuteMaxCharge(_activeChargeMs.Value));
            }

            // If a regular numpad key (1-4) is pressed and the corresponding slot has an assigned stratagem, execute the macro for that stratagem
            // This allows the user to trigger the assigned stratagems using the numpad keys, while Numpad5 is reserved for charge macros if any charge weapons are present in the slots.
            // Note that if Numpad5 is pressed but no charge weapon is active, it will not trigger any action since slotIndex will be -1 in that case.
            if (slotIndex >= 0 && slotIndex <= 3 && _slots[slotIndex] != null)
            {
                var inputs = _slots[slotIndex]!.Inputs; // Get the input sequence for the assigned stratagem in the pressed slot
                // Fire and forget - don't block the hook thread
                Task.Run(() => _inputService.ExecuteStratagem(inputs));
            }

        }

        return NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    private void UpdateChargeBinding() // This method can be called whenever slot assignments change to ensure the charge macro is correctly bound to Numpad5 if a charge weapon is present
    {
        // Check if any slot contains a charge weapon
        bool hasChargeWeapon = _slots.Any(s => s != null && WeaponCharge.ContainsKey(s!.Name));

        // If a charge weapon is present, then update _activeChargeMs to the charge time of the weapon in the lowest indexed slot that contains a charge weapon.
        // If no charge weapon is present, set _activeChargeMs to null to indicate that Numpad5 should not trigger any charge macro.
        if (hasChargeWeapon)
        {
            for (int i = 0; i < 4; i++)
            {
                // Check if the slot is not empty and contains a charge weapon
                if (_slots[i] != null && WeaponCharge.ContainsKey(_slots[i]!.Name))
                {
                    // Update _activeChargeMs to the charge time of the weapon in this slot
                    _activeChargeMs = WeaponCharge[_slots[i]!.Name];
                }
            }
        }
        else
        {
            // If no charge weapon is present, Numpad5 should not trigger any charge macro, so we can set _activeChargeMs to null
            _activeChargeMs = null;
        }
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