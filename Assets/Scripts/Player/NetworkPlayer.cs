
using UnityEngine;
using Fusion;
using OodlesEngine;
using UnityEngine.SocialPlatforms;
using Fusion.Addons.Physics;


public class NetworkPlayer : NetworkBehaviour
{
    [Networked]
    public PlayerRef PlayerId { get; set; }
    public bool AllowInput=true;
    public float freezeTimer = 1f; // 凍結計時器，初始值為3秒
    private OodlesCharacter characterController;
    public override void Spawned()
    {
        Debug.Log($"[NetworkPlayer] 玩家 {PlayerId} 已生成。");
        characterController = GetComponent<OodlesCharacter>();
        if (PlayerId == Runner.LocalPlayer)
        {
            CameraFollow.Get().player = characterController.GetPhysicsBody().transform;
            CameraFollow.Get().enable = true;
            MiniMap.instance.target = characterController.GetPhysicsBody().transform;
            LocalBackpack.Instance.userInventory = this.GetComponent<PlayerInventory>();
            LocalBackpack.Instance.playerIdentify = this.GetComponent<PlayerIdentify>();
            LocalBackpack.Instance.scanner = this.transform.Find("Ragdoll").GetComponent<PlayerScanner>();
            LocalBackpack.Instance.scanner.enableScan = true;


        }

    }

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

        // Runner.DeltaTime 是每個網路 Tick 的時間
        if (freezeTimer > 0f)
        {
            freezeTimer -= Runner.DeltaTime;
            return; // 還在凍結 → 不處理輸入
        }

        if (Object.HasStateAuthority) // Host 負責模擬
        {
            if (Runner.TryGetInputForPlayer(PlayerId, out OodlesCharacterInput data))
            {
                characterController.ProcessInput(data);

            }

        }

    }

}