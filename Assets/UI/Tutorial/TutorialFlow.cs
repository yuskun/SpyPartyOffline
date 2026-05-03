using UnityEngine;
using OodlesEngine;

/// <summary>
/// 教學流程主控：把 TutorialManager 的互動事件（pick/use/move）對應到
/// TutorialController 的 15 步 dialogue。處理新增的 info-step（HUD 介紹、
/// 道具圖鑑 modal、任務卡 modal、結算）以及切換道具次數偵測。
/// </summary>
public class TutorialFlow : MonoBehaviour
{
    public static TutorialFlow Instance;

    [Header("References (auto-find on Awake if null)")]
    [SerializeField] private TutorialController       controller;
    [SerializeField] private TutorialManager          tutorialManager;
    [SerializeField] private TutorialInventory        playerInventory;
    [SerializeField] private TutorialScanner          playerScanner;
    [SerializeField] private TutorialCardUseManager   cardUseManager;

    [Header("Behavior")]
    [SerializeField] private bool startCollapsed = false;
    [Tooltip("步驟 4 完成所需的切換次數")]
    [SerializeField] private int switchCountToAdvance = 3;

    // 對應 TutorialController 的步驟 index（已移除「認識介面」step）
    private const int STEP_MOVE         = 0;
    private const int STEP_PICK_BOX     = 1;
    private const int STEP_SWITCH_ITEM  = 2;
    private const int STEP_USE_ITEM     = 3;
    private const int STEP_ITEMS_MODAL  = 4;
    private const int STEP_PRACTICE     = 5;
    private const int STEP_TOOLTIP      = 6;
    private const int STEP_PICK_WEAPON  = 7;
    private const int STEP_ATTACK       = 8;
    private const int STEP_PICK_CARD    = 9;
    private const int STEP_MISSION_MODAL= 10;
    private const int STEP_STEAL_FIRST  = 11;
    private const int STEP_STEAL_REST   = 12;
    private const int STEP_DONE         = 13;

    /// <summary>每步驟解鎖的操控位元；用 Has/Or 組合。</summary>
    [System.Flags]
    public enum Controls
    {
        None         = 0,
        Move         = 1 << 0,
        Switch       = 1 << 1,  // 1-6 / 滾輪切換
        UseItem      = 1 << 2,  // E 用道具卡
        PickupWeapon = 1 << 3,  // RMB 撿武器
        Attack       = 1 << 4,  // LMB 揮擊
        All          = -1
    }

    /// <summary>當前解鎖的操控位元（依步驟）</summary>
    public Controls UnlockedNow => GetControlsForStep(controller != null ? controller.CurrentStep : 0);

    private Controls GetControlsForStep(int step)
    {
        // 0 移動 / 1 撿盒：只能走
        if (step <= STEP_PICK_BOX)    return Controls.Move;
        // 2 切換道具：解鎖切換
        if (step == STEP_SWITCH_ITEM) return Controls.Move | Controls.Switch;
        // 3 用 Peek / 4 道具圖鑑 modal / 5 練習 Banana / 6 道具說明：解鎖 E
        if (step <= STEP_TOOLTIP)     return Controls.Move | Controls.Switch | Controls.UseItem;
        // 7 撿武器：解鎖 RMB
        if (step == STEP_PICK_WEAPON) return Controls.Move | Controls.Switch | Controls.UseItem | Controls.PickupWeapon;
        // 8+ 揮擊到結束：全部解鎖
        return Controls.All;
    }

    private bool Can(Controls c) => (UnlockedNow & c) != 0;

    private int _switchCount = 0;

