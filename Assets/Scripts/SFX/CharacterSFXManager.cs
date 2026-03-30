using UnityEngine;

public class CharacterSFXManager : MonoBehaviour
{
    public static CharacterSFXManager Instance;

    [Header("角色音效")]
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
                sfxSource.volume = 0.03f;   
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayAttack() => PlayOneShot(attackClip, 0.1f);
    public void PlayBreak() => PlayOneShot(breakClip, 0.1f);
    public void PlayPunch() => PlayOneShot(punchClip, 0.1f);
    public void PlayJump() => PlayOneShot(jumpClip);
    public void PlayPickUp() => PlayOneShot(pickUpClip);
    public void PlayBanana() => PlayOneShot(BananaClip, 0.1f);
    public void PlayOpenUI() => PlayOneShot(OpenUIClip);
    public void PlayUseCard() => PlayOneShot(UseCardClip, 0.8f);

    

    private void PlayOneShot(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
            sfxSource.PlayOneShot(clip);
    }
    private void PlayOneShot(AudioClip clip, float v)
    {
        if (clip != null && sfxSource != null)
            sfxSource.PlayOneShot(clip, v);
    }
}
