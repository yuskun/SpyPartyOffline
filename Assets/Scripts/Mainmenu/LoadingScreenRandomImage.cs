using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 掛在 LoadingScreen GameObject 上。
/// 每次 SetActive(true) 時，從 sprites 清單隨機挑一張換上去。
/// </summary>
public class LoadingScreenRandomImage : MonoBehaviour
{
    [SerializeField] private Image targetImage;
    [SerializeField] private Sprite[] sprites;

    private void OnEnable()
    {
        if (targetImage == null || sprites == null || sprites.Length == 0) return;
        targetImage.sprite = sprites[Random.Range(0, sprites.Length)];
    }
}
