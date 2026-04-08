using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class UISFXManager : MonoBehaviour
{
    public static UISFXManager Instance;

    public AudioClip buttonClick;
    private AudioSource audioSource;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            CheckUIButtonClick();
        }
    }

    void CheckUIButtonClick()
    {
        if (EventSystem.current == null)
            return;

        PointerEventData pointerData = new PointerEventData(EventSystem.current);
        pointerData.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (RaycastResult result in results)
        {
            Button btn = result.gameObject.GetComponent<Button>();

            if (btn != null && btn.interactable)
            {
                PlayButtonSound();
                break;
            }
        }
    }

    public void PlayButtonSound()
    {
        if (buttonClick != null)
        {
            audioSource.PlayOneShot(buttonClick);
        }
    }
    
}
