---

<details>
<summary>Українська</summary>

# ExtendedLocalization

Плагін, який перевизначає наявні токени локалізації та додає нові з простих текстових файлів для кожної мови.

# ВИКОРИСТАННЯ

1. Встановіть BepInEx.
2. Скопіюйте `ExtendedLocalization.dll` до `BepInEx\plugins`.
3. Запустіть гру один раз. Буде створено папку `BepInEx\config\ExtendedLocalization` (вона порожня).
   Візьміть `Example.yaml` з вихідного коду як шаблон.
4. Створіть по одному файлу на кожну мову гри: `English.yaml`, `Ukrainian.yaml`, ... (`.yml` також працює)
   (назва файлу — це назва мови гри, регістр не має значення).
5. Застосовується лише файл поточної вибраної мови. Мови без файлу залишаються без змін.

# ФОРМАТ ФАЙЛУ

```
# коментар (лише в окремому рядку)
item_stone_description : "It's a HARD ROCK!"
poi_leviathan : "Leviathan"
multi_line : "First line\nSecond line"
```

- `token : "Текст"` — початковий `$` у токені необов'язковий
- Наявні токени перевизначаються, невідомі додаються (їх можуть використовувати інші моди)
- `\n` — перенесення рядка, `\"` — лапка всередині значення в подвійних лапках
- Файли у кодуванні UTF-8, розширення може бути `.yaml` або `.yml`

# КОНФІГУРАЦІЯ

- `Enabled` — увімкнути/вимкнути мод
- `Hot reload` - автоматично повторно застосовувати файли в разі їх зміни (тексти, що вже були показані, оновлюються при повторному відображенні)
- `Verbose logging` — записувати в лог кожен застосований токен і кожну проблему розбору

# СПИСОК ЗМІН

## 1.0.2

- Оновлено `manifest.json` (оновлено посилання на спільноту).

## 1.0.1

### Виправлення
- Токени не підтягувалися в збірках з великою кількістю модів.
- Англійський файл більше не перекриває обрану мову, якщо інший мод повторно викликає завантаження англійської.
- `Hot reload` тепер перезавантажує обрану мову гри, а не ту, що була в останньому виклику.

### Зміни

- Токени підставляються при самому пошуку перекладу, тому результат не залежить від кількості таблиць, їх очищення чи порядку завантаження.
- Порядок застосування: спочатку `English.yml`, потім файл обраної мови поверх нього.
- Після кожної перебудови скидається кеш рядків гри, тож застарілі `[token]` не лишаються.
- Файли YAML парсяться лише при першому читанні або після змін на диску, а не при кожному виклику `SetupLanguage`.
- У логу один запис `Loaded N token(s)` на файл замість повторів на кожен виклик.
- Якщо Harmony-патч не вдалося застосувати (наприклад, після оновлення гри), мод не падає, а пише помилку в лог.

## 1.0.0

- Перший випуск.

</details>

---

<details>
<summary>English</summary>

# ExtendedLocalization

A plugin that overrides existing localization tokens and adds new ones from simple per-language text files.

# USAGE

1. Install BepInEx.
2. Copy the `ExtendedLocalization.dll` into `BepInEx\plugins`.
3. Start the game once. The folder `BepInEx\config\ExtendedLocalization` is created (it is empty).
   Take `Example.yaml` from the source as a template.
4. Create one file per game language: `English.yaml`, `Ukrainian.yaml`, ... (`.yml` also works)
   (the file name is the game language name, case-insensitive).
5. Only the file of the currently selected language is applied. Languages without a file stay untouched.

# FILE FORMAT

```
# comment (own line only)
item_stone_description : "It's a HARD ROCK!"
poi_leviathan : "Leviathan"
multi_line : "First line\nSecond line"
```

- `token : "Text"` - the leading `$` of the token is optional
- Existing tokens are overridden, unknown tokens are added (other mods can use them)
- `\n` is a line break, `\"` is a quote inside a double-quoted value
- Files are UTF-8, the extension can be `.yaml` or `.yml`

# CONFIG

- `Enabled` - turn the mod on/off
- `Hot reload` - re-apply files automatically when they change (texts already shown update when displayed again)
- `Verbose logging` - log every applied token and every parsing problem

# CHANGELOG

## 1.0.2

- Updated `manifest.json` (updated the link to the community).

## 1.0.1

### Fixes
- Tokens were not picked up in large modpacks.
- The English file no longer overrides the selected language when another mod calls the English language load again.
- `Hot reload` now reloads the language selected in the game, not the one from the last call.

### Changes

- Tokens are now supplied at the moment of translation lookup, so the result no longer depends on how many tables exist, who clears them, or the load order.
- Application order: `English.yml` first, then the selected language file on top of it.
- The game’s string cache is cleared after every rebuild, so stale `[token]` results do not stay on screen.
- YAML files are parsed only on first read or after they change on disk, not on every `SetupLanguage` call.
- The log has a single `Loaded N token(s)` entry per file instead of repeats on every call.
- If the Harmony patch cannot be applied (for example after a game update), the mod no longer crashes and writes an error to the log instead.

## 1.0.0

- Initial release.

</details>

---

| [Discord](https://discord.gg/8hdmpkCB6w) | [Github](https://github.com/BioHazardN7/ValheimModding) |
|----|----|