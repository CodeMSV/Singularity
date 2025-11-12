using UnityEngine;

public class LightFollowPlayer : MonoBehaviour
{
    public Transform playerTarget;
    // Guardaremos la altura (Y) de la luz para que no cambie
    private float lightHeight; 

    void Start()
    {
        // Encontrar al jugador si no lo hemos asignado
        if (playerTarget == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                playerTarget = playerObject.transform;
            }
        }
        
        // Guardamos la altura inicial de la luz
        lightHeight = transform.position.y;
    }

    // Usamos LateUpdate para mover la luz DESPUÉS de que el jugador se mueva
    void LateUpdate()
    {
        if (playerTarget != null)
        {
            // 1. Tomamos la posición X y Z del jugador (el centro del plano)
            Vector3 targetPosition = playerTarget.position;
            
            // 2. Le forzamos a usar la altura Y original de la luz
            targetPosition.y = lightHeight;
            
            // 3. Aplicamos la posición
            transform.position = targetPosition;
        }
    }
}