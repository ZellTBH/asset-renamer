# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2026-06-05

### Added
- Initial release of the Asset Renamer editor tool.
- Drag-and-drop batch renaming of project assets to a chosen naming convention (Pascal, camel, snake, kebab, flat, upper flat, Pascal snake, camel snake, screaming snake, train, cobol).
- Optional name normalization: strips diacritics and copy markers ("(1)", "copie"/"copy") before formatting.
- Optional auto type prefix driven by an editable `AssetTypePrefixTable` ScriptableObject (extension rules first, then type rules by inheritance).
- Preview with per-asset status (Ok, Unchanged, Collision, Invalid) and in-batch collision detection.
- Renaming goes through `AssetDatabase`, preserving GUIDs and references.
