using UnityEngine;

public class CharacterSFXManager : MonoBehaviour
{
    public static CharacterSFXManager Instance;

    // --- 新增枚舉定義，這會對應 NetworkPlayer RPC 傳進來的類型 ---
    public enum SFXType 
    { 
        Attack, Break, Punch, Jump, PickUp, Banana, OpenUI, UseCard 
    }

    [Header("角色音效檔案")]
    public AudioSource sfxSource;
    public AudioClip attackClip;
    public AudioClip breakClip;
    public AudioClip punchClip;
    public AudioClip jumpClip;
    public AudioClip pickUpClip;
    public AudioClip BananaClip;
    public AudioClip OpenUIClip;
    public AudioClip UseCardClip;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
                sfxSource.loop = false;
                sfxSource.spatialBlend = 0f; // 2D 音效
                sfxSource.volume = 1.0f;     // 基礎音量設為 1，在 PlayOneShot 再細調
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // --- 核心播放方法：由 NetworkPlayer 的 RPC 呼叫 ---
    public void PlaySFX(SFXType type)
    {
        switch (type)
        {
            case SFXType.Attack: PlayAttack(); break;
            case SFXType.Break:  PlayBreak();  break;
            case SFXType.Punch:  PlayPunch();  break;
            case SFXType.Jump:   PlayJump();   break;
            case SFXType.PickUp: PlayPickUp(); break;
            case SFXType.Banana: PlayBanana(); break;
            case SFXType.OpenUI: PlayOpenUI(); break;
            case SFXType.UseCard: PlayUseCard(); break;
        }
    }

    // --- 個別音效的細節設定 ---
    public void PlayAttack() => PlayOneShot(attackClip, 0.1f);
    public void PlayBreak()  => PlayOneShot(breakClip, 0.1f);
    public void PlayPunch()  => PlayOneShot(punchClip, 0.1f);
    public void PlayJump()   => PlayOneShot(jumpClip, 0.05f);
    public void PlayPickUp() => PlayOneShot(pickUpClip, 1f);
    public void PlayBanana() => PlayOneShot(BananaClip, 0.1f);
    public void PlayOpenUI() => PlayOneShot(OpenUIClip, 0.05f);
    public void PlayUseCard() => PlayOneShot(UseCardClip, 0.8f);

    // --- 底層播放邏輯 ---
    private void PlayOneShot(AudioClip clip, float volume = 1f)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip, volume);
        }
    }
}