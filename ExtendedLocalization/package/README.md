# ExtendedLocalization

A plugin that overrides existing localization tokens and adds new ones from simple per-language text files.

## Usage

1. Install BepInEx and Jotunn.
2. Copy the `ExtendedLocalization.dll` into `BepInEx\plugins`.
3. Start the game once. The folder `BepInEx\config\ExtendedLocalization` is created (it is empty).
   Take `Example.yaml` from the source as a template.
4. Create one file per game language: `English.yaml`, `Ukrainian.yaml`, ... (`.yml` also works)
   (the file name is the game language name, case-insensitive).
5. Only the file of the currently selected language is applied. Languages without a file stay untouched.

## File format

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

## Config (BepInEx\config\BioHazardN7.ExtendedLocalization.cfg)

- `Enabled` - turn the mod on/off
- `Hot reload` - re-apply files automatically when they change (texts already shown update when displayed again)
- `Verbose logging` - log every applied token and every parsing problem