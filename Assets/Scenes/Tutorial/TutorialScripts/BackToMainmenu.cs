using UnityEngine;
using UnityEngine.SceneManagement;

public class BackToMainmenu : MonoBehaviour
{
    public void GoToMainmenu()
    {
        SceneManager.LoadScene(0);
    }
}
