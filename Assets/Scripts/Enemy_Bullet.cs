using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    // Esta bala mata de un toque, así que no necesitamos una variable de daño.

    private void Start()
    {
        // Se destruye sola después de un tiempo (para no saturar)
        Destroy(gameObject, 8f);
    }

    // Se llama cuando la bala atraviesa un collider
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            if (other.gameObject.CompareTag("Player"))
            {
                // --- AÑADE ESTAS LÍNEAS DE TEST ---
                Debug.Log("¡CHOQUE CON JUGADOR DETECTADO!");

                if (GameManager.Instance == null)
                {
                    Debug.LogError("¡ERROR! GameManager.Instance es NULL.");
                }
                // ------------------------------------

                // --- TU CÓDIGO NUEVO ---
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.TriggerGameOver();
                }

                Destroy(gameObject); // Destruye la bala
                return;
            }


            // Si choca con cualquier cosa que NO sea el Jugador o Enemigos (Muros/Suelo)
            // (Esto es por si añadimos muros después)
            if (!other.gameObject.CompareTag("Enemy") && !other.gameObject.CompareTag("PlayerBullet"))
            {
                Destroy(gameObject);
            }
        }
    }
}