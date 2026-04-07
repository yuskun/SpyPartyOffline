
using UnityEngine;
using Fusion;
using OodlesEngine;
using UnityEngine.SocialPlatforms;
using Fusion.Addons.Physics;
using TMPro;


public class NetworkPlayer : NetworkBehaviour
{
    [Networked]
    public PlayerRef PlayerId { get; set; }

    
    public bool AllowInput = true;
    public float freezeTimer = 1f; // 凍結計時器，初始值為3秒
    private OodlesCharacter characterController;
    public bool isPrepare=false;
    private float loadingHideTimer = -1f;
    public override void Spawned()
    {
        Debug.Log($"[NetworkPlayer] 玩家 {PlayerId} 已生成。");
        characterController = GetComponent<OodlesCharacter>();
        if (PlayerId == Runner.LocalPlayer)
        {
            CameraFollow.Get().player = characterController.GetPhysicsBody().transform;
            CameraFollow.Get().enable = true;

            // 玩家生成完成 → 延遲 1 秒後關閉 Loading
            loadingHideTimer = 1f;

            if(isPrepare)return;
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
        // Loading 延遲關閉倒數（用 Runner.DeltaTime）
        if (loadingHideTimer > 0f)
        {
            loadingHideTimer -= Runner.DeltaTime;
            if (loadingHideTimer <= 0f)
            {
                loadingHideTimer = -1f;
                if (MenuUIManager.instance != null && MenuUIManager.instance.LoadingScreen != null)
                    MenuUIManager.instance.LoadingScreen.SetActive(false);
            }
        }

        if (!AllowInput)
            return;

        // 遊戲規則凍結（OK）
        if (freezeTimer > 0f)
        {
            freezeTimer -= Runner.DeltaTime;
            return;
        }

        //  只讓 Host 模擬
        if (!Object.HasStateAuthority)
            return;

        if (Runner.TryGetInputForPlayer(PlayerId, out OodlesCharacterInput data))
        {
            // // ====== 關鍵：丟掉過期輸入 ======
            // int currentTick = Runner.Tick;   // Fusion.Tick 可直接當 int 用
            // int inputTick = data.tick;

            // if (currentTick - inputTick > 2)
            // {
            //     // 過期輸入 → 直接忽略
            //     return;
            // }

            // ====== 只有新鮮輸入才進物理 ======
            characterController.ProcessInput(data);
        }

    }

}