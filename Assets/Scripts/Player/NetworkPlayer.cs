using UnityEngine;
using Fusion;
using OodlesEngine;
using Fusion.Addons.Physics;

public class NetworkPlayer : NetworkBehaviour
{
    // 新增：方便外部存取本地玩家的連線物件
    public static NetworkPlayer Local;

    [Networked]
    public PlayerRef PlayerId { get; set; }

    public bool AllowInput = true;
    public float freezeTimer = 1f;
    private OodlesCharacter characterController;

    // Networked：讓 Client 端也能讀到正確值。
    // PlayerSpawner.SpawnPlayer 會在 Spawn callback 裡設定此欄位，
    // 由於 Fusion 會把此 [Networked] 的初始值隨 Spawn 一併同步，
    // 當 Client 的 Spawned() 被呼叫時此值已經是正確的。
    [Networked]
    public NetworkBool isPrepare { get; set; }

    public override void Spawned()
    {
        Debug.Log($"[NetworkPlayer] 玩家 {PlayerId} 已生成。");
        characterController = GetComponent<OodlesCharacter>();

        if (PlayerId == Runner.LocalPlayer)
        {
            // 記錄本地玩家實例
            Local = this;

            CameraFollow.Get().player = characterController.GetPhysicsBody().transform;

            // PrepareRoom 階段：由 SkinChange.PickCharacterUI 全權控制相機
            // （它會把 enable 設成 false 並跑 MoveCamera 協程）。
            // 此處不再碰 enable，避免 Spawned 時序跑在 Rpc_PlayerSpawnComplete 之後
            // 把 CameraFollow 重新打開，導致 Client 端 MoveCamera 的結果被覆蓋。
            if (isPrepare) return;

            CameraFollow.Get().enable = true;

            if (MiniMap.instance != null)
                MiniMap.instance.target = characterController.GetPhysicsBody().transform;

            LocalBackpack.Instance.userInventory = this.GetComponent<PlayerInventory>();
            LocalBackpack.Instance.playerIdentify = this.GetComponent<PlayerIdentify>();
            LocalBackpack.Instance.character = this.GetComponent<OodlesCharacter>();
            LocalBackpack.Instance.networkPlayer = this.GetComponent<NetworkPlayer>();
            // 預覽錨點改掛在 MainCamera 底下的 "SpawnObject"
            if (Camera.main != null)
            {
                var anchor = Camera.main.transform.Find("SpawnObject");
                if (anchor != null)
                    CardPreviewSystem.Instance.previewAnchor = anchor;
                else
                    Debug.LogWarning("[NetworkPlayer] MainCamera 底下找不到 SpawnObject，PreviewAnchor 未設定");
            }
            LocalBackpack.Instance.scanner = this.transform.Find("Ragdoll").GetComponent<PlayerScanner>();
            LocalBackpack.Instance.scanner.enableScan = true;
            this.GetComponent<PlayerIdentify>().Text.gameObject.SetActive(false);
        }
    }

    // =============================
    // 音效同步系統 (RPC)
    // =============================

    /// <summary>
    /// 最核心的 RPC 方法：讓所有端呼叫本地的 CharacterSFXManager 播放聲音
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_PlayGlobalSFX(CharacterSFXManager.SFXType type, PlayerRef targetPlayer)
    {
        if (Runner.LocalPlayer != targetPlayer) return;
        if (CharacterSFXManager.Instance != null)
        {
            CharacterSFXManager.Instance.PlaySFX(type);
        }
    }

    // 快捷播放方法：方便你從 Animator 腳本或卡片腳本呼叫
    //public void PlayPunchSound() => RPC_PlayGlobalSFX(CharacterSFXManager.SFXType.Punch);
    //public void PlayAttackSound() => RPC_PlayGlobalSFX(CharacterSFXManager.SFXType.Attack);
    //public void PlayUseCardSound() => RPC_PlayGlobalSFX(CharacterSFXManager.SFXType.UseCard);

    // =============================
    // 原有邏輯保持不變
    // =============================

    public void TeleportTo(Vector3 position)
    {
        var body = this.gameObject.transform.Find("Ragdoll").GetComponent<NetworkRigidbody3D>();
        if (body != null)
        {
            Debug.Log("Tp");
            body.Teleport(position, Quaternion.identity);
            freezeTimer = 5;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!AllowInput) return;

        if (freezeTimer > 0f)
        {
            freezeTimer -= Runner.DeltaTime;
            return;
        }

        if (!Object.HasStateAuthority) return;

        if (Runner.TryGetInputForPlayer(PlayerId, out OodlesCharacterInput data))
        {
            characterController.ProcessInput(data);
        }
    }
}