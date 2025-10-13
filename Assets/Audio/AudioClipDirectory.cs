using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Oodles/AudioClipDirectory", fileName = "AudioClipDirectory")]
public class AudioClipDirectory : ScriptableObject
{
    [System.Serializable]
    public class NamedClip
    {
        public string key;       // 對應名稱（如 "Footstep"、"Explosion"）
        public AudioClip clip;   // 對應音效
    }

    public List<NamedClip> clips = new List<NamedClip>();

    // 取得音效
    public AudioClip GetClip(string key)
    {
        foreach (var item in clips)
        {
            if (item.key == key)
                return item.clip;
        }
        Debug.LogWarning($"[AudioClipDirectory] 找不到音效：{key}");
        return null;
    }
}
