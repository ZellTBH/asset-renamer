# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.2.0] - 2026-06-05

### Added
- Number suffix padding: pad a trailing number to a fixed width (1 / 01 / 001) or Auto, which aligns every number to the widest one in the batch.
- Convention help panel: a "?" button next to the convention dropdown previews every convention with a live example, highlighting the active one.
- Tooltips on every control.
- Revert: undo the last applied batch in one click.
- Folder drop: dropping a folder adds all its contents recursively.
- Add selection: add the assets currently selected in the Project window.
- Hide unchanged in preview, plus a per-row toggle to exclude individual assets from the batch (Excluded status).
- documentationUrl in the package manifest so the Package Manager "Documentation" link opens the guide.

### Changed
- Apply now surfaces failed renames (count + Console error) instead of swallowing them, and shows a result line.
- Drag-and-drop is restricted to assets under Assets/ (dropping from immutable Packages/ was a silent no-op before).

## [0.1.0] - 2026-06-05

### Added
- Initial release of the Asset Renamer editor tool.
- Drag-and-drop batch renaming of project assets to a chosen naming convention (Pascal, camel, snake, kebab, flat, upper flat, Pascal snake, camel snake, screaming snake, train, cobol).
- Optional name normalization: strips diacritics and copy markers ("(1)", "copie"/"copy") before formatting.
- Optional auto type prefix driven by an editable `AssetTypePrefixTable` ScriptableObject (extension rules first, then type rules by inheritance).
- Preview with per-asset status (Ok, Unchanged, Collision, Invalid) and in-batch collision detection.
- Renaming goes through `AssetDatabase`, preserving GUIDs and references.
