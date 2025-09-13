#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class InventoryInspectorWindow : EditorWindow
{
    [Serializable]
    private class PlayerEntry
    {
        public string DisplayName;
        public PlayerInventory Inventory;
        public Transform Root;
    }

    // ====== 狀態 ======
    [SerializeField] private CardCatalog _catalog;       // 指定卡片資料庫（可為 null）
    private Card[] _sourceCards = Array.Empty<Card>();   // 來源卡（由 Catalog 或專案掃描提供）
    private string[] _sourceNames = Array.Empty<string>();
    private int _selectedPlayerIdx = 0;
    private int _selectedCardIdx = 0;                    // 全域選取的卡（供 Replace ▼ 使用）
    private string _search = "";                         // 搜尋關鍵字
    private Vector2 _scroll;
    private List<bool> _slotFoldouts = new();

    private readonly List<PlayerEntry> _players = new();

    // ====== 入口 ======
    [MenuItem("Tools/Multiplayer/Inventory Inspector")]
    public static void Open()
    {
        var win = GetWindow<InventoryInspectorWindow>("Inventory Inspector");
        win.minSize = new Vector2(820, 460);
        win.RefreshAll();
    }

    private void OnEnable()
    {
        RefreshAll();
        EditorApplication.hierarchyChanged += OnHierarchyChanged;
        EditorApplication.playModeStateChanged += _ => Repaint();
    }

    private void OnDisable()
    {
        EditorApplication.hierarchyChanged -= OnHierarchyChanged;
    }

    private void OnHierarchyChanged()
    {
        RefreshPlayersOnly();
        Repaint();
    }

    // ====== 掃描 ======
    private void RefreshAll()
    {
        BuildSourceFromCatalogOrProject();
        RefreshPlayersOnly();
    }

    private void BuildSourceFromCatalogOrProject()
    {
        // 先以 Catalog 為主
        if (_catalog != null && _catalog.cards != null && _catalog.cards.Count > 0)
        {
            _sourceCards = _catalog.cards
                .Where(c => c != null)
                .OrderBy(c => c.cardData.type).ThenBy(c => c.cardData.id).ThenBy(c => c.name)
                .ToArray();
        }
        else
        {
            // 沒指定 Catalog → 掃描專案所有 Card ScriptableObject
            var guids = AssetDatabase.FindAssets("t:Card");
            _sourceCards = guids
                .Select(g => AssetDatabase.LoadAssetAtPath<Card>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(a => a != null)
                .OrderBy(c => c.cardData.type).ThenBy(c => c.cardData.id).ThenBy(c => c.name)
                .ToArray();
        }

        var list = new List<string> { "(Empty) [-1, None]" }; // index 0 = 空卡
        list.AddRange(_sourceCards.Select(c => $"{c.name}  [ID:{c.cardData.id}, {c.cardData.type}]"));
        _sourceNames = list.ToArray();
        _selectedCardIdx = Mathf.Clamp(_selectedCardIdx, 0, _sourceNames.Length - 1);
    }

    private void RefreshPlayersOnly()
    {
        _players.Clear();

        var mgr = FindObjectOfType<PlayerInventoryManager>();
        if (mgr != null)
        {
            // 嘗試取快取欄位
            var parents = GetPrivateField<List<GameObject>>(mgr, "playerParents") ?? new List<GameObject>();
            var inventories = GetPrivateField<List<PlayerInventory>>(mgr, "playerInventories") ?? new List<PlayerInventory>();

            if (inventories.Count == 0)
            {
                // 嘗試呼叫公開 Refresh
                var refresh = mgr.GetType().GetMethod("Refresh", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                refresh?.Invoke(mgr, null);
                inventories = GetPrivateField<List<PlayerInventory>>(mgr, "playerInventories") ?? new List<PlayerInventory>();
            }

            foreach (var inv in inventories)
            {
                if (inv == null) continue;
                var root = inv.transform.root;
                _players.Add(new PlayerEntry
                {
                    DisplayName = $"{root.name} ({inv.name})",
                    Inventory = inv,
                    Root = root
                });
            }
        }

        if (_players.Count == 0)
        {
            foreach (var inv in FindObjectsOfType<PlayerInventory>(true))
            {
                var root = inv.transform.root;
                _players.Add(new PlayerEntry
                {
                    DisplayName = $"{root.name} ({inv.name})",
                    Inventory = inv,
                    Root = root
                });
            }
        }

        _players.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.Ordinal));
        _selectedPlayerIdx = Mathf.Clamp(_selectedPlayerIdx, 0, Math.Max(0, _players.Count - 1));
    }

    private static T GetPrivateField<T>(object obj, string fieldName) where T : class
    {
        if (obj == null) return null;
        var fi = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        return fi?.GetValue(obj) as T;
    }

    // ====== GUI ======
    private void OnGUI()
    {
        DrawToolbar();

        if (_players.Count == 0)
        {
            EditorGUILayout.HelpBox("找不到任何 PlayerInventory。請先進入場景或按 Rescan Players。", MessageType.Info);
            return;
        }

        // 玩家下拉
        _selectedPlayerIdx = EditorGUILayout.Popup("Player", _selectedPlayerIdx, _players.Select(p => p.DisplayName).ToArray());
        var player = _players[_selectedPlayerIdx];
        if (player.Inventory == null)
        {
            EditorGUILayout.HelpBox("此玩家沒有 PlayerInventory。", MessageType.Warning);
            return;
        }

        // 資料來源：Catalog + 搜尋 + 全域卡選擇
        EditorGUILayout.Space(6);
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("Card Source", EditorStyles.boldLabel);
            var newCatalog = (CardCatalog)EditorGUILayout.ObjectField("Card Catalog", _catalog, typeof(CardCatalog), false);
            if (newCatalog != _catalog)
            {
                _catalog = newCatalog;
                BuildSourceFromCatalogOrProject();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                _search = EditorGUILayout.TextField("Search", _search);
                if (GUILayout.Button("Clear", GUILayout.Width(64))) _search = string.Empty;
            }

            // 全域候選卡下拉（經過搜尋過濾）
            var filtered = FilterCardsBySearch(_sourceCards, _search).ToArray();
            var filteredNames = BuildPopupNames(filtered);

            // 讓 _selectedCardIdx 對應到「完整庫」中的索引；這裡顯示時只顯示過濾後
            int mappedIndex = MapSelectedToFilteredIndex(_selectedCardIdx, filtered);
            int newMappedIndex = EditorGUILayout.Popup("Global Select", mappedIndex, filteredNames);
            if (newMappedIndex != mappedIndex)
            {
                // 把過濾後的選擇反映回完整庫索引
                _selectedCardIdx = MapFilteredToFullIndex(newMappedIndex, filtered);
            }

            // 拖放提示
            EditorGUILayout.HelpBox("你也可以直接拖一個 Card 到單格的「Card」欄位再按 Drag。", MessageType.None);
        }

        EditorGUILayout.Space(8);
        DrawInventoryGrid(player.Inventory);
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            if (GUILayout.Button("Rescan Players", EditorStyles.toolbarButton, GUILayout.Width(120)))
                RefreshPlayersOnly();

            if (GUILayout.Button("Reload Cards", EditorStyles.toolbarButton, GUILayout.Width(120)))
                BuildSourceFromCatalogOrProject();

            GUILayout.FlexibleSpace();
            GUILayout.Label($"Mode: {(EditorApplication.isPlaying ? "PLAY" : "EDIT")}", EditorStyles.miniLabel);
        }
    }

    private void DrawInventoryGrid(PlayerInventory inv)
    {
        if (inv.slots == null || inv.slots.Length == 0)
        {
            EditorGUILayout.HelpBox("slots 為空或未初始化。", MessageType.Error);
            return;
        }

        EditorGUILayout.LabelField($"Inventory of: {inv.transform.root.name}", EditorStyles.boldLabel);

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        var box = new GUIStyle("box");

        for (int i = 0; i < inv.slots.Length; i++)
        {
            using (new EditorGUILayout.VerticalScope(box))
            {
                var data = inv.slots[i];
                EditorGUILayout.LabelField($"Slot {i}", EditorStyles.boldLabel);

                if (data.IsEmpty())
                {
                    EditorGUILayout.LabelField("名稱", "空白");
                    EditorGUILayout.LabelField("類型", "空白");
                }
                else
                {
                    string cardName = "Unknown";
                    Card cardAsset = _sourceCards?.FirstOrDefault(ca => ca != null && ca.cardData.id == data.id && ca.cardData.type == data.type);
                    if (cardAsset != null) cardName = cardAsset.name;

                    EditorGUILayout.LabelField("名稱", cardName);
                    EditorGUILayout.LabelField("類型", data.type.ToString());
                }

                // 編輯功能
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    var filtered = FilterCardsBySearch(_sourceCards, _search).ToArray();
                    var filteredNames = BuildPopupNames(filtered);

                    int slotMappedIdx = MapCardDataToFilteredIndex(data, filtered);
                    int newSlotMappedIdx = EditorGUILayout.Popup("Pick", slotMappedIdx, filteredNames);
                    if (newSlotMappedIdx != slotMappedIdx)
                    {
                        var chosen = GetCardFromFilteredIndex(newSlotMappedIdx, filtered);
                        ApplyReplace(inv, i, chosen);
                    }

                    if (GUILayout.Button("Clear (Empty)", GUILayout.Height(20)))
                        ApplyReplace(inv, i, null);
                }
            }

            GUILayout.Space(6); // Slot 之間留白
        }

        EditorGUILayout.EndScrollView();

        // 全部操作
        EditorGUILayout.Space(4);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Replace ALL with Global Card", GUILayout.Height(26)))
            {
                var card = GetCardFromFullIndex(_selectedCardIdx);
                Undo.RecordObject(inv, "Replace ALL Slots");
                for (int i = 0; i < inv.slots.Length; i++)
                    inv.slots[i] = card == null ? CardData.Empty() : ToData(card);
                EditorUtility.SetDirty(inv);
                CallNotify(inv);
            }

            if (GUILayout.Button("Clear ALL", GUILayout.Height(26)))
            {
                Undo.RecordObject(inv, "Clear ALL Slots");
                for (int i = 0; i < inv.slots.Length; i++)
                    inv.slots[i] = CardData.Empty();
                EditorUtility.SetDirty(inv);
                CallNotify(inv);
            }
        }
    }

    private void CallNotify(PlayerInventory inv)
    {
        var notify = inv.GetType().GetMethod("NotifyChanged",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        notify?.Invoke(inv, null);
        Repaint();
    }


    // ====== 套用動作 ======
    private void ApplyReplace(PlayerInventory inv, int slotIndex, Card cardAssetOrNull)
    {
        if (inv == null || inv.slots == null || slotIndex < 0 || slotIndex >= inv.slots.Length) return;

        Undo.RecordObject(inv, "Replace Inventory Slot");
        inv.slots[slotIndex] = cardAssetOrNull == null ? CardData.Empty() : ToData(cardAssetOrNull);
        EditorUtility.SetDirty(inv);

        // 你的 PlayerInventory.NotifyChanged() 會呼叫 PlayerInventoryManager.Instance.Refresh();
        // 這裡若需要即時也可手動呼叫（若你把 NotifyChanged 設為 public）
        var notify = inv.GetType().GetMethod("NotifyChanged", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        notify?.Invoke(inv, null);

        Repaint();
    }

    private void ApplyAddFirstEmpty(PlayerInventory inv, Card cardAsset)
    {
        if (inv == null || cardAsset == null) return;

        // 直接使用你定義的 AddCard（會自己找第一個空格）
        var add = inv.GetType().GetMethod("AddCard", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        if (add != null)
        {
            Undo.RecordObject(inv, "Add Card (First Empty)");
            add.Invoke(inv, new object[] { ToData(cardAsset) });
            EditorUtility.SetDirty(inv);
        }
        else
        {
            // 後備：直接尋找第一個空格
            var data = ToData(cardAsset);
            for (int i = 0; i < inv.slots.Length; i++)
            {
                if (inv.slots[i].IsEmpty())
                {
                    ApplyReplace(inv, i, cardAsset);
                    break;
                }
            }
        }

        Repaint();
    }

    private static CardData ToData(Card c)
    {
        // 目前 Card 沒有 cooldown，維持 0f；若未來加欄位，在此一併映射
        return new CardData(c.cardData.id, c.cardData.type, 0f);
    }

    // ====== 搜尋/映射工具 ======
    private IEnumerable<Card> FilterCardsBySearch(IEnumerable<Card> src, string search)
    {
        if (src == null) yield break;
        if (string.IsNullOrWhiteSpace(search))
        {
            foreach (var c in src) yield return c;
            yield break;
        }

        search = search.Trim();
        foreach (var c in src)
        {
            if (c == null) continue;
            // name / id / type 任何一個包含就通過
            if (c.name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) { yield return c; continue; }
            if (c.cardData.id.ToString().IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) { yield return c; continue; }
            if (c.cardData.type.ToString().IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) { yield return c; continue; }
        }
    }

    private string[] BuildPopupNames(Card[] arr)
    {
        var list = new List<string> { "(Empty) [-1, None]" };
        if (arr != null)
            list.AddRange(arr.Select(c => c != null ? $"{c.name}  [ID:{c.cardData.id}, {c.cardData.type}]" : "(null)"));
        return list.ToArray();
    }

    private int MapSelectedToFilteredIndex(int fullIndex, Card[] filtered)
    {
        // fullIndex=0 → 空卡
        if (fullIndex <= 0) return 0;
        var fullCard = GetCardFromFullIndex(fullIndex);
        if (fullCard == null) return 0;

        // filtered[0] 是空卡，所以從 1 起算
        for (int i = 0; i < filtered.Length; i++)
        {
            var c = filtered[i];
            if (c != null && c.cardData.id == fullCard.cardData.id && c.cardData.type == fullCard.cardData.type)
                return i + 1;
        }
        return 0;
    }

    private int MapFilteredToFullIndex(int mappedIndex, Card[] filtered)
    {
        // mappedIndex=0 → 空卡
        if (mappedIndex <= 0) return 0;

        int fi = mappedIndex - 1;
        if (fi < 0 || fi >= filtered.Length) return 0;

        var target = filtered[fi];
        if (target == null) return 0;

        // 對應回完整庫（_sourceCards）
        int idx = Array.FindIndex(_sourceCards, c => c != null && c.cardData.id == target.cardData.id && c.cardData.type == target.cardData.type);
        return (idx >= 0) ? (idx + 1) : 0;
    }

    private int MapCardDataToFilteredIndex(CardData data, Card[] filtered)
    {
        if (data.IsEmpty()) return 0;
        for (int i = 0; i < filtered.Length; i++)
        {
            var c = filtered[i];
            if (c != null && c.cardData.id == data.id && c.cardData.type == data.type)
                return i + 1; // +1 因為 0 是空卡
        }
        return 0;
    }

    private Card GetCardFromFullIndex(int fullIndex)
    {
        if (fullIndex <= 0) return null;
        int i = fullIndex - 1;
        if (_sourceCards == null || i < 0 || i >= _sourceCards.Length) return null;
        return _sourceCards[i];
    }

    private Card GetCardFromFilteredIndex(int mappedIndex, Card[] filtered)
    {
        if (mappedIndex <= 0) return null;
        int i = mappedIndex - 1;
        if (filtered == null || i < 0 || i >= filtered.Length) return null;
        return filtered[i];
    }
}
#endif
