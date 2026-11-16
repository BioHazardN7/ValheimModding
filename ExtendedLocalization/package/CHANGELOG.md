# Changelog

## 1.0.2

- Updated `manifest.json` (updated the link to the community).

## 1.0.1

### Fixes
- Tokens were not picked up in large modpacks: the game showed `[label_helmet]` instead of the translation. The cause was that the game could read a different translation table than the one the mod wrote its tokens into.
- The English file no longer overrides the selected language when another mod calls the English language load again.
- Hot reload now reloads the language selected in the game, not the one from the last call.

### Changes

- Tokens are now supplied at the moment of translation lookup, so the result no longer depends on how many tables exist, who clears them, or the load order.
- Application order: `English.yml` first, then the selected language file on top of it.
- The game’s string cache is cleared after every rebuild, so stale `[token]` results do not stay on screen.
- YAML files are parsed only on first read or after they change on disk, not on every `SetupLanguage` call.
- The log has a single `Loaded N token(s)` entry per file instead of repeats on every call.
- If the Harmony patch cannot be applied (for example after a game update), the mod no longer crashes and writes an error to the log instead.

## 1.0.0

- Initial release.