    void Awake()
    {
        Instance = this;
        if (controller == null) controller = FindFirstObjectByType<TutorialController>();
        if (tutorialManager == null) tutorialManager = FindFirstObjectByType<TutorialManager>();
        if (playerInventory == null)
        {
            var local = FindFirstObjectByType<LocalPlayer>();
            if (local != null) playerInventory = local.GetComponent<TutorialInventory>();
            if (playerInventory == null) playerInventory = FindFirstObjectByType<TutorialInventory>();
        }
        if (playerScanner == null)  playerScanner  = FindFirstObjectByType<TutorialScanner>();
        if (cardUseManager == null) cardUseManager = FindFirstObjectByType<TutorialCardUseManager>();
    }

    void Start()
    {
        if (controller == null)
        {
            Debug.LogError("[TutorialFlow] 找不到 TutorialController");
            enabled = false; return;
        }

        // 強制由本流程接管
        controller.externallyControlled = true;
        controller.GoToStep(STEP_MOVE);
        if (startCollapsed) controller.ToggleCollapse();

        if (tutorialManager != null)
        {
            tutorialManager.OnInteraction += HandleInteraction;
            tutorialManager.OnStepChanged += HandleTMStepChanged;
        }
        else
        {
            Debug.LogWarning("[TutorialFlow] 找不到 TutorialManager；只會處理 info-step 推進");
        }

        if (cardUseManager != null) cardUseManager.OnCardUsed += HandleCardUsed;
    }

    void OnDestroy()
    {
        if (tutorialManager != null)
        {
            tutorialManager.OnInteraction -= HandleInteraction;
            tutorialManager.OnStepChanged -= HandleTMStepChanged;
        }
        if (cardUseManager != null) cardUseManager.OnCardUsed -= HandleCardUsed;
    }

    // ---------- E 鍵：使用選中的卡 ----------
    // Function 卡（Peek/Give/Swap）→ 必須瞄準 scanner.currentTarget
    // Item   卡（Banana/SlowTrap）→ 直接生成在玩家身前，不需目標
    private void TryUseSelectedCard()
    {
        if (playerInventory == null) return;
        var card = playerInventory.GetSelected();
        if (card.IsEmpty()) { Debug.Log("[TutorialFlow] 沒有選中道具"); return; }

        var cardSO = (CardManager.Instance != null) ? CardManager.Instance.GetCardScriptObject(card) : null;
        if (cardSO == null) { Debug.LogWarning("[TutorialFlow] CardManager 找不到卡片 SO"); return; }

        var tgtGo = playerScanner != null ? playerScanner.currentTarget : null;

        // Item 卡：直接生成（不要求目標）
        if (card.type == CardType.Item)
        {
            if (TryUseItemCard(cardSO, tgtGo))
            {
                playerInventory.RemoveAt(playerInventory.SelectedIndex);
                FireItemCardUsed(cardSO);
            }
            return;
        }

        // Function 卡：必須有 target 與其 inventory
        if (card.type == CardType.Function)
        {
            if (tgtGo == null) { Debug.Log("[TutorialFlow] Function 卡需要瞄準目標"); return; }
            var targetInv = tgtGo.GetComponent<TutorialInventory>();
            if (targetInv == null) { Debug.Log("[TutorialFlow] 目標沒有 TutorialInventory"); return; }
            if (cardUseManager == null) { Debug.LogWarning("[TutorialFlow] 缺 cardUseManager"); return; }
            cardUseManager.TryUseFunctionCard(cardSO, playerInventory, targetInv, playerInventory.SelectedIndex);
            return;
        }

        Debug.Log($"[TutorialFlow] 不支援使用 {((UnityEngine.Object)cardSO).name} (type={card.type})");
    }

