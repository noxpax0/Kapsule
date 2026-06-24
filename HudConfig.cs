using System.Windows.Media;
using MediaColor = System.Windows.Media.Color;

namespace FuturisticCtrlHud;

public sealed record MenuOption(string Label, string ActionKey, MediaColor Accent);

public static class HudConfig
{
    public const int DoubleTapMilliseconds = 380;
    public const double HudRadius = 210;
    public const double InnerRadius = 58;

    public static readonly ActionDefinition[] AvailableActions =
    [
        new("print_option_1", "Print Option 1", "Prints Option 1 selected to the console."),
        new("print_option_2", "Print Option 2", "Prints Option 2 selected to the console."),
        new("open_notepad", "Open Notepad", "Opens Windows Notepad."),
        new("show_notification", "Show Notification", "Shows a small HUD action notification."),
        new("print_contact_card", "Doctor Contacts", "Prints a thermal-roll patient handout with logo, QR, website, email, mobile, and location."),
        new("print_poyc", "POYC", "Prints the configured POYC text preset."),
        new("print_dda", "DDA", "Prints the configured DDA text preset."),
        new("open_handover", "Handover", "Opens a simple handover note app and prints a dated bullet handover slip."),
        new("open_custom_text", "Custom TxT", "Loads or edits a TXT/PDF printout sized to the selected printer."),
        new("open_order_list", "Order List", "Tracks sold pharmacy items from a CSV reference sheet with quantity and profit margin."),
        new("open_remedy_recipes", "Remedy Recipes", "Searches safe non-medical household recipe options with recipe-only guardrails."),
        new("open_settings", "Open Settings", "Opens the HUD settings window."),
    ];
}
