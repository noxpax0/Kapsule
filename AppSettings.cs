using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Media;
using MediaColor = System.Windows.Media.Color;

namespace FuturisticCtrlHud;

public sealed class AppSettings
{
    public const string BundledLogoFileName = "Doctor Logo.png";
    public List<MenuItemSetting> MenuItems { get; set; } = [];
    public PresetSettings Presets { get; set; } = new();
    public List<PrepChecklistItem> PrepChecklist { get; set; } = [];

    public static string InstallDefaultSettingsPath => Path.Combine(AppContext.BaseDirectory, "default-settings.json");
    public static string MasterTemplateSettingsPath => Path.Combine(AppContext.BaseDirectory, "master-template-settings.json");
    public static string BundledLogoPath => Path.Combine(AppContext.BaseDirectory, BundledLogoFileName);

    public static string SettingsPath
    {
        get
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "FuturisticCtrlHud");
            Directory.CreateDirectory(folder);
            return Path.Combine(folder, "settings.json");
        }
    }

    public static AppSettings Load()
    {
        EnsureMasterTemplateBackup();
        try
        {
            if (File.Exists(SettingsPath))
            {
                var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath));
                if (settings is not null)
                {
                    settings.EnsureDefaults();
                    return settings;
                }
            }
        }
        catch
        {
            // A bad settings file should not prevent the HUD from opening.
        }

        var defaults = LoadInstallDefaults();
        defaults.Save();
        return defaults;
    }

    public static AppSettings LoadInstallDefaults()
    {
        EnsureMasterTemplateBackup();
        try
        {
            if (File.Exists(InstallDefaultSettingsPath))
            {
                var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(InstallDefaultSettingsPath));
                if (settings is not null)
                {
                    settings.EnsureDefaults();
                    return settings;
                }
            }
        }
        catch
        {
            // If the portable default file is not readable, fall back to compiled defaults.
        }

        return CreateDefault();
    }

    public void SaveAsInstallDefaults()
    {
        EnsureDefaults();
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(InstallDefaultSettingsPath, json);
        File.WriteAllText(MasterTemplateSettingsPath, json);
    }

    public void Save()
    {
        EnsureDefaults();
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SettingsPath, json);
    }

    public MenuOption[] ToMenuOptions()
    {
        EnsureDefaults();
        var options = new List<MenuOption>();
        foreach (var item in MenuItems)
        {
            options.Add(new MenuOption(item.Label, item.ActionKey, ParseColor(item.AccentHex)));
        }

        return options.ToArray();
    }

    public void EnsureDefaults()
    {
        if (MenuItems.Count == 0)
        {
            MenuItems = CreateDefault().MenuItems;
        }

        if (!MenuItems.Exists(item => item.ActionKey == "print_contact_card"))
        {
            MenuItems.Insert(0, new MenuItemSetting("Doctor Contacts", "print_contact_card", "#26D9FF"));
        }

        for (var i = 0; i < MenuItems.Count; i++)
        {
            if (MenuItems[i].ActionKey == "print_contact_card" && MenuItems[i].Label is "Patient Slip" or "Print Contact Card")
            {
                MenuItems[i] = MenuItems[i] with { Label = "Doctor Contacts" };
            }
        }

        if (!MenuItems.Exists(item => item.ActionKey == "open_handover"))
        {
            var settingsIndex = MenuItems.FindIndex(item => item.ActionKey == "open_settings");
            var insertIndex = settingsIndex >= 0 ? settingsIndex : MenuItems.Count;
            MenuItems.Insert(insertIndex, new MenuItemSetting("Handover", "open_handover", "#3AF5C6"));
        }

        InsertBeforeSettings("print_poyc", "POYC", "#FF9F2F");
        InsertBeforeSettings("print_dda", "DDA", "#FF6B35");
        InsertBeforeSettings("open_custom_text", "Custom TxT", "#B68CFF");
        InsertBeforeSettings("open_order_list", "Order List", "#7CE38B");
        InsertBeforeSettings("open_remedy_recipes", "Remedy Recipes", "#A7F3D0");

        if (!MenuItems.Exists(item => item.ActionKey == "open_settings"))
        {
            MenuItems.Add(new MenuItemSetting("Settings", "open_settings", "#CFF6FF"));
        }

        Presets ??= new PresetSettings();
        if (string.IsNullOrWhiteSpace(Presets.LogoPath))
        {
            Presets.LogoPath = BundledLogoFileName;
        }

        Presets.DdaMedicineNames ??= [];
        Presets.DdaMedicineNames = Presets.DdaMedicineNames
            .Select(name => SanitizeAlphanumeric(name, allowSpaces: true).ToUpperInvariant())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name)
            .ToList();

        if (PrepChecklist.Count == 0)
        {
            PrepChecklist = CreateDefaultChecklist();
        }
        EnsureChecklistItem("Remedy Recipes API", "Optional: set an OpenAI-compatible recipe API endpoint/model, or leave blank for offline safe recipes.");
    }

    public static AppSettings CreateDefault() => new()
    {
        MenuItems =
        [
            new("Doctor Contacts", "print_contact_card", "#26D9FF"),
            new("POYC", "print_poyc", "#FF9F2F"),
            new("DDA", "print_dda", "#FF6B35"),
            new("Handover", "open_handover", "#3AF5C6"),
            new("Custom TxT", "open_custom_text", "#B68CFF"),
            new("Order List", "open_order_list", "#7CE38B"),
            new("Remedy Recipes", "open_remedy_recipes", "#A7F3D0"),
            new("Settings", "open_settings", "#CFF6FF"),
        ],
        Presets = new PresetSettings
        {
            LogoPath = BundledLogoFileName,
            QrWebsite = "https://example.com",
            QrCaption = "https://example.com",
            Email = "hello@example.com",
            Mobile = "+1 555 0100",
            Location = "Your location",
            DdaText = "DDA Details",
            DdaMedicineNames = []
        },
        PrepChecklist = CreateDefaultChecklist()
    };

    private static List<PrepChecklistItem> CreateDefaultChecklist() =>
    [
        new("Logo image", "Upload pharmacy/clinic logo used for tray icon and Doctor Contacts."),
        new("QR website", "Paste website URL for Doctor Contacts QR code."),
        new("Email", "Set public patient-facing email."),
        new("Mobile number", "Set public contact number."),
        new("Address", "Set multiline address, including skipped lines if needed."),
        new("POYC text", "Review POYC print text."),
        new("DDA details and medicine search", "Review reusable DDA Details text; medicine names build automatically from the DDA HUD tool."),
        new("Custom TxT/PDF", "Choose default custom text or PDF file."),
        new("Order CSV", "Choose product reference CSV with Product NAME, WHS, RRP, %."),
        new("Remedy Recipes API", "Optional: set an OpenAI-compatible recipe API endpoint/model, or leave blank for offline safe recipes."),
        new("Thermal test print", "Enable Superuser Mode and run a test print on the target printer."),
    ];

    private void EnsureChecklistItem(string name, string needed)
    {
        if (!PrepChecklist.Exists(item => item.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            PrepChecklist.Add(new PrepChecklistItem(name, needed));
        }
    }

    private void InsertBeforeSettings(string actionKey, string label, string accentHex)
    {
        if (MenuItems.Exists(item => item.ActionKey == actionKey))
        {
            return;
        }

        var settingsIndex = MenuItems.FindIndex(item => item.ActionKey == "open_settings");
        var insertIndex = settingsIndex >= 0 ? settingsIndex : MenuItems.Count;
        MenuItems.Insert(insertIndex, new MenuItemSetting(label, actionKey, accentHex));
    }

    public static string SanitizeAlphanumeric(string value, bool allowSpaces)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "";
        }

        var chars = new List<char>(value.Length);
        var previousWasSpace = false;
        foreach (var ch in value)
        {
            if (char.IsLetterOrDigit(ch))
            {
                chars.Add(ch);
                previousWasSpace = false;
                continue;
            }

            if (allowSpaces && char.IsWhiteSpace(ch) && !previousWasSpace && chars.Count > 0)
            {
                chars.Add(' ');
                previousWasSpace = true;
            }
        }

        return new string(chars.ToArray()).Trim();
    }

    private static MediaColor ParseColor(string hex)
    {
        try
        {
            return (MediaColor)ColorConverter.ConvertFromString(hex);
        }
        catch
        {
            return MediaColor.FromRgb(0x26, 0xD9, 0xFF);
        }
    }

    public static string ResolveLogoPath(PresetSettings presets)
    {
        if (!string.IsNullOrWhiteSpace(presets.LogoPath))
        {
            if (Path.IsPathRooted(presets.LogoPath) && File.Exists(presets.LogoPath))
            {
                return presets.LogoPath;
            }

            var portablePath = Path.Combine(AppContext.BaseDirectory, presets.LogoPath);
            if (File.Exists(portablePath))
            {
                return portablePath;
            }

            if (File.Exists(presets.LogoPath))
            {
                return Path.GetFullPath(presets.LogoPath);
            }
        }

        return File.Exists(BundledLogoPath) ? BundledLogoPath : "";
    }

    private static void EnsureMasterTemplateBackup()
    {
        try
        {
            if (File.Exists(MasterTemplateSettingsPath))
            {
                return;
            }

            if (File.Exists(InstallDefaultSettingsPath))
            {
                File.Copy(InstallDefaultSettingsPath, MasterTemplateSettingsPath, overwrite: false);
                return;
            }

            var json = JsonSerializer.Serialize(CreateDefault(), new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(MasterTemplateSettingsPath, json);
        }
        catch
        {
            // Template backup creation should never slow or block app startup.
        }
    }
}