    /// <summary>處理 Item 卡使用效果。優先使用 TutorialItemSpawner 把 prefab 生出來；
    /// 沒有 spawner 才 fallback 直接擊倒。回傳是否真的觸發。</summary>
    private bool TryUseItemCard(Card cardSO, GameObject targetGo)
    {
        // 1. 用本地 spawner 生成 ItemCard 的 prefab（Banana 會飛、SlowTrap 會落地）
        if (TutorialItemSpawner.Instance != null && playerInventory != null)
        {
            // 起點：玩家位置 + 鏡頭朝向
            var playerT = playerInventory.transform;
            var cam = (playerScanner != null && playerScanner.cameraTransform != null)
                      ? playerScanner.cameraTransform : Camera.main?.transform;
            var fwd = (cam != null) ? cam.forward : playerT.forward;
            fwd.y = 0f; if (fwd.sqrMagnitude > 0.0001f) fwd.Normalize();

            switch (((UnityEngine.Object)cardSO).name)
            {
                case "Banana":
                {
                    // 優先用預覽位置（玩家+鏡頭算好的點）；沒預覽就用 fallback
                    Vector3 pos; Vector3 spawnFwd;
                    if (TutorialItemPreview.Instance != null
                        && TutorialItemPreview.Instance.TryGetSpawnPose(out pos, out spawnFwd))
                    {
                        // 用預覽方向當投擲方向
                        spawnFwd.y = 0f;
                        if (spawnFwd.sqrMagnitude > 0.0001f) spawnFwd.Normalize();
                        else spawnFwd = fwd;
                    }
                    else
                    {
                        pos = playerT.position + Vector3.up * 1.2f + fwd * 1.5f;
                        spawnFwd = fwd;
                    }
                    var go = TutorialItemSpawner.Instance.SpawnFromCard(cardSO, pos, spawnFwd, throwForce: 14f, thrower: playerT.gameObject);
                    if (go != null && TutorialItemPreview.Instance != null) TutorialItemPreview.Instance.ForceClear();
                    return go != null;
                }
                case "SlowTrap":
                {
                    Vector3 pos; Vector3 spawnFwd;
                    if (TutorialItemPreview.Instance != null
                        && TutorialItemPreview.Instance.TryGetSpawnPose(out pos, out spawnFwd))
                    {
                        spawnFwd.y = 0f;
                        if (spawnFwd.sqrMagnitude > 0.0001f) spawnFwd.Normalize();
                        else spawnFwd = fwd;
                    }
                    else
                    {
                        pos = playerT.position + Vector3.up * 0.4f + fwd * 0.8f;
                        spawnFwd = fwd;
                    }
                    var go = TutorialItemSpawner.Instance.SpawnFromCard(cardSO, pos, spawnFwd, throwForce: 0f, thrower: playerT.gameObject);
                    if (go != null && TutorialItemPreview.Instance != null) TutorialItemPreview.Instance.ForceClear();
                    return go != null;
                }
            }
        }

        // 2. Fallback：沒 spawner / 沒 prefab → 至少把 Banana 擊倒效果做出來
        if (((UnityEngine.Object)cardSO).name == "Banana")
        {
            TutorialKnockDown(targetGo);
            return true;
        }

        Debug.Log("[TutorialFlow] 教學沒處理的 Item 卡：" + ((UnityEngine.Object)cardSO).name);
        return false;
    }

    /// <summary>本地版擊倒：呼叫 OodlesCharacter.KnockDown，但容錯 PlayerInventory 缺漏</summary>
    private void TutorialKnockDown(GameObject targetGo)
    {
        var oc = targetGo.GetComponent<OodlesEngine.OodlesCharacter>();
        if (oc == null) { Debug.LogWarning("[TutorialFlow] target 沒有 OodlesCharacter，無法擊倒"); return; }

        try { oc.KnockDown(); }
        catch (System.Exception e)
        {
            // 假人沒有 PlayerInventory，KnockDown 內部會炸；視覺部分（ragdollMode 等）通常已經 set 完
            Debug.Log("[TutorialFlow] KnockDown 部分失敗，補上 ragdollMode：" + e.Message);
            oc.ragdollMode = true;
            try { oc.ChangeState(OodlesEngine.OodlesCharacter.State.LostControl); } catch { }
        }
    }

    private void FireItemCardUsed(Card cardSO)
    {
        if (TutorialManager.Instance == null) return;
        if (((UnityEngine.Object)cardSO).name == "Banana") TutorialManager.Instance.OnUseBanana();
        // 之後其它 Item 卡可以加在這
    }

