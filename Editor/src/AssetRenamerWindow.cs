using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

namespace AssetRenamer.Editor
{
    /// <summary>
    /// Editor window to batch-rename project assets to a chosen naming convention. Drag assets from the
    /// Project window onto the drop zone, pick a convention, optionally normalize messy names and auto-apply
    /// a type prefix, review the preview, then apply.
    /// </summary>
    public class AssetRenamerWindow : EditorWindow
    {
        #region Public

        [MenuItem("Tools/Asset Renamer")]
        public static void Open() => GetWindow<AssetRenamerWindow>("Asset Renamer").Show();

        #endregion


        #region Unity API

        private void CreateGUI()
        {
            _root = new VisualElement();
            _root.AddToClassList("ar-root");
            LoadStyle();
            rootVisualElement.Add(_root);
            EnsurePrefixTable();
            Rebuild();
            MaybeCheckForUpdate();
        }

        #endregion


        #region Tools and Utilities

        private void LoadStyle()
        {
            var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(STYLE_PATH);
            if (sheet != null) _root.styleSheets.Add(sheet);
        }

        private static void ApplyTooltip(VisualElement element, string text)
        {
            element.tooltip = text;
            element.Query().ForEach(child => { child.tooltip = text; });
        }

        private VisualElement BuildBanner()
        {
            var full = AssetDatabase.LoadAssetAtPath<Texture2D>(BANNER_PATH);
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(BANNER_ICON_PATH);
            if (full == null && icon == null) return null;

            var banner = new VisualElement();
            banner.AddToClassList("ar-banner");
            banner.style.backgroundImage = new StyleBackground(full != null ? full : icon);
            banner.RegisterCallback<GeometryChangedEvent>(evt => ApplyBannerVariant(banner, evt.newRect.width, full, icon));
            return banner;
        }

        private void ApplyBannerVariant(VisualElement banner, float width, Texture2D full, Texture2D icon)
        {
            bool wideEnough = width >= BANNER_MIN_WIDTH && full != null;
            var texture = wideEnough ? full : (icon != null ? icon : full);
            if (texture != null) banner.style.backgroundImage = new StyleBackground(texture);

            float target = wideEnough ? width / BANNER_ASPECT : BANNER_ICON_HEIGHT;
            if (Mathf.Abs(banner.resolvedStyle.height - target) > 0.5f) banner.style.height = target;
        }

