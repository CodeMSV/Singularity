using UnityEngine;
using UnityEngine.SceneManagement;

public class InfoGameController : MonoBehaviour
{
    // Esta función la llama el botón "Jugar"
    public void LoadGameScene()
    {
        // Carga la Escena 4: "Juego"
        SceneManager.LoadScene("Juego");
    }
}