    /// <summary>Function 卡使用完（透過 cardUseManager）→ fire TutorialManager 推進步驟</summary>
    private void HandleCardUsed(Card card, TutorialInventory user, TutorialInventory target)
    {
        if (TutorialManager.Instance == null) return;
        if (card is Peek) TutorialManager.Instance.OnUsePeek();
        else if (card is Give) { /* 教學流程沒有 Give 步驟 */ }
        else if (card is Swap) { /* 教學流程沒有 Swap 步驟 */ }
    }

    void Update()
    {
        if (controller == null) return;
        int cur = controller.CurrentStep;

        // info-step 用 Enter 推進
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (IsInfoStep(cur))
            {
                AdvanceTo(cur + 1);
                return;
            }
        }

        // E 鍵：對 scanner.currentTarget 使用選中的卡（要先解鎖 UseItem）
        if (Can(Controls.UseItem) && Input.GetKeyDown(KeyCode.E))
        {
            TryUseSelectedCard();
        }

        // 切換道具：只有解鎖 Switch 才處理輸入
        if (Can(Controls.Switch) && playerInventory != null)
        {
            bool switched = playerInventory.HandleSwitchInput();
            if (switched && cur == STEP_SWITCH_ITEM)
            {
                _switchCount++;
                if (_switchCount >= switchCountToAdvance) AdvanceTo(STEP_USE_ITEM);
            }
        }
    }

    // ---------- TutorialManager → TutorialController 對應表 ----------
    private void HandleInteraction(string kind, string name)
    {
        if (controller == null) return;
        int cur = controller.CurrentStep;

        // (current TC step, kind, name) -> next TC step
        // Phase A：用 TM 9 步事件推進；Phase B/C 會補上掉卡、3 個 target 等
        if (cur == STEP_MOVE && kind == "move")
        {
            AdvanceTo(STEP_PICK_BOX);
        }
        else if (cur == STEP_PICK_BOX && kind == "pick" && name == "peek")
        {
            AdvanceTo(STEP_SWITCH_ITEM);
        }
        else if (cur == STEP_USE_ITEM && kind == "use" && name == "peek")
        {
            AdvanceTo(STEP_ITEMS_MODAL);
        }
        else if (cur == STEP_PRACTICE && (kind == "use" && name == "banana"))
        {
            AdvanceTo(STEP_TOOLTIP);
        }
        else if (cur == STEP_PICK_WEAPON && kind == "pick" && name == "bat")
        {
            AdvanceTo(STEP_ATTACK);
        }
        else if (cur == STEP_ATTACK && kind == "use" && name == "bat")
        {
            // Phase A：暫且把「用武器」直接視為打倒假人 → 跳過卡片掉落直接到任務卡 modal
            AdvanceTo(STEP_MISSION_MODAL);
        }
        else if (cur == STEP_STEAL_FIRST && kind == "use" && name == "steal")
        {
            // Phase A：偷一次就視為完成（Phase C 會擴成 3 個）
            AdvanceTo(STEP_DONE);
        }
    }

    private void HandleTMStepChanged(int tmStep)
    {
        // 預留：如果之後想反向用 TM 的步數驅動 TC，可在這處理
    }

    private bool IsInfoStep(int step)
    {
        return step == STEP_ITEMS_MODAL
            || step == STEP_TOOLTIP
            || step == STEP_MISSION_MODAL
            || step == STEP_DONE;
    }

    private void AdvanceTo(int step)
    {
        if (controller == null) return;
        if (step < 0 || step >= controller.StepCount) return;
        controller.GoToStep(step);
        // 進到 STEP_SWITCH_ITEM 重置切換計數
        if (step == STEP_SWITCH_ITEM) _switchCount = 0;
        Debug.Log($"[TutorialFlow] -> step {step}");
    }
}
