# Asset Renamer

Editor tool to batch-rename project assets to a consistent naming convention. Built as a teaching aid for asset naming conventions, usable on any asset type (textures, models, prefabs, VFX, materials, audio...).

## Install (Unity Package Manager, git URL)

Window > Package Manager > + > "Install package from git URL":

```
https://github.com/ZellTBH/asset-renamer.git
```

Pin a version with a tag: append `#0.1.0`.

## Usage

1. Open via `Tools > Asset Renamer`.
2. Drag assets from the Project window onto the drop zone.
3. Pick a naming convention. Optionally enable normalization and the type prefix.
4. Review the preview. Collisions and invalid names are flagged and skipped.
5. Click Apply. Renames go through `AssetDatabase`, so GUIDs and references are preserved.

## Type prefixes

Prefixes are resolved by an `AssetTypePrefixTable` asset (create one via the window button or `Assets > Create > Asset Renamer > Type Prefix Table`). Extension rules are evaluated before type rules. The prefix keeps its own `_` separator and is never re-cased.

## Naming conventions

Pascal, camel, snake, kebab, flat, upper flat, Pascal snake, camel snake, screaming snake, train, cobol. Some (cobol, upper flat) are included for completeness and rarely fit assets.
