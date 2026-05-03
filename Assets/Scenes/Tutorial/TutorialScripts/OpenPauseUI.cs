using UnityEngine;

public class OpenPauseUI : MonoBehaviour
{
    public GameObject PauseUI;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            PauseUI.SetActive(!PauseUI.activeSelf);
        }
    }
}
