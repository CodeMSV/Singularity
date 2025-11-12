using UnityEngine;
using System.Collections;

/// <summary>
/// Gestiona feedback visual (flash) y sonoro cuando el jugador recibe daño.
/// </summary>
public class DamageFeedback : MonoBehaviour
{
    #region Constants
    private const string EMISSION_COLOR_PROPERTY = "_EmissionColor";
    private const float DEFAULT_SFX_VOLUME = 0.5f;
    private const float EMISSION_INTENSITY = 10f;
    private const float SPATIAL_BLEND_2D = 0f;
    #endregion

    #region Private Fields
    private MeshRenderer meshRenderer;
    private Material playerMaterial;
    private Color originalEmissionColor;
    private AudioSource audioSource;
    private Coroutine currentFlashCoroutine;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        InitializeMaterial();
        InitializeAudioSource();
    }
    #endregion

    #region Initialization
    private void InitializeMaterial()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        
        if (meshRenderer == null)
        {
            Debug.LogWarning($"No MeshRenderer found on {gameObject.name}. Visual feedback disabled.", this);
            return;
        }

        playerMaterial = meshRenderer.material;
        
        originalEmissionColor = playerMaterial.HasProperty(EMISSION_COLOR_PROPERTY)
            ? playerMaterial.GetColor(EMISSION_COLOR_PROPERTY)
            : Color.white;
    }

    private void InitializeAudioSource()
    {
        audioSource = GetComponent<AudioSource>() ?? GetComponentInChildren<AudioSource>();

        if (audioSource == null)
        {
            audioSource = CreateAudioSource();
        }
    }

    private AudioSource CreateAudioSource()
    {
        AudioSource newSource = gameObject.AddComponent<AudioSource>();
        newSource.playOnAwake = false;
        newSource.spatialBlend = SPATIAL_BLEND_2D;
        
        Debug.LogWarning($"AudioSource created automatically on {gameObject.name}", this);
        
        return newSource;
    }
    #endregion

    #region Public Methods - Visual Feedback
    public void Flash(float duration)
    {
        if (playerMaterial == null) return;

        if (currentFlashCoroutine != null)
        {
            StopCoroutine(currentFlashCoroutine);
        }

        currentFlashCoroutine = StartCoroutine(FlashRoutine(duration));
    }
    #endregion

    #region Public Methods - Audio Feedback
    public void PlaySFX(AudioClip clip)
    {
        PlaySFX(clip, DEFAULT_SFX_VOLUME);
    }

    public void PlaySFX(AudioClip clip, float volume)
    {
        if (!ValidateAudioPlayback(clip)) return;

        audioSource.PlayOneShot(clip, volume);
    }
    #endregion

    #region Private Methods
    private IEnumerator FlashRoutine(float duration)
    {
        playerMaterial.SetColor(EMISSION_COLOR_PROPERTY, Color.white * EMISSION_INTENSITY);
        
        yield return new WaitForSeconds(duration);
        
        playerMaterial.SetColor(EMISSION_COLOR_PROPERTY, originalEmissionColor);
        
        currentFlashCoroutine = null;
    }

    private bool ValidateAudioPlayback(AudioClip clip)
    {
        if (audioSource == null)
        {
            Debug.LogError($"AudioSource is null on {gameObject.name}", this);
            return false;
        }

        if (clip == null)
        {
            Debug.LogError($"AudioClip is null when trying to play on {gameObject.name}", this);
            return false;
        }

        return true;
    }
    #endregion
}