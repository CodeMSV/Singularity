using UnityEngine;
using System.Collections;

public class DamageFeedback : MonoBehaviour
{
    private MeshRenderer meshRenderer;
    private Material playerMaterial;
    
    private const string EmissionColorProperty = "_EmissionColor";
    private Color originalEmissionColor;
    
    // Componente de audio
    private AudioSource audioSource; 

    void Awake()
    {
        // --- Lógica de Inicialización de Materiales ---
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            playerMaterial = meshRenderer.material;
            
            if (playerMaterial.HasProperty(EmissionColorProperty))
            {
                originalEmissionColor = playerMaterial.GetColor(EmissionColorProperty);
            }
            else
            {
                originalEmissionColor = Color.white; 
            }
        }
        
        // --- SOLUCIÓN DE AUDIO - Opción 1: AudioSource en el mismo GameObject ---
        audioSource = GetComponent<AudioSource>();
        
        // --- Opción 2: Si el AudioSource está en un hijo ---
        if (audioSource == null)
        {
            audioSource = GetComponentInChildren<AudioSource>();
        }
        
        // --- Opción 3: Si NO existe, créalo automáticamente ---
        if (audioSource == null)
        {
            Debug.LogWarning("No se encontró AudioSource. Creando uno automáticamente...");
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D sound
        }
        
        if (audioSource == null)
        {
            Debug.LogError("FATAL ERROR: No se pudo inicializar el AudioSource.");
        }
    }

    public void Flash(float duration)
    {
        StopAllCoroutines(); 
        StartCoroutine(FlashRoutine(duration));
    }

    IEnumerator FlashRoutine(float duration)
    {
        if (playerMaterial == null) yield break;

        playerMaterial.SetColor(EmissionColorProperty, Color.white * 10f); 

        yield return new WaitForSeconds(duration);

        playerMaterial.SetColor(EmissionColorProperty, originalEmissionColor);
    }
    
    // Reproducir sonido
    // EN: DamageFeedback.cs

    // Esta es la que usan el Disparo y el Dash (con volumen fijo de 0.5)
    public void PlaySFX(AudioClip clip)
    {
        PlaySFX(clip, 0.5f); // Llama a la otra función con 0.5f
    }

    // ¡NUEVA FUNCIÓN! Esta la usará el enemigo (acepta volumen variable)
    public void PlaySFX(AudioClip clip, float customVolume)
    {
        if (audioSource != null && clip != null)
        {
            // (Tu código de diagnóstico sigue aquí...)
            Debug.Log($"Clip: {clip.name}");
            Debug.Log($"AudioSource Volume: {audioSource.volume}");
            
            // ¡CLAVE! Usamos el volumen personalizado
            audioSource.PlayOneShot(clip, customVolume); 
        }
        else
        {
            if (audioSource == null) Debug.LogError("AudioSource es NULL");
            if (clip == null) Debug.LogError("AudioClip es NULL");
        }
    }
} 
