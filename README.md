# Futuristic Ctrl HUD

A local Windows desktop app that shows a transparent sci-fi radial HUD at the mouse position when you double-tap the Ctrl key.

## Run

Use the published build:

```powershell
.\FuturisticCtrlHud.exe
```

No black console window appears. The app stays running in the system tray beside the Windows clock.

## Tray Icon

Right-click the tray icon to:

- Show HUD
- Open Settings
- Restart App
- Exit

Double-click the tray icon to toggle the HUD. If you upload a logo in Settings, the tray icon uses that logo after the app restarts.

## Shortcuts

- Double-tap `Ctrl`: show/hide the HUD at the mouse position.
- Double-tap `Ctrl` while a tool window is open: autosaves that window, closes it, and reloads the HUD menu.
- `Ctrl+Space`: restart the app while keeping existing settings.
- Double-tap `Ctrl+Space`: close the app.

Only one instance of the app can run at a time.

## Presets

The default HUD presets are:

- **Doctor Contacts**: thermal-roll handout with logo and QR side by side, then link, email, number, and address lines.
- **POYC**: prints the configured POYC text.
- **DDA**: prints the configured DDA text as justified text.
- **Handover**: opens a small handover notes app and prints `Handover DD-MMM-YY` with justified bullet notes.
- **Custom TxT**: loads/edits TXT files or prints PDF files through the default PDF app.
- **Order List**: pharmacy sold-item list with large CSV search, bold result matching, quantity selector, client-order marking, profit visual, autosave, delete, clear, and table-style print.
- **Settings**: opens app settings.

## Settings

Settings are split into:

- **Menu slices**: choose the preset/action, auto-rename the slice from the preset, adjust label, and choose a color from a swatch/RGB color picker.
- **General Settings**: enable Superuser Mode, upload logo, website, email, mobile number, and multiline address.
- **Preset Details**: POYC text, DDA text, Custom TxT/PDF file, and Order CSV file.

The address field supports skipped lines and multiline entry.

When hovering a HUD slice, the center ring shows a short preview of what that slice does.

## Superuser Mode

Superuser Mode is enabled from Settings > General Settings.

When enabled, these advanced print tools appear before printing:

- Receipt preview thumbnail for Doctor Contacts.
- Receipt preview thumbnail for Handover.
- Receipt preview thumbnail for Order List.
- Test Print button for thermal printer calibration.

Normal users bypass those extra steps and print directly.

Settings are saved here:

```text
%APPDATA%\FuturisticCtrlHud\settings.json
```

## Order List CSV

The CSV/reference sheet should contain these headers:

```text
Product NAME    WHS    RRP    %
```

Tab-separated or comma-separated files are supported. The search scores typed words against product names. If nothing matches, the typed word is kept as a custom item in uppercase, and the CSV/database is not updated.

The order list saves immediately whenever an item is added, deleted, cleared, or a quantity changes, so it can recover after a power cut.

Double-clicking a search result opens a fast quantity selector with `+`, `-`, `Cancel`, and `Submit`.

The Client Order button marks the selected order-list line. Marked lines are lightly highlighted on the printout.

The selected product/item shows its profit margin as a filled progress bar for quick visual reference.

## Custom TxT/PDF

TXT files load into the editor and print to the printer selected in the print dialog. If you choose a PDF/XPS virtual printer, the app warns you because that would save a PDF instead of printing on paper.

PDF files are sent to the default PDF application's print command. Make sure the default printer is the physical printer if you want paper output.

## Build From Source

```powershell
dotnet build .\FuturisticCtrlHud.csproj
dotnet publish .\FuturisticCtrlHud.csproj -c Release -o outputs\FuturisticCtrlHudApp
```
