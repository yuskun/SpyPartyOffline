using Fusion;
using OodlesEngine;
using UnityEngine;

public class AIController : NetworkBehaviour
{
    // ── Components ──────────────────────────────────────────────
    private OodlesCharacter  characterController;
    private PlayerInventory  inventory;
    private PlayerIdentify   identify;
    private NetworkPlayer    networkPlayer;

    // ── AI Input ─────────────────────────────────────────────────
    private OodlesCharacterInput aiInput;

    // ── Movement ─────────────────────────────────────────────────
    private GameObject currentTarget;
    private Vector3    wanderTarget;
    private float      wanderTimer;

    // ── Item Seeking ──────────────────────────────────────────────
    [SerializeField] private float itemSeekRange  = 20f;   // 感知道具的最大距離
    [SerializeField] private float weaponPriority = 1.5f;  // 武器的距離加權（越高越優先）

    // ── Card Usage ───────────────────────────────────────────────
    [SerializeField] private float cardInterval = 6f;
    [SerializeField] private float cardUseRange = 5f;  // 需要目標的卡牌最大使用距離
    private float cardCooldown;

    // ────────────────────────────────────────────────────────────

    public override void Spawned()
    {
        networkPlayer        = GetComponent<NetworkPlayer>();
        characterController  = GetComponent<OodlesCharacter>();
        inventory            = GetComponent<PlayerInventory>();
        identify             = GetComponent<PlayerIdentify>();

        // 只對 AI 玩家（PlayerRef.None）啟用
        if (networkPlayer == null || networkPlayer.PlayerId != PlayerRef.None)
        {
            enabled = false;
            return;
        }

        // 關閉人類輸入，改由本腳本驅動
        networkPlayer.AllowInput = false;

        // 錯開各 AI 的首次出牌時機，避免同一幀同時使用
        cardCooldown = Random.Range(3f, cardInterval);
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;
        if (characterController == null || identify == null) return;

        // 卡牌計時
        cardCooldown -= Runner.DeltaTime;
        if (cardCooldown <= 0f)
        {
            TryUseCard();
            cardCooldown = cardInterval;
        }

        // 更新移動 / 攻擊輸入
        UpdateAIInput();

        // 驅動角色
        characterController.ProcessInput(aiInput);
    }

    // ── Movement & Attack ────────────────────────────────────────

    private void UpdateAIInput()
    {
        aiInput = new OodlesCharacterInput(
            0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f,
            Vector3.forward, Runner.DeltaTime, Runner.Tick
        );

        // 背包有空格時優先撿道具
        bool wantsItem = HasEmptySlot();
        if (wantsItem)
        {
            GameObject nearestItem = FindNearestItem();
            if (nearestItem != null)
            {
                MoveToward(nearestItem.transform.position);
                return;
            }
        }

        // 沒道具可撿 → 追最近玩家
        currentTarget = FindNearestPlayer();

        if (currentTarget == null)
        {
            DoWander();
            return;
        }

        float dist = Vector3.Distance(GetMyPos(), GetPos(currentTarget));

        if (dist < 1.5f)
        {
            aiInput.fire1Axis = 1f;  // 攻擊
        }
        else
        {
            MoveToward(GetPos(currentTarget));  // 追擊
        }
    }

    // 找場景內最近的可拾取道具（武器優先）
    private GameObject FindNearestItem()
    {
        Vector3      myPos = GetMyPos();
        PlayerItem[] items = UnityEngine.Object.FindObjectsByType<PlayerItem>(FindObjectsSortMode.None);

        GameObject best      = null;
        float      bestScore = float.MaxValue;  // 分數越小越優先

        foreach (var item in items)
        {
            if (item == null || !item.gameObject.activeInHierarchy) continue;

            float dist = Vector3.Distance(myPos, item.transform.position);
            if (dist > itemSeekRange) continue;

            // 武器道具距離縮小（提升優先級）
            bool isWeapon = item.cardData.type == CardType.Item &&
                            IsWeaponItem(item);
            float score = isWeapon ? dist / weaponPriority : dist;

            if (score < bestScore) { bestScore = score; best = item.gameObject; }
        }
        return best;
    }

    private bool IsWeaponItem(PlayerItem item)
    {
        // 嘗試取得對應的 ItemCard ScriptableObject 判斷是否為武器
        if (CardManager.Instance == null) return false;
        ItemCard ic = CardManager.Instance.GetItemCard(item.cardData.id);
        return ic != null && ic.WeaponItem;
    }

    private bool HasEmptySlot()
    {
        if (inventory == null) return false;
        for (int i = 0; i < PlayerInventory.MaxSlots; i++)
            if (inventory.slots[i].IsEmpty()) return true;
        return false;
    }

