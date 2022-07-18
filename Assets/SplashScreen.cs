using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SplashScreen : MonoBehaviour
{
    [SerializeField] private float loadTime = 1.5f;

    public void Start()
    {
        Invoke(nameof(LoadScene), loadTime);
    }

    private void LoadScene()
    {
        StartCoroutine(LoadAsynchronously(1));
    }

    private IEnumerator LoadAsynchronously(int sceneIndex)
    {
        Debug.Log("Start");
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneIndex);

        while (!operation.isDone)
        {
            Debug.Log(operation.progress);
            yield return null;
        }
    }
}
