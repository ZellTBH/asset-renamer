using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
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
        }

        #endregion


        #region Tools and Utilities

        private void LoadStyle()
        {
            var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(STYLE_PATH);
            if (sheet != null) _root.styleSheets.Add(sheet);
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
            _root.Add(BuildPreview());
            _root.Add(BuildFooter());
        }

        private VisualElement BuildControls()
        {
            var panel = new VisualElement();
            panel.AddToClassList("ar-panel");

            var conventionField = new EnumField("Convention", _convention);
            conventionField.RegisterValueChangedCallback(evt => { _convention = (NamingConvention)evt.newValue; Recompute(); });
            panel.Add(conventionField);

            var normalizeToggle = new Toggle("Normalize messy names") { value = _normalize };
            normalizeToggle.RegisterValueChangedCallback(evt => { _normalize = evt.newValue; Recompute(); });
            panel.Add(normalizeToggle);

            var prefixToggle = new Toggle("Apply type prefix") { value = _applyPrefix };
            prefixToggle.RegisterValueChangedCallback(evt => { _applyPrefix = evt.newValue; Recompute(); });
            panel.Add(prefixToggle);

            var tableField = new ObjectField("Type Prefix Table") { objectType = typeof(AssetTypePrefixTable), value = _prefixTable };
            tableField.RegisterValueChangedCallback(evt => { _prefixTable = evt.newValue as AssetTypePrefixTable; Recompute(); });
            panel.Add(tableField);

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
            panel.Add(scroll);

            var plans = BuildPlans();
            for (int i = 0; i < plans.Count; i++) scroll.Add(BuildRow(plans[i]));

            return panel;
        }

        private VisualElement BuildRow(AssetRenamePlan plan)
        {
            var row = new VisualElement();
            row.AddToClassList("ar-row");

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

            return bar;
        }

        private void AddPaths(string[] paths)
        {
            if (paths == null) return;

            for (int i = 0; i < paths.Length; i++)
            {
                string path = paths[i];
                if (string.IsNullOrEmpty(path)) continue;
                if (!path.StartsWith("Assets/") && !path.StartsWith("Packages/")) continue;
                if (AssetDatabase.IsValidFolder(path)) continue;
                if (!_paths.Contains(path)) _paths.Add(path);
            }

            Rebuild();
        }

        private void ClearPaths()
        {
            _paths.Clear();
            Rebuild();
        }

        private void Recompute() => Rebuild();

        private List<AssetRenamePlan> BuildPlans()
            => AssetRenamerEngine.BuildPlans(_paths, _convention, normalize: _normalize, applyPrefix: _applyPrefix, _prefixTable);

        private void ApplyAll()
        {
            var plans = BuildPlans();
            int applied = AssetRenamerEngine.ApplyAll(plans);
            Debug.Log($"[Asset Renamer] Renamed {applied} asset(s).");

            KeepUnresolved(plans);
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

        [SerializeField] private NamingConvention _convention = NamingConvention.PascalCase;
        [SerializeField] private bool _normalize = true;
        [SerializeField] private bool _applyPrefix = true;
        [SerializeField] private AssetTypePrefixTable _prefixTable;
        [SerializeField] private List<string> _paths = new List<string>();

        private VisualElement _root;

        #endregion
    }
}
