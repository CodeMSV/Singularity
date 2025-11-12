using UnityEngine;
using System.Collections;

/// <summary>
/// Desvanece progresivamente el objeto (base + emisión) y luego lo destruye.
/// Útil para efectos de partículas o mini-cubos de explosión.
/// </summary>
public class FadeAndDie : MonoBehaviour
{
    #region Constants
    private const string EMISSION_COLOR_PROPERTY = "_EmissionColor";
    private const string EMISSION_KEYWORD = "_EMISSION";
    private const float FULLY_TRANSPARENT = 0f;
    #endregion

    #region Serialized Fields
    [Header("Timing")]
    [SerializeField, Min(0f), Tooltip("Retraso antes de iniciar el fade")]
    private float initialDelay = 0.5f;
    
    [SerializeField, Min(0f), Tooltip("Duración del fade del color base")]
    private float fadeCubeDuration = 1f;
    
    [SerializeField, Min(0f), Tooltip("Duración del fade de la emisión")]
    private float fadeGlowDuration = 1f;
    #endregion

    #region Private Fields
    private Material fadeMaterial;
    private Color startBaseColor;
    private Color startEmissionColor;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if (!InitializeMaterial())
        {
            enabled = false;
        }
    }

    private void Start()
    {
        StartCoroutine(FadeOutSequence());
    }
    #endregion

    #region Initialization
    private bool InitializeMaterial()
    {
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        
        if (meshRenderer == null)
        {
            Debug.LogWarning($"FadeAndDie: No MeshRenderer on {gameObject.name}", this);
            return false;
        }

        fadeMaterial = meshRenderer.material;
        CacheInitialColors();
        
        return true;
    }

    private void CacheInitialColors()
    {
        startBaseColor = fadeMaterial.color;
        startEmissionColor = GetInitialEmissionColor();
    }

    private Color GetInitialEmissionColor()
    {
        if (fadeMaterial.IsKeywordEnabled(EMISSION_KEYWORD))
        {
            return fadeMaterial.GetColor(EMISSION_COLOR_PROPERTY);
        }
        
        return Color.black;
    }
    #endregion

    #region Fade Sequence
    private IEnumerator FadeOutSequence()
    {
        yield return new WaitForSeconds(initialDelay);
        
        yield return StartCoroutine(FadeBaseColor());
        yield return StartCoroutine(FadeEmission());
        
        Destroy(gameObject);
    }

    private IEnumerator FadeBaseColor()
    {
        float elapsed = 0f;

        while (elapsed < fadeCubeDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / fadeCubeDuration;
            
            UpdateBaseColorAlpha(progress);
            
            yield return null;
        }

        SetBaseColorFullyTransparent();
    }

    private IEnumerator FadeEmission()
    {
        float elapsed = 0f;

        while (elapsed < fadeGlowDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / fadeGlowDuration;
            
            UpdateEmissionColor(progress);
            
            yield return null;
        }

        SetEmissionFullyOff();
    }
    #endregion

    #region Color Updates
    private void UpdateBaseColorAlpha(float progress)
    {
        Color currentColor = startBaseColor;
        currentColor.a = Mathf.Lerp(startBaseColor.a, FULLY_TRANSPARENT, progress);
        fadeMaterial.color = currentColor;
    }

    private void SetBaseColorFullyTransparent()
    {
        Color finalColor = startBaseColor;
        finalColor.a = FULLY_TRANSPARENT;
        fadeMaterial.color = finalColor;
    }

    private void UpdateEmissionColor(float progress)
    {
        Color currentEmission = Color.Lerp(startEmissionColor, Color.black, progress);
        fadeMaterial.SetColor(EMISSION_COLOR_PROPERTY, currentEmission);
    }

    private void SetEmissionFullyOff()
    {
        fadeMaterial.SetColor(EMISSION_COLOR_PROPERTY, Color.black);
    }
    #endregion
}