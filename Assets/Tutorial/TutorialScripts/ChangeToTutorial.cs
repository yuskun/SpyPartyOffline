using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeToTutorial : MonoBehaviour
{
    public void GoToTutorial()
    {
        SceneManager.LoadScene("Tutorial");
    }
}
