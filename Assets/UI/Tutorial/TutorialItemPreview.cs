using UnityEngine;
using OodlesEngine;

/// <summary>
/// 玩家選中可預覽的道具卡（如 Banana）時，在鏡頭前方生成一個半透明 / 無碰撞的預覽物件，
/// 隨玩家+鏡頭方向更新位置；按 E 使用時，TutorialFlow 會抓 preview 的位置當生成點。
/// </summary>
public class TutorialItemPreview : MonoBehaviour
{
    public static TutorialItemPreview Instance { get; private set; }

    [Header("預覽 prefab 對應（依 Card asset 名 m_Name 比對）")]
    [SerializeField] private GameObject bananaPreviewPrefab;
    // 之後要加新道具就在這裡加 SerializeField

    [Header("References")]
    [SerializeField] private TutorialInventory playerInventory;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform cameraTransform;

    [Header("Layout")]
    [SerializeField] private float distance     = 1.8f;
    [SerializeField] private float heightOffset = 1.0f;

    private GameObject _currentPreview;
    private string     _currentCardName;

    void Awake() { Instance = this; }

    void Start()
    {
        if (playerInventory == null)
        {
            var local = FindFirstObjectByType<LocalPlayer>();
            if (local != null) playerInventory = local.GetComponent<TutorialInventory>();
            if (playerInventory == null) playerInventory = FindFirstObjectByType<TutorialInventory>();
        }
        if (playerTransform == null && playerInventory != null) playerTransform = playerInventory.transform;
        if (cameraTransform == null && Camera.main != null)     cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        // 自動補抓 camera reference（OodlesEngine 有時會延後設定）
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;

        var wantedName = ResolveSelectedCardName();
        if (wantedName != _currentCardName)
        {
            DestroyPreview();
            if (!string.IsNullOrEmpty(wantedName)) SpawnPreviewFor(wantedName);
            _currentCardName = wantedName;
        }

        if (_currentPreview != null) UpdatePreviewTransform();
    }

    void OnDisable()  { DestroyPreview(); }
    void OnDestroy()  { DestroyPreview(); }

    // ──────────────────────────────────────────
    // public API
    // ──────────────────────────────────────────

    /// <summary>取得當前預覽物件的世界座標（讓 TutorialFlow 用作真正生成位置）</summary>
    public bool TryGetSpawnPose(out Vector3 pos, out Vector3 forward)
    {
        if (_currentPreview != null)
        {
            pos = _currentPreview.transform.position;
            forward = _currentPreview.transform.forward;
            return true;
        }
        pos = Vector3.zero; forward = Vector3.forward;
        return false;
    }

    public void ForceClear() { DestroyPreview(); _currentCardName = null; }

    // ──────────────────────────────────────────
    private string ResolveSelectedCardName()
    {
        if (playerInventory == null) return null;
        var card = playerInventory.GetSelected();
        if (card.IsEmpty()) return null;

        var cardSO = (CardManager.Instance != null) ? CardManager.Instance.GetCardScriptObject(card) : null;
        if (cardSO == null) return null;
        return ((Object)cardSO).name;
    }

    private GameObject GetPreviewPrefab(string cardName)
    {
        switch (cardName)
        {
            case "Banana": return bananaPreviewPrefab;
            default:       return null;
        }
    }

    private void SpawnPreviewFor(string cardName)
    {
        var prefab = GetPreviewPrefab(cardName);
        if (prefab == null) return;

        _currentPreview = Instantiate(prefab);
        StripPhysics(_currentPreview);
        UpdatePreviewTransform();
    }

    private void DestroyPreview()
    {
        if (_currentPreview != null) Destroy(_currentPreview);
        _currentPreview = null;
    }

    private void UpdatePreviewTransform()
    {
        if (_currentPreview == null) return;
        if (playerTransform == null) return;

        Vector3 fwd = (cameraTransform != null) ? cameraTransform.forward : playerTransform.forward;
        fwd.y = 0f; if (fwd.sqrMagnitude > 0.0001f) fwd.Normalize();

        var pos = playerTransform.position + Vector3.up * heightOffset + fwd * distance;
        _currentPreview.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(fwd, Vector3.up));
    }

    /// <summary>關閉預覽物件的物理 / 碰撞，避免影響玩家移動或產生 collision 事件</summary>
    private static void StripPhysics(GameObject go)
    {
        foreach (var rb in go.GetComponentsInChildren<Rigidbody>(true))
        {
            rb.isKinematic = true;
            rb.useGravity  = false;
        }
        foreach (var col in go.GetComponentsInChildren<Collider>(true))
        {
            col.enabled = false;
        }
        // 移除任何網路元件，避免干擾
        var fusionAsm = System.AppDomain.CurrentDomain.GetAssemblies();
        System.Type netType = null;
        foreach (var asm in fusionAsm)
        {
            netType = asm.GetType("Fusion.NetworkBehaviour");
            if (netType != null) break;
        }
        if (netType != null)
        {
            foreach (var c in go.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (c == null) continue;
                if (netType.IsAssignableFrom(c.GetType())) Destroy(c);
            }
        }
    }
}
