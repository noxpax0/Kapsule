"""User-editable HUD configuration.

Change labels, accents, or action names here. Action names must match functions
defined in actions.py. Each action function receives an ActionContext object.
"""

DOUBLE_TAP_SECONDS = 0.38

HUD_RADIUS = 210
INNER_RADIUS = 58

MENU_OPTIONS = [
    {
        "label": "Option 1",
        "action": "print_option_1",
        "accent": "#26d9ff",
    },
    {
        "label": "Option 2",
        "action": "print_option_2",
        "accent": "#3af5c6",
    },
    {
        "label": "Notepad",
        "action": "open_notepad",
        "accent": "#ff9f2f",
    },
    {
        "label": "Notify",
        "action": "show_notification",
        "accent": "#ff6b35",
    },
]
