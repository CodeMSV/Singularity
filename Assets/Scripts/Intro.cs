using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class AutoSceneChanger : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(LoadSceneAfterDelay(1.0f));
    }

    private IEnumerator LoadSceneAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        SceneManager.LoadScene("InfoScene");
    }
}