using Fusion;
using TMPro;
using UnityEngine;

public class PlayerIdentify : NetworkBehaviour
{
    [Networked] public int PlayerID { get; set; }

    [Networked, OnChangedRender(nameof(OnPlayerNameChanged))]
    public string PlayerName { get; set; }

    [Networked] public int SkinIndex { get; set; }

    /// <summary>
    /// 玩家識別色票。Alpha = 0 視為「未指定」（不套色，但材質仍會獨立 instance）。
    /// 透過 [Networked] 同步，所有 client 端都會在 OnChangedRender 套用。
    /// </summary>
    [Networked, OnChangedRender(nameof(OnTintColorChanged))]
    public Color TintColor { get; set; }

    public TextMeshProUGUI Text;

    [Header("色票套用目標")]
    [Tooltip("要套色的根物件（會抓它底下所有 Renderer）。留空就用 skinObjectName 自動找")]
    [SerializeField] private Transform skinRoot;
    [Tooltip("skinRoot 沒指定時，按這個名字往子物件找。預設 'Skin'")]
    [SerializeField] private string skinObjectName = "Skin";
    [Tooltip("只動材質名稱等於這個的 material（其他 material 不變色）。預設 'Skin'")]
    [SerializeField] private string skinMaterialName = "Skin";

    public override void Spawned()
    {
        base.Spawned();
        RefreshNameText();
        // 沒傳色票（alpha = 0）→ 不動任何材質，保留 prefab 共用 material
        if (TintColor.a > 0.001f)
            ApplyTint();
    }

    private void OnPlayerNameChanged()
    {
        RefreshNameText();
    }

    private void OnTintColorChanged()
    {
        if (TintColor.a > 0.001f)
            ApplyTint();
    }

    private void RefreshNameText()
    {
        if (Text != null)
            Text.text = PlayerName;

        // 本地玩家：把同步進來的真名也推給 HUD（解決 Client 端 GameStart 時序在 PlayerIdentify spawn 之前的問題）
        if (HasInputAuthority && GameHUDManager.Instance != null)
            GameHUDManager.Instance.RefreshPlayerName();
    }

    /// <summary>
    /// 在 Skin 根物件底下所有 Renderer 中，找名稱為 skinMaterialName 的 material，
    /// 套 TintColor 到它的 _BaseColor / _Color；其他 material 保持原狀。
    /// 透過 r.materials 觸發 Unity 自動 clone sharedMaterials → 此實例獨立，不影響其他玩家。
    /// </summary>
    private void ApplyTint()
    {
        Transform target = ResolveSkinRoot();
        if (target == null)
        {
            Debug.LogWarning($"[PlayerIdentify] 找不到 Skin 根物件（name='{skinObjectName}'），跳過套色");
            return;
        }

        var renderers = target.GetComponentsInChildren<Renderer>(true);
        bool tintedAny = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (r == null) continue;
            var mats = r.materials; // ← 讀取會自動 clone sharedMaterials 成 instance
            for (int j = 0; j < mats.Length; j++)
            {
                var m = mats[j];
                if (m == null) continue;
                if (!IsTargetMaterial(m, skinMaterialName)) continue; // 只動指定名稱的 material

                if (m.HasProperty("_BaseColor"))      m.SetColor("_BaseColor", TintColor); // URP Lit
                else if (m.HasProperty("_Color"))      m.SetColor("_Color",     TintColor); // 內建 / 舊版
                tintedAny = true;
            }
        }

        if (!tintedAny)
            Debug.LogWarning($"[PlayerIdentify] 在 '{target.name}' 底下找不到名稱為 '{skinMaterialName}' 的 material");
    }

    /// <summary>比對材質名稱（忽略 Unity 自動加的 ' (Instance)' 後綴）</summary>
    private static bool IsTargetMaterial(Material m, string targetName)
    {
        if (m == null || string.IsNullOrEmpty(targetName)) return false;
        string n = m.name;
        int idx = n.IndexOf(" (Instance)");
        if (idx >= 0) n = n.Substring(0, idx);
        return n == targetName;
    }

    private Transform ResolveSkinRoot()
    {
        if (skinRoot != null) return skinRoot;
        if (string.IsNullOrEmpty(skinObjectName)) return null;
        return FindChildByNameRecursive(transform, skinObjectName);
    }

    private static Transform FindChildByNameRecursive(Transform parent, string n)
    {
        if (parent == null) return null;
        if (parent.name == n) return parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            var found = FindChildByNameRecursive(parent.GetChild(i), n);
            if (found != null) return found;
        }
        return null;
    }
}
