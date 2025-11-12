using UnityEngine;
using UnityEngine.SceneManagement;

public class InfoGameController : MonoBehaviour
{
    public void LoadGameScene()
    {
        SceneManager.LoadScene("Juego");
    }
}