using UnityEngine;

/// <summary>
/// Controla el comportamiento de los proyectiles disparados por el jugador.
/// Gestiona daño a enemigos y autodestrucción.
/// </summary>
public class Bullet : MonoBehaviour
{
    #region Serialized Fields
    [SerializeField, Min(0f)] 
    private float damage = 1f;
    #endregion

    #region Unity Callbacks
    private void OnTriggerEnter(Collider other)
    {
        if (TryDamageEnemy(other))
        {
            DestroyBullet();
        }
    }

    private void OnBecameInvisible()
    {
        DestroyBullet();
    }
    #endregion

    #region Private Methods
    private bool TryDamageEnemy(Collider collider)
    {
        Enemy enemy = collider.GetComponent<Enemy>();
        
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            return true;
        }
        
        return false;
    }

    private void DestroyBullet()
    {
        Destroy(gameObject);
    }
    #endregion
}