public sealed record MenuItemSetting(string Label, string ActionKey, string AccentHex);

public sealed class PresetSettings
{
    public bool SuperuserMode { get; set; }
    public string LogoPath { get; set; } = "";
    public string QrWebsite { get; set; } = "";
    public string QrCaption { get; set; } = "";
    public string Email { get; set; } = "";
    public string Mobile { get; set; } = "";
    public string Location { get; set; } = "";
    public string HandoverNotes { get; set; } = "";
    public string PoycText { get; set; } = "POYC";
    public string DdaText { get; set; } = "DDA Details";
    public List<string> DdaMedicineNames { get; set; } = [];
    public string CustomTextFilePath { get; set; } = "";
    public string CustomTextContent { get; set; } = "";
    public string OrderCsvPath { get; set; } = "";
    public string RemedyApiEndpoint { get; set; } = "";
    public string RemedyApiModel { get; set; } = "";
    public string RemedyApiKey { get; set; } = "";
    public List<OrderLineItem> OrderItems { get; set; } = [];
    public List<RemedyRecipe> CustomRemedyRecipes { get; set; } = [];
}

public sealed record ActionDefinition(string Key, string Name, string Description);

public sealed class PrepChecklistItem
{
    public string Name { get; set; } = "";
    public string Needed { get; set; } = "";
    public bool Done { get; set; }

    public PrepChecklistItem()
    {
    }

    public PrepChecklistItem(string name, string needed)
    {
        Name = name;
        Needed = needed;
    }
}

public sealed class OrderLineItem
{
    public string ProductName { get; set; } = "";
    public string Whs { get; set; } = "";
    public string Rrp { get; set; } = "";
    public string ProfitPercent { get; set; } = "";
    public int Quantity { get; set; } = 1;
    public bool IsCustom { get; set; }
    public bool IsClientOrder { get; set; }
}
