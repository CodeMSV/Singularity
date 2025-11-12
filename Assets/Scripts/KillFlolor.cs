using UnityEngine;

public class KillFloor : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // 1. Si el JUGADOR toca el suelo, activa el Game Over
        if (other.CompareTag("Player"))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerGameOver();
            }
        }
        else
        {
            // 2. Si es CUALQUIER OTRA COSA (enemigos, balas, escombros),
            // simplemente destrúyelo para limpiar la escena.
            Destroy(other.gameObject);
        }
    }
}