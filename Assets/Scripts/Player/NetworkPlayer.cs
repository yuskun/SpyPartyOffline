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
    public bool isPrepare = false;

    public override void Spawned()
    {
        Debug.Log($"[NetworkPlayer] 玩家 {PlayerId} 已生成。");
        characterController = GetComponent<OodlesCharacter>();

        if (PlayerId == Runner.LocalPlayer)
        {
            // 記錄本地玩家實例
            Local = this;

            CameraFollow.Get().player = characterController.GetPhysicsBody().transform;
            CameraFollow.Get().enable = true;

            if (isPrepare) return;
            if (MiniMap.instance != null)
                MiniMap.instance.target = characterController.GetPhysicsBody().transform;

            LocalBackpack.Instance.userInventory = this.GetComponent<PlayerInventory>();
            LocalBackpack.Instance.playerIdentify = this.GetComponent<PlayerIdentify>();
            LocalBackpack.Instance.character = this.GetComponent<OodlesCharacter>();
            CardPreviewSystem.Instance.previewAnchor = this.transform.Find("Ragdoll/SpawnObject");
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