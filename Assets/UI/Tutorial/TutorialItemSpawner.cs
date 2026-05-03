using UnityEngine;

/// <summary>
/// 教學版本地物件生成器：吃 ItemCard.itemPrefab，本地 Instantiate，
/// 移除網路元件並掛上對應的 Tutorial* 行為腳本。
/// 已支援：Banana、SlowTrap。其他 ItemCard 會原樣 spawn（沒有自訂行為）。
/// </summary>
public class TutorialItemSpawner : MonoBehaviour
{
    public static TutorialItemSpawner Instance { get; private set; }

    [Header("丟擲力（Banana 等需要往前飛的道具用）")]
    [SerializeField] private float defaultThrowForce = 10f;

    void Awake() { Instance = this; }

    /// <summary>
    /// 用 ItemCard 的 prefab 在 spawnPos 生成物件，朝 forward 方向。
    /// throwForce > 0 時對 Rigidbody 加 impulse。
    /// </summary>
    public GameObject SpawnFromCard(Card itemCardSO, Vector3 spawnPos, Vector3 forward, float throwForce = 0f, GameObject thrower = null)
    {
        var itemCard = itemCardSO as ItemCard;
        if (itemCard == null)
        {
            Debug.LogWarning("[TutorialItemSpawner] 不是 ItemCard：" + (itemCardSO != null ? itemCardSO.name : "null"));
            return null;
        }
        if (itemCard.itemPrefab == null)
        {
            Debug.LogWarning("[TutorialItemSpawner] ItemCard 沒設 itemPrefab：" + itemCard.name);
            return null;
        }

        var rot = forward.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(forward, Vector3.up) : Quaternion.identity;
        var go = Instantiate(itemCard.itemPrefab, spawnPos, rot);

        // 1. 移除網路元件
        StripNetworkComponents(go);

        // 2. 換成本地版行為腳本，並設置 thrower（避免打到自己）
        // 注意：Card class 自帶一個 public string name 欄位（會 shadow ScriptableObject.name）。
        // 它存的可能是中文顯示名（例如 "香蕉皮"），不能拿來做 type 判斷，要用 asset 真名。
        string assetName = ((UnityEngine.Object)itemCard).name;
        AttachLocalBehavior(go, assetName, thrower);

        // 3. 給初速
        if (throwForce > 0f)
        {
            var rb = go.GetComponent<Rigidbody>();
            if (rb == null) rb = go.GetComponentInChildren<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.AddForce(forward.normalized * throwForce, ForceMode.Impulse);
            }
        }

        return go;
    }

    /// <summary>給玩家位置 + 鏡頭方向，自己算 spawnPos 並丟出。</summary>
    public GameObject SpawnInFrontOf(Card itemCardSO, Transform origin, float forwardOffset = 1.2f, float throwForce = -1f)
    {
        if (origin == null) return null;
        if (throwForce < 0f) throwForce = defaultThrowForce;
        var fwd = origin.forward; fwd.y = 0f; fwd.Normalize();
        var pos = origin.position + Vector3.up * 0.6f + fwd * forwardOffset;
        return SpawnFromCard(itemCardSO, pos, fwd, throwForce);
    }

    // ------------------------------------------------------------
    private void StripNetworkComponents(GameObject root)
    {
        // 用反射避免硬編譯依賴 Fusion namespace
        var fusionAsm = System.AppDomain.CurrentDomain.GetAssemblies();
        System.Type netBehaviourType = null;
        System.Type netObjectType = null;
        foreach (var asm in fusionAsm)
        {
            if (netBehaviourType != null && netObjectType != null) break;
            var t1 = asm.GetType("Fusion.NetworkBehaviour");
            if (t1 != null) netBehaviourType = t1;
            var t2 = asm.GetType("Fusion.NetworkObject");
            if (t2 != null) netObjectType = t2;
        }

        // Destroy 所有 NetworkBehaviour 子類別
        if (netBehaviourType != null)
        {
            var comps = root.GetComponentsInChildren<MonoBehaviour>(includeInactive: true);
            foreach (var c in comps)
            {
                if (c == null) continue;
                if (netBehaviourType.IsAssignableFrom(c.GetType()))
                    Destroy(c);
            }
        }

        // Destroy NetworkObject
        if (netObjectType != null)
        {
            var no = root.GetComponent(netObjectType);
            if (no != null) Destroy(no);
            // 子物件也搜
            var nos = root.GetComponentsInChildren(netObjectType, includeInactive: true);
            foreach (var n in nos) if (n != null) Destroy(n);
        }
    }

    private void AttachLocalBehavior(GameObject go, string cardName, GameObject thrower)
    {
        switch (cardName)
        {
            case "Banana":
            {
                var b = go.AddComponent<TutorialBanana>();
                if (thrower != null) b.SetThrower(thrower);
                break;
            }
            case "SlowTrap":
                go.AddComponent<TutorialSlowTrap>();
                break;
        }
    }
}
