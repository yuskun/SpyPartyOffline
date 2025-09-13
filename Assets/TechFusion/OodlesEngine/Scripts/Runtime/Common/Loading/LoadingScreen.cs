using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace OodlesEngine
{
    public class LoadingScreen : MonoBehaviour
    {
        private float progress;

        [Header("References")]
        [SerializeField] private GameObject loadingContent;

        [SerializeField] private Transform movingLoadingThing;

        [SerializeField] private Image loadingFillImage;

        [SerializeField] private Text loadingText;

        [Header("Loading knob positions")]
        [SerializeField] private Transform startPoint;

        [SerializeField] private Transform endPoint;

        private void Start()
        {
            LoadScene(LoadSceneHelper.loadingScene);
        }

        public void LoadScene(string sceneName)
        {
            StartCoroutine(LoadAsynchronously(sceneName));
        }

        IEnumerator LoadAsynchronously(string sceneName)
        {
            AsyncOperation _opearation = SceneManager.LoadSceneAsync(sceneName);

            loadingContent.SetActive(true);

            while (!_opearation.isDone)
            {
                float _progress = Mathf.Clamp01(_opearation.progress / 0.9f);
                float _prosentProgress = _progress * 100f;
                loadingFillImage.fillAmount = _progress;
                loadingText.text = _prosentProgress.ToString("F0") + "%";
                movingLoadingThing.localPosition = new Vector3(Mathf.Lerp(startPoint.localPosition.x, endPoint.localPosition.x, _progress), 0f, 0f);

                progress = _prosentProgress;

                yield return null;
            }

            //SceneManager.UnloadSceneAsync("Loading");
        }

        public float Progress()
        {
            return progress;
        }
    }
}