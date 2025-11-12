using UnityEngine;

/// <summary>
/// Hace que la luz siga al jugador en el plano XZ, manteniendo su altura Y fija.
/// Útil para iluminación dinámica centrada en el jugador.
/// </summary>
public class LightFollowPlayer : MonoBehaviour
{
    #region Constants
    private const string PLAYER_TAG = "Player";
    #endregion

    #region Serialized Fields
    [SerializeField, Tooltip("Transform del jugador a seguir")]
    private Transform playerTarget;
    #endregion

    #region Private Fields
    private float lightHeight;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        FindPlayerIfNeeded();
        CacheLightHeight();
    }

    private void LateUpdate()
    {
        FollowPlayer();
    }
    #endregion

    #region Initialization
    private void FindPlayerIfNeeded()
    {
        if (playerTarget != null) return;

        GameObject playerObject = GameObject.FindGameObjectWithTag(PLAYER_TAG);
        
        if (playerObject != null)
        {
            playerTarget = playerObject.transform;
        }
        else
        {
            Debug.LogWarning($"LightFollowPlayer: Player not found on {gameObject.name}", this);
        }
    }

    private void CacheLightHeight()
    {
        lightHeight = transform.position.y;
    }
    #endregion

    #region Following Logic
    private void FollowPlayer()
    {
        if (playerTarget == null) return;

        Vector3 newPosition = CalculateTargetPosition();
        transform.position = newPosition;
    }

    private Vector3 CalculateTargetPosition()
    {
        Vector3 targetPosition = playerTarget.position;
        targetPosition.y = lightHeight;
        return targetPosition;
    }
    #endregion
}