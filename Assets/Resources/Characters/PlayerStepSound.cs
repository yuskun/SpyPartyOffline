using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlayerStepSound : MonoBehaviour
{
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        bool isMoving = Mathf.Abs(moveX) > 0.1f || Mathf.Abs(moveZ) > 0.1f;

        if (isMoving)
        {
            if (!audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }
        else
        {
            if (audioSource.isPlaying)
            {
                audioSource.Stop(); // 立刻切斷
            }
        }
    }
}
