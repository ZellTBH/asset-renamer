# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.4.0] - 2026-06-06

### Added
- Strip redundant type words: when a name already contains a word that just repeats its type prefix, it is dropped (e.g. wall_mat -> M_Wall instead of M_WallMat, Wall Prefab -> PF_Wall, TorchPF -> PF_Torch). A glued upper-case acronym tail is detached first so the tokenizer can separate it. Controlled by a "Strip redundant type words" toggle, on by default.
- PrefixRule gains an optional aliases field listing the words that mean the same type (e.g. "material mat" for M_), so each studio can tune what counts as redundant.
- A "Reset table to defaults" button on the tool to refresh an existing prefix table with the up-to-date built-in rules (so an older table picks up the new type aliases after upgrading).

### Changed
- Prefix table resolution refactored around a single TryResolveRule, so the prefix and its aliases come from the same matched rule (no behavior change for existing prefixes).

## [0.3.1] - 2026-06-06

### Changed
- Update confirmation now shows the current and target versions (e.g. "Update from v0.3.0 to v0.3.1?").

## [0.3.0] - 2026-06-06

### Added
- Find & replace: a case-insensitive search/replace applied to each name before formatting.
- Custom prefix and custom suffix: free text wrapped around the formatted name, written verbatim.
- Collapsible "Find / Replace / Affixes" section, collapsed by default, marked with an indicator when a transform is active.
- Version readout in the footer, plus an "Update available" button that appears only when a newer version exists on the repository (reads the manifest on main once per session; git installs only).

### Changed
- Engine formatting options bundled into a RenameOptions struct (internal refactor, no behavior change).

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
