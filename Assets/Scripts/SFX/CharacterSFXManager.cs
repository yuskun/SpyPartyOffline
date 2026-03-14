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

    public void PlayAttack() => PlayOneShot(attackClip);
    public void PlayBreak() => PlayOneShot(breakClip);
    public void PlayPunch() => PlayOneShot(punchClip);
    public void PlayJump() => PlayOneShot(jumpClip);
    public void PlayPickUp() => PlayOneShot(pickUpClip);
    public void PlayBanana() => PlayOneShot(BananaClip);
    public void PlayOpenUI() => PlayOneShot(OpenUIClip, 0.5f);
    public void PlayUseCard() => PlayOneShot(UseCardClip);

    

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
