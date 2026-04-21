import json
import os

SESSION_FILE = "debit_session.json"

def save_session(tabs_data, theme, font_size, active_index, geometry=None):
    session = {
        "theme": theme,
        "font_size": font_size,
        "tabs": tabs_data,
        "active": active_index
    }
    if geometry is not None:
        session["geometry"] = geometry
    with open(SESSION_FILE, 'w', encoding='utf-8') as f:
        json.dump(session, f, indent=2, ensure_ascii=False)

def load_session():
    if not os.path.exists(SESSION_FILE):
        return None
    try:
        with open(SESSION_FILE, 'r', encoding='utf-8') as f:
            return json.load(f)
    except:
        return None