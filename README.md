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

The EXE and tray icon use the bundled Doctor logo by default. You can still replace the logo from Settings.

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
- **DDA**: opens a fast DDA slip tool with side-by-side ID and numeric EUR fees, a red Confirm Fees action, medicine/quantity rows, grouped saved-medicine chips with SuperUser contextual removal, Renew DDA Card (Fee), and optional justified DDA info.
- **Handover**: opens a small handover notes app and prints `Handover DD-MMM-YY` with justified bullet notes.
- **Custom TxT**: loads/edits TXT files or prints PDF files through the default PDF app.
- **Order List**: pharmacy sold-item list with large CSV search, bold result matching, quantity selector, client-order marking, profit visual, autosave, delete, clear, and table-style print.
- **Remedy Recipes**: safe recipe-only search for low-risk household preparations, limited to the top three results.
- **Settings**: opens app settings.

## Settings

Settings are split into:

- **Menu slices**: choose the preset/action, auto-rename the slice from the preset, adjust label, and choose a color from a swatch/RGB color picker.
- **General Settings**: enable Superuser Mode, upload logo, website, email, mobile number, and multiline address.
- **Preset Details**: POYC text, reusable DDA Details text, Custom TxT/PDF file, Order CSV file, and optional Remedy Recipes API fields.
- **Preparation Checklist**: tick off what needs changing before a new PC/site is ready.
- **Install Defaults**: save the current setup as portable defaults, or reset software data back to those defaults.

The address field supports skipped lines and multiline entry.

When hovering a HUD slice, the center ring shows a short preview of what that slice does.

Regular checkboxes use a large accessible white box with a thick dark border and an oversized green tick, including the DDA slip options.

## Superuser Mode

Superuser Mode is enabled from Settings > General Settings.

When enabled, these advanced print tools appear before printing:

- Receipt preview thumbnail for Doctor Contacts.
- Receipt preview thumbnail for Handover.
- Receipt preview thumbnail for Order List.
- Test Print button for thermal printer calibration.

Normal users bypass those extra steps and print directly.

## Printing Fit

The app uses a shared print fitter for app-controlled printouts. Thermal/receipt printers keep long receipt-style output, while toner/A4/letter style printers paginate long stack/text output instead of sending one tall clipped visual. Printouts are measured against the selected printer's printable width.

PDF files still print through the default PDF app, so PDF page fitting depends on that PDF app and printer driver.

Settings are saved here:

```text
%APPDATA%\FuturisticCtrlHud\settings.json
```

Portable install defaults are saved beside the EXE:

```text
default-settings.json
```

The master template backup is also saved beside the EXE:

```text
master-template-settings.json
```

When the app is first run on a new PC, it uses `default-settings.json` if that file is present. If the user later resets software data, the app restores from `default-settings.json` again.

The bundled default logo is:

```text
Doctor Logo.png
```

If Settings has no custom logo, or the custom logo cannot be found, the app falls back to the bundled Doctor logo automatically.

Recommended setup flow:

1. Configure logo, website, email, address, POYC, reusable DDA Details, Custom TxT/PDF, Order CSV, optional Remedy Recipes API, and menu slice colors.
2. Tick the relevant preparation checklist items.
3. Click **Save Current as Install Defaults**.
4. Keep `master-template-settings.json` as the backup copy of the approved template details.
5. Copy the whole published app folder to the new PC.
6. On that PC, the app is ready pending minor local adjustments.

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

## Remedy Recipes

Remedy Recipes is designed as a safe recipe search preset, not a medical assistant.

It can suggest low-risk household preparations such as salt-water rinse, saline foot soak, baking-soda foot deodorizer, diluted shoe spray, oatmeal soak, warm/cool compress, ginger comfort drink, humidified-air comfort, hand scrub, dry-hand glycerin mix, simple lip balm, rice warm pack, or a honey lemon comfort drink. It uses synonym expansion and typo-tolerant matching, so a search like `sallt bath`, `salin`, or `fooot odour` can still find safe `saline`, `salt water`, `salt soak`, or foot odour concepts. Quick-search buttons are included for common pharmacy-counter intents such as salt/saline, throat comfort, foot odour, dry skin, compresses, nasal dryness, hand care, and digestive comfort. It refuses medical diagnosis, treatment claims, personal information, emergencies, serious symptoms, unsafe chemicals, homemade eye washes, essential oils in eyes/ears, wounds, infections, burns, poisoning, pregnancy, children, pets, medication-interaction concerns, and essential-oil ingestion. Eye-wash searches are blocked with the safety message to use only sterile saline or professional care.

Recipe results appear as compact sorted-card stacks so more results fit in the window. Each closed card has a source badge: book/blue for local library, globe/green for found-online references, and star/gold for custom saved recipes. Click a card to open its drawer; click it again to close it. Quantity controls, ingredient rows, source notes, and the Print button are only shown inside the opened drawer. Grams and ml fields are mutually exclusive: filling grams greys out ml, and filling ml greys out grams.

Use **Create Custom Remedy Recipe** in the same window to build your own printable recipe with ingredient rows, preparation steps, source/reference notes, and the same grams/ml behaviour. Saved custom recipes appear as light-gold cards to distinguish them from searched recipe cards.

The preset works offline with a broader built-in safe recipe library. Every safe search also tries public lookups such as DuckDuckGo Instant Answer, Wikipedia, PubMed, and OpenFDA as best-effort source references when the PC has internet access. In Settings > Preset Details, you can optionally add an OpenAI-compatible chat completions endpoint, model, and API key. Leave those fields blank to keep using the offline recipe library plus public repositories. No third-party API is guaranteed to remain permanently free, so local/free-compatible providers are best treated as configurable rather than hard-coded.

## Custom TxT/PDF

TXT files load into the editor and print to the printer selected in the print dialog. If you choose a PDF/XPS virtual printer, the app warns you because that would save a PDF instead of printing on paper.

PDF files are sent to the default PDF application's print command. Make sure the default printer is the physical printer if you want paper output.

## Build From Source

```powershell
dotnet build .\FuturisticCtrlHud.csproj
dotnet publish .\FuturisticCtrlHud.csproj -c Release -o outputs\FuturisticCtrlHudApp
```
