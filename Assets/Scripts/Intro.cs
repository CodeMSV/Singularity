using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections; // <-- ¡Importante! Necesario para las Corutinas

public class AutoSceneChanger : MonoBehaviour // Puedes cambiar el nombre de la clase
{
    // Esta función se llama automáticamente una vez 
    // en el primer frame que este script está activo.
    void Start()
    {
        // Inicia la corutina que esperará 1 segundo.
        StartCoroutine(LoadSceneAfterDelay(1.0f));
    }

    // Esta es la Corutina
    private IEnumerator LoadSceneAfterDelay(float delay)
    {
        // Espera por el número de segundos especificado
        yield return new WaitForSeconds(delay);

        // Después de esperar, carga la escena
        SceneManager.LoadScene("InfoScene");
    }
}