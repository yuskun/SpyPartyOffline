
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Windows;

public class AudioManager : MonoBehaviour
{
    public AudioMixer mixer;
    public AudioSource sfxSource;
    public AudioSource BGMSource;
    public static AudioManager instance;
    public AudioClipDirectory audioDirectory;
    [Range(0.8f, 1.2f)]
    public float minPitch = 0.95f;
    [Range(0.8f, 1.2f)]
    public float maxPitch = 1.05f;
    void Awake()
    {

        if (instance == null)
        {
            instance = this;

            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
   
    public void SetBGMVolume(float volume)
    {
        mixer.SetFloat("BGMVol", Mathf.Log10(volume) * 20);
    }
    public void SetVFXVolume(float volume)
    {
        mixer.SetFloat("VFXVol", Mathf.Log10(volume) * 20);
    }
    public void PlayVFX(string key)
    {
        if (audioDirectory == null)
        {
            Debug.LogWarning("[AudioManager] audioDirectory 未指定");
            return;
        }

        AudioClip clip = audioDirectory.GetClip(key);
        if (clip == null) return;
        Debug.Log("[AudioManager] 播放音效: " + key);

        sfxSource.pitch = Random.Range(0.95f, 1.05f);
        sfxSource.PlayOneShot(clip);
    }

}
