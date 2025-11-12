using UnityEngine;

public class SelfDestruct : MonoBehaviour
{
    public float lifeTime = 3f; // Segundos que vivirá el escombro

    void Start()
    {
        // Le dice a Unity: "destrúyeme a mí mismo en 3 segundos"
        Destroy(gameObject, lifeTime);
    }
}