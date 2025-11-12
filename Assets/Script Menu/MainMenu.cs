using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    // Esta función la llama el botón "Empezar"
    public void LoadInfoGameScene()
    {
        // Carga la Escena 3: "InfoGame"
        SceneManager.LoadScene("InfoGame");
    }
}