using UnityEngine;

/// <summary>
/// Proyectil disparado por enemigos. Mata al jugador al contacto y se autodestruye
/// después de un tiempo para evitar acumulación.
/// </summary>
public class EnemyBullet : MonoBehaviour
{
    #region Constants
    private const float AUTO_DESTROY_TIME = 8f;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        Destroy(gameObject, AUTO_DESTROY_TIME);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsPlayer(other))
        {
            HandlePlayerCollision();
        }
    }
    #endregion

    #region Collision Handling
    private bool IsPlayer(Collider collider)
    {
        return collider.CompareTag("Player");
    }

    private void HandlePlayerCollision()
    {
        TriggerGameOver();
        DestroyBullet();
    }

    private void TriggerGameOver()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerGameOver();
        }
        else
        {
            Debug.LogError("GameManager.Instance is null. Cannot trigger game over.", this);
        }
    }

    private void DestroyBullet()
    {
        Destroy(gameObject);
    }
    #endregion
}