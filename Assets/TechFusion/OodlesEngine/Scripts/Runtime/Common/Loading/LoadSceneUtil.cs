using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OodlesEngine
{
    public class LoadSceneUtil : MonoBehaviour
    {
        public string SceneName;
        // Start is called before the first frame update
        public void LoadScene()
        {
            LoadSceneHelper.loadingScene = SceneName;
            SceneManager.LoadScene("Loading");
        }
    }
}