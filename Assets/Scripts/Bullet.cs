using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField, Min(0f)]
    private float damage = 1f;

    private void OnTriggerEnter(Collider other)
    {
        if (TryDamageEnemy(other))
        {
            DestroyBullet();
            return;
        }

        if (other.CompareTag("Obstacle") || other.CompareTag("Wall") || other.gameObject.layer == LayerMask.NameToLayer("Default"))
        {
            if (!other.CompareTag("Player") && !other.CompareTag("Bullet") && !other.isTrigger)
            {
                DestroyBullet();
            }
            else if (!other.isTrigger)
            {
                DestroyBullet();
            }
        }
    }

    private void OnBecameInvisible()
    {
        DestroyBullet();
    }

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
}