        private void EnsurePrefixTable()
        {
            if (_prefixTable != null) return;

            var guids = AssetDatabase.FindAssets("t:AssetTypePrefixTable");
            if (guids.Length > 0)
                _prefixTable = AssetDatabase.LoadAssetAtPath<AssetTypePrefixTable>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        private void Rebuild()
        {
            if (_root == null) return;
            _root.Clear();
            var banner = BuildBanner();
            if (banner != null) _root.Add(banner);
            _root.Add(BuildControls());
            _root.Add(BuildDropZone());
            _root.Add(BuildAddSelectionButton());
            _root.Add(BuildPreview());
            _root.Add(BuildFooter());
            if (!string.IsNullOrEmpty(_lastResult))
            {
                var result = new Label(_lastResult);
                result.AddToClassList("ar-result");
                _root.Add(result);
            }

            _root.Add(BuildVersionBar());
        }

        private VisualElement BuildControls()
        {
            var panel = new VisualElement();
            panel.AddToClassList("ar-panel");
            panel.style.flexShrink = 0f;

            var conventionRow = new VisualElement();
            conventionRow.AddToClassList("ar-field-row");

            var conventionField = new EnumField("Convention", _convention);
            conventionField.style.flexGrow = 1f;
            conventionField.RegisterValueChangedCallback(evt => { _convention = (NamingConvention)evt.newValue; Recompute(); });
            conventionRow.Add(conventionField);
            ApplyTooltip(conventionField, "Casing style applied to each name. Click the ? to preview every style.");

            var helpButton = new Button(() => { _showConventionHelp = !_showConventionHelp; Rebuild(); }) { text = "?" };
            helpButton.AddToClassList("ar-help-button");
            ApplyTooltip(helpButton, "Preview every naming convention with a live example.");
            conventionRow.Add(helpButton);

            panel.Add(conventionRow);

            if (_showConventionHelp) panel.Add(BuildConventionHelp());

            var numberChoices = new List<string> { "Off", "1", "01", "001", "Auto" };
            var numberField = new DropdownField("Number suffix", numberChoices, (int)_numberPadding);
            const string numberTip = "Pads the trailing number of each name.\nOff: leave numbers as-is.\n1 / 01 / 001: force a fixed width.\nAuto: pad every number to the widest one in the dropped batch (e.g. 1..15 becomes 01..15) so they sort correctly.";
            ApplyTooltip(numberField, numberTip);
            numberField.RegisterValueChangedCallback(evt => { _numberPadding = (NumberPadding)numberChoices.IndexOf(evt.newValue); Recompute(); });
            panel.Add(numberField);

            var normalizeToggle = new Toggle("Normalize messy names") { value = _normalize };
            normalizeToggle.RegisterValueChangedCallback(evt => { _normalize = evt.newValue; Recompute(); });
            panel.Add(normalizeToggle);
            ApplyTooltip(normalizeToggle, "Strip accents, copy markers like (1) / copie, and extra spaces before formatting.");

            var prefixToggle = new Toggle("Apply type prefix") { value = _applyPrefix };
            prefixToggle.RegisterValueChangedCallback(evt => { _applyPrefix = evt.newValue; Recompute(); });
            panel.Add(prefixToggle);
            ApplyTooltip(prefixToggle, "Auto-prepend a type prefix (T_ textures, SM_ meshes, PF_ prefabs...) from the table below.");

            var hideToggle = new Toggle("Hide unchanged in preview") { value = _hideUnchanged };
            hideToggle.RegisterValueChangedCallback(evt => { _hideUnchanged = evt.newValue; Recompute(); });
            panel.Add(hideToggle);
            ApplyTooltip(hideToggle, "Hide rows that already conform, to focus on what will actually change.");

            var tableField = new ObjectField("Type Prefix Table") { objectType = typeof(AssetTypePrefixTable), value = _prefixTable };
            tableField.RegisterValueChangedCallback(evt => { _prefixTable = evt.newValue as AssetTypePrefixTable; Recompute(); });
            panel.Add(tableField);
            ApplyTooltip(tableField, "Maps file extensions and asset types to prefixes. Edit it to match your studio convention.");

            bool advancedActive = !string.IsNullOrEmpty(_findText) || !string.IsNullOrEmpty(_customPrefix) || !string.IsNullOrEmpty(_customSuffix);
            var advanced = new Foldout { text = advancedActive ? "Find / Replace / Affixes  *" : "Find / Replace / Affixes", value = _showAdvanced };
            advanced.AddToClassList("ar-advanced");
            advanced.RegisterValueChangedCallback(evt => _showAdvanced = evt.newValue);

            var findField = new TextField("Find") { value = _findText, isDelayed = true };
            findField.RegisterValueChangedCallback(evt => { _findText = evt.newValue; Recompute(); });
            advanced.Add(findField);
            ApplyTooltip(findField, "Text to search for in each name, applied before formatting. Case-insensitive. Leave empty to skip.");

            var replaceField = new TextField("Replace") { value = _replaceText, isDelayed = true };
            replaceField.RegisterValueChangedCallback(evt => { _replaceText = evt.newValue; Recompute(); });
            advanced.Add(replaceField);
            ApplyTooltip(replaceField, "Text that replaces every Find match. Leave empty to delete the matched text.");

            var customPrefixField = new TextField("Custom prefix") { value = _customPrefix, isDelayed = true };
            customPrefixField.RegisterValueChangedCallback(evt => { _customPrefix = evt.newValue; Recompute(); });
            advanced.Add(customPrefixField);
            ApplyTooltip(customPrefixField, "Free text inserted in front of the formatted name, after the type prefix. Written verbatim, e.g. Boss_.");

            var customSuffixField = new TextField("Custom suffix") { value = _customSuffix, isDelayed = true };
            customSuffixField.RegisterValueChangedCallback(evt => { _customSuffix = evt.newValue; Recompute(); });
            advanced.Add(customSuffixField);
            ApplyTooltip(customSuffixField, "Free text appended at the very end of the name. Written verbatim, e.g. _LOD0.");

            panel.Add(advanced);

            if (_applyPrefix && _prefixTable == null) panel.Add(BuildMissingTableHint());

            return panel;
        }

        private VisualElement BuildMissingTableHint()
        {
            var block = new VisualElement();

            var warn = new Label("No Type Prefix Table assigned. Create one to enable prefixes.");
            warn.AddToClassList("ar-warning");
            block.Add(warn);

            var create = new Button(CreatePrefixTable) { text = "Create Default Table" };
            create.AddToClassList("ar-button");
            block.Add(create);

            return block;
        }

        private VisualElement BuildConventionHelp()
        {
            var help = new VisualElement();
            help.AddToClassList("ar-help-panel");

            foreach (NamingConvention convention in System.Enum.GetValues(typeof(NamingConvention)))
            {
                var row = new VisualElement();
                row.AddToClassList("ar-help-row");
                if (convention == _convention) row.AddToClassList("ar-help-row--active");

                var name = new Label(ObjectNames.NicifyVariableName(convention.ToString()));
                name.AddToClassList("ar-help-name");
                row.Add(name);

                var example = new Label(NameFormatter.Format(HELP_SAMPLE, convention));
                example.AddToClassList("ar-help-example");
                row.Add(example);

                help.Add(row);
            }

            return help;
        }

        private VisualElement BuildDropZone()
        {
            var zone = new VisualElement();
            zone.AddToClassList("ar-dropzone");

            var label = new Label("Drag assets here");
            label.AddToClassList("ar-dropzone-label");
            zone.Add(label);

            zone.RegisterCallback<DragUpdatedEvent>(_ => DragAndDrop.visualMode = DragAndDropVisualMode.Copy);
            zone.RegisterCallback<DragPerformEvent>(_ =>
            {
                DragAndDrop.AcceptDrag();
                AddPaths(DragAndDrop.paths);
            });

            return zone;
        }

        private VisualElement BuildPreview()
        {
            var panel = new VisualElement();
            panel.AddToClassList("ar-panel");
            panel.style.flexGrow = 1f;
            panel.style.minHeight = 0;

            var title = new Label(_paths.Count == 0 ? "PREVIEW" : $"PREVIEW ({_paths.Count})");
            title.AddToClassList("ar-section-title");
            panel.Add(title);

            if (_paths.Count == 0)
            {
                panel.Add(new Label("Drop assets above to see the proposed names."));
                return panel;
            }

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1f;
            scroll.style.minHeight = 0;
            panel.Add(scroll);

            var plans = BuildPlans();
            for (int i = 0; i < plans.Count; i++)
            {
                if (_hideUnchanged && plans[i].m_status == RenameStatus.Unchanged) continue;
                scroll.Add(BuildRow(plans[i]));
            }

            return panel;
        }

        private VisualElement BuildRow(AssetRenamePlan plan)
        {
            var row = new VisualElement();
            row.AddToClassList("ar-row");

            bool toggleable = plan.m_status == RenameStatus.Ok || plan.m_status == RenameStatus.Excluded;
            if (toggleable)
            {
                string path = plan.m_assetPath;
                var include = new Toggle { value = plan.m_status != RenameStatus.Excluded };
                include.AddToClassList("ar-row-toggle");
                include.RegisterValueChangedCallback(evt =>
                {
                    if (evt.newValue) _excluded.Remove(path);
                    else if (!_excluded.Contains(path)) _excluded.Add(path);
                    Rebuild();
                });
                row.Add(include);
            }
            if (plan.m_status == RenameStatus.Excluded) row.style.opacity = 0.5f;

            var accent = new VisualElement();
            accent.AddToClassList("ar-row-accent");
            accent.style.backgroundColor = StatusColor(plan.m_status);
            row.Add(accent);

            var content = new VisualElement();
            content.AddToClassList("ar-row-content");

            var names = new Label($"{plan.m_originalName}{plan.m_extension}   ->   {plan.m_proposedName}{plan.m_extension}");
            names.AddToClassList("ar-row-names");
            content.Add(names);

            string detail = string.IsNullOrEmpty(plan.m_message) ? plan.m_status.ToString() : $"{plan.m_status}  -  {plan.m_message}";
            var status = new Label(detail);
            status.AddToClassList("ar-row-status");
            status.style.color = StatusColor(plan.m_status);
            content.Add(status);

            row.Add(content);
            return row;
        }

        private VisualElement BuildFooter()
        {
            var bar = new VisualElement();
            bar.AddToClassList("ar-button-row");

            var apply = new Button(ApplyAll) { text = "Apply" };
            apply.AddToClassList("ar-button");
            apply.AddToClassList("ar-button--primary");
            bar.Add(apply);

            var clear = new Button(ClearPaths) { text = "Clear" };
            clear.AddToClassList("ar-button");
            bar.Add(clear);

            if (_undo != null && _undo.Count > 0)
            {
                var revert = new Button(RevertLast) { text = $"Revert ({_undo.Count})" };
                revert.AddToClassList("ar-button");
                bar.Add(revert);
            }

            return bar;
        }

        private VisualElement BuildVersionBar()
        {
            var info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(AssetRenamerWindow).Assembly);

            var bar = new VisualElement();
            bar.AddToClassList("ar-versionbar");

            string current = info != null ? info.version : null;
            var label = new Label(info != null ? $"Asset Renamer v{current}" : "Asset Renamer (local)");
            label.AddToClassList("ar-version-label");
            bar.Add(label);

            bool isGit = info != null && info.source == PackageSource.Git;
            bool updateAvailable = isGit && !string.IsNullOrEmpty(_latestVersion) && IsNewer(_latestVersion, current);

            if (updateAvailable)
            {
                var update = new Button(UpdateTool) { text = $"Update available: v{_latestVersion}" };
                update.AddToClassList("ar-button");
                update.AddToClassList("ar-button--primary");
                bar.Add(update);
            }

            return bar;
        }