    private void DoWander()
    {
        wanderTimer -= Runner.DeltaTime;
        if (wanderTimer <= 0f)
        {
            wanderTarget = GetMyPos() + new Vector3(
                Random.Range(-6f, 6f), 0f, Random.Range(-6f, 6f));
            wanderTimer = Random.Range(2f, 5f);
        }
        MoveToward(wanderTarget);
    }

    private void MoveToward(Vector3 target)
    {
        Vector3 dir = (target - GetMyPos());
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;
        dir.Normalize();

        aiInput.cameraForward = dir;
        aiInput.forwardAxis   = 1f;
    }

    // ── Card Usage ───────────────────────────────────────────────

    private void TryUseCard()
    {
        if (inventory == null || CardManager.Instance == null) return;
        if (PlayerInventoryManager.Instance == null) return;

        int myId = identify.PlayerID;

        // 找目標（供需要目標的卡牌使用）
        GameObject targetObj    = FindNearestPlayer();
        PlayerInventory targetInv = targetObj?.GetComponentInChildren<PlayerInventory>()
                                    ?? targetObj?.GetComponent<PlayerInventory>();
        int targetId = targetObj?.GetComponent<PlayerIdentify>()?.PlayerID ?? myId;

        float distToTarget = targetObj != null
            ? Vector3.Distance(GetMyPos(), GetPos(targetObj))
            : float.MaxValue;

        for (int i = 0; i < PlayerInventory.MaxSlots; i++)
        {
            CardData card = inventory.slots[i];
            if (card.IsEmpty()) continue;
            if (!inventory.CanUse(card)) continue;

            bool canUse  = false;
            int  useTarget = myId;  // 預設目標為自己

            switch (card.type)
            {
                case CardType.Mission:
                {
                    MissionCard mc = CardManager.Instance.GetMissionCard(card.id);
                    if (mc == null) continue;
                    if (mc.needTarget && (targetInv == null || distToTarget > cardUseRange)) continue;
                    PlayerInventory t = mc.needTarget ? targetInv : null;
                    canUse = mc.CanUse(inventory, t, card);
                    if (mc.needTarget) useTarget = targetId;
                    break;
                }
                case CardType.Function:
                {
                    FunctionCard fc = CardManager.Instance.GetFunctionCard(card.id);
                    if (fc == null) continue;
                    if (fc.needTarget && (targetInv == null || distToTarget > cardUseRange)) continue;
                    PlayerInventory t = fc.needTarget ? targetInv : null;
                    canUse = fc.CanUse(inventory, t);
                    if (fc.needTarget) useTarget = targetId;
                    break;
                }
                case CardType.Item:
                {
                    ItemCard ic = CardManager.Instance.GetItemCard(card.id);
                    if (ic == null) continue;
                    if (ic.needTarget && (targetInv == null || distToTarget > cardUseRange)) continue;
                    canUse = true;
                    if (ic.needTarget) useTarget = targetId;
                    break;
                }
            }

            if (!canUse) continue;

            CardUseParameters p = new CardUseParameters
            {
                UserId            = myId,
                TargetId          = useTarget,
                Card              = card,
                UseCardIndex      = i,
                SelectIndex       = i,
                TargetSelectIndex = GetFirstFilledSlot(targetInv),
            };

            CardManager.Instance.UseCard(p);
            Debug.Log($"[AI] PlayerID={myId} 使用卡牌 type={card.type} id={card.id} → target={useTarget}");
            return;  // 每次只用一張
        }
    }

    // ── Helpers ──────────────────────────────────────────────────

    private GameObject FindNearestPlayer()
    {
        if (PlayerInventoryManager.Instance == null) return null;

        Vector3    myPos      = GetMyPos();
        int        myId       = identify.PlayerID;
        GameObject nearest    = null;
        float      nearestDist = float.MaxValue;

        foreach (var p in PlayerInventoryManager.Instance.playerParents)
        {
            if (p == null) continue;
            var pid = p.GetComponent<PlayerIdentify>();
            if (pid == null || pid.PlayerID == myId) continue;

            float d = Vector3.Distance(myPos, GetPos(p));
            if (d < nearestDist) { nearestDist = d; nearest = p; }
        }
        return nearest;
    }

    private Vector3 GetMyPos()
    {
        Transform r = transform.Find("Ragdoll");
        return r != null ? r.position : transform.position;
    }

    private Vector3 GetPos(GameObject go)
    {
        Transform r = go.transform.Find("Ragdoll");
        return r != null ? r.position : go.transform.position;
    }

    private int GetFirstFilledSlot(PlayerInventory inv)
    {
        if (inv == null) return 0;
        for (int i = 0; i < PlayerInventory.MaxSlots; i++)
            if (!inv.slots[i].IsEmpty()) return i;
        return 0;
    }
}
