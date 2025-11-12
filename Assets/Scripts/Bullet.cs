using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float damage = 1f; // El daño que hace esta bala

    // Esta función se llama AUTOMÁTICAMENTE cuando "Is Trigger" está marcado
    // y atravesamos otro collider.
    private void OnTriggerEnter(Collider other)
    {
        // 1. Comprobar si el objeto que hemos atravesado tiene un script "Enemy"
        Enemy enemy = other.gameObject.GetComponent<Enemy>();

        // 2. Si SÍ es un enemigo...
        if (enemy != null)
        {
            // 3. ...le decimos que reciba daño
            enemy.TakeDamage(damage);

            // 4. Y destruimos la bala
            Destroy(gameObject);
        }
    }

    // --- ¡NUEVA FUNCIÓN! ---
    // Esta función se llama automáticamente cuando el Mesh Renderer
    // de la bala deja de ser visible por CUALQUIER cámara.
    private void OnBecameInvisible()
    {
        // Destruir la bala (evita que se acumulen infinitamente)
        Destroy(gameObject); 
    }
}