        private void UpdateTool()
        {
            var info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(AssetRenamerWindow).Assembly);
            string current = info != null ? info.version : "current";
            string target = string.IsNullOrEmpty(_latestVersion) ? "latest" : _latestVersion;

            bool confirmed = EditorUtility.DisplayDialog(
                "Update Asset Renamer",
                $"Update from v{current} to v{target}? This re-resolves the package from GitHub and may trigger a recompile and reopen the window.",
                "Update",
                "Cancel");
            if (!confirmed) return;

            Client.Add(GIT_URL);
        }

        private void MaybeCheckForUpdate()
        {
            if (_updateChecked) return;
            _updateChecked = true;

            var info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(AssetRenamerWindow).Assembly);
            if (info == null || info.source != PackageSource.Git) return;

            var request = UnityWebRequest.Get(MANIFEST_URL);
            var operation = request.SendWebRequest();
            operation.completed += _ =>
            {
                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var manifest = JsonUtility.FromJson<ManifestVersion>(request.downloadHandler.text);
                        if (manifest != null && !string.IsNullOrEmpty(manifest.version)) _latestVersion = manifest.version;
                    }
                    catch { /* malformed manifest, ignore */ }
                }

                request.Dispose();
                if (this != null) Rebuild();
            };
        }

        private static bool IsNewer(string latest, string current)
        {
            int[] l = ParseVersion(latest);
            int[] c = ParseVersion(current);
            for (int i = 0; i < 3; i++)
                if (l[i] != c[i]) return l[i] > c[i];
            return false;
        }

        private static int[] ParseVersion(string version)
        {
            var parts = new int[3];
            if (string.IsNullOrEmpty(version)) return parts;

            string core = version.Split('-')[0];
            string[] segments = core.Split('.');
            for (int i = 0; i < 3 && i < segments.Length; i++)
                int.TryParse(segments[i], out parts[i]);

            return parts;
        }

        private void AddPaths(string[] paths)
        {
            if (paths == null) return;
            _lastResult = string.Empty;

            for (int i = 0; i < paths.Length; i++)
            {
                string path = paths[i];
                if (string.IsNullOrEmpty(path)) continue;
                if (!path.StartsWith("Assets/")) continue;
                if (AssetDatabase.IsValidFolder(path)) AddFolderContents(path);
                else AddSingle(path);
            }

            Rebuild();
        }

        private void AddFolderContents(string folder)
        {
            var guids = AssetDatabase.FindAssets(string.Empty, new[] { folder });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!AssetDatabase.IsValidFolder(path)) AddSingle(path);
            }
        }

        private void AddSingle(string path)
        {
            if (!_paths.Contains(path)) _paths.Add(path);
        }

        private VisualElement BuildAddSelectionButton()
        {
            var add = new Button(AddSelection) { text = "Add selection" };
            add.AddToClassList("ar-button");
            add.style.flexShrink = 0f;
            add.style.marginTop = 0f;
            add.style.marginBottom = 10f;
            add.style.marginLeft = 0f;
            add.style.marginRight = 0f;
            ApplyTooltip(add, "Add the assets currently selected in the Project window. Folders are expanded to their contents.");
            return add;
        }

        private void AddSelection()
        {
            var paths = new List<string>();
            foreach (var obj in Selection.objects)
            {
                string assetPath = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(assetPath)) paths.Add(assetPath);
            }
            AddPaths(paths.ToArray());
        }

        private void ClearPaths()
        {
            _paths.Clear();
            _excluded.Clear();
            _lastResult = string.Empty;
            Rebuild();
        }

        private void Recompute() => Rebuild();

        private List<AssetRenamePlan> BuildPlans()
        {
            var options = new RenameOptions
            {
                m_convention = _convention,
                m_normalize = _normalize,
                m_applyPrefix = _applyPrefix,
                m_prefixTable = _prefixTable,
                m_numberPadding = _numberPadding,
                m_customPrefix = _customPrefix,
                m_customSuffix = _customSuffix,
                m_findText = _findText,
                m_replaceText = _replaceText
            };

            var plans = AssetRenamerEngine.BuildPlans(_paths, options);
            for (int i = 0; i < plans.Count; i++)
                if (plans[i].m_status == RenameStatus.Ok && _excluded.Contains(plans[i].m_assetPath))
                    plans[i].m_status = RenameStatus.Excluded;
            return plans;
        }

        private void ApplyAll()
        {
            var plans = BuildPlans();
            _undo.Clear();
            int applied = AssetRenamerEngine.ApplyAll(plans, _undo, out int failed);
            _lastResult = failed > 0 ? $"Renamed {applied}, {failed} failed (see Console)." : $"Renamed {applied} asset(s).";

            KeepUnresolved(plans);
            Rebuild();
        }

        private void RevertLast()
        {
            if (_undo == null || _undo.Count == 0) return;

            int reverted = 0;
            for (int i = _undo.Count - 1; i >= 0; i--)
            {
                string error = AssetDatabase.RenameAsset(_undo[i].m_newPath, _undo[i].m_originalName);
                if (string.IsNullOrEmpty(error)) reverted++;
                else Debug.LogError($"[Asset Renamer] Revert failed for '{_undo[i].m_newPath}': {error}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            _lastResult = $"Reverted {reverted} asset(s).";
            _undo.Clear();
            Rebuild();
        }

        private void KeepUnresolved(List<AssetRenamePlan> plans)
        {
            var survivors = new List<string>();
            for (int i = 0; i < plans.Count; i++)
                if (plans[i].m_status != RenameStatus.Ok) survivors.Add(plans[i].m_assetPath);

            _paths = survivors;
        }

        private void CreatePrefixTable()
        {
            var table = CreateInstance<AssetTypePrefixTable>();
            table.LoadDefaults();
            AssetDatabase.CreateAsset(table, DEFAULT_TABLE_PATH);
            AssetDatabase.SaveAssets();
            _prefixTable = table;
            Rebuild();
        }

        private static Color StatusColor(RenameStatus status) => status switch
        {
            RenameStatus.Ok => new Color(0.30f, 0.85f, 0.45f),
            RenameStatus.Unchanged => new Color(0.55f, 0.55f, 0.60f),
            RenameStatus.Collision => new Color(0.95f, 0.35f, 0.30f),
            RenameStatus.Invalid => new Color(1f, 0.65f, 0.20f),
            RenameStatus.Excluded => new Color(0.40f, 0.42f, 0.48f),
            _ => Color.gray
        };

        #endregion


        #region Private and Protected

        private const string STYLE_PATH = "Packages/com.tools.assetrenamer/Editor/src/AssetRenamerTheme.uss";
        private const string BANNER_PATH = "Packages/com.tools.assetrenamer/Editor/src/AssetRenamerBanner.jpg";
        private const string BANNER_ICON_PATH = "Packages/com.tools.assetrenamer/Editor/src/AssetRenamerIcon.jpg";
        private const float BANNER_MIN_WIDTH = 360f;
        private const float BANNER_ASPECT = 4f;
        private const float BANNER_ICON_HEIGHT = 110f;
        private const string DEFAULT_TABLE_PATH = "Assets/AssetTypePrefixTable.asset";
        private const string GIT_URL = "https://github.com/ZellTBH/asset-renamer.git";
        private const string MANIFEST_URL = "https://raw.githubusercontent.com/ZellTBH/asset-renamer/main/package.json";
        private static readonly List<string> HELP_SAMPLE = new List<string> { "wall", "torch" };

        [SerializeField] private NamingConvention _convention = NamingConvention.PascalCase;
        [SerializeField] private NumberPadding _numberPadding = NumberPadding.Off;
        [SerializeField] private bool _showConventionHelp;
        [SerializeField] private bool _normalize = true;
        [SerializeField] private bool _applyPrefix = true;
        [SerializeField] private AssetTypePrefixTable _prefixTable;
        [SerializeField] private List<string> _paths = new List<string>();
        [SerializeField] private List<RenameRecord> _undo = new List<RenameRecord>();
        [SerializeField] private string _lastResult = string.Empty;
        [SerializeField] private bool _hideUnchanged;
        [SerializeField] private List<string> _excluded = new List<string>();
        [SerializeField] private string _customPrefix = string.Empty;
        [SerializeField] private string _customSuffix = string.Empty;
        [SerializeField] private string _findText = string.Empty;
        [SerializeField] private string _replaceText = string.Empty;
        [SerializeField] private bool _showAdvanced;

        private VisualElement _root;

        private static bool _updateChecked;
        private static string _latestVersion;

        [System.Serializable]
        private class ManifestVersion
        {
            public string version;
        }

        #endregion
    }
}
