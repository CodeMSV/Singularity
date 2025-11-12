using UnityEngine;
using System.Collections;

public class FadeAndDie : MonoBehaviour
{
    [Header("Tiempos")]
    public float initialDelay = 0.5f; // Retraso antes de que empiece nada
    public float fadeCubeDuration = 1.0f; // Tiempo para que el cubo se vuelva invisible
    public float fadeGlowDuration = 1.0f; // Tiempo para que el brillo se apague (DESPUÉS)

    private Material fadeMaterial;
    private Color startBaseColor;
    private Color startEmissionColor;

    void Awake()
    {
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null) { enabled = false; return; }
        
        // Creamos una instancia única del material
        fadeMaterial = meshRenderer.material; 

        // Guardamos los colores iniciales
        startBaseColor = fadeMaterial.color; 
        if (fadeMaterial.IsKeywordEnabled("_EMISSION"))
        {
            startEmissionColor = fadeMaterial.GetColor("_EmissionColor");
        }
        else
        {
            startEmissionColor = Color.black;
        }
    }

    void Start()
    {
        StartCoroutine(FadeOutAndDestroy());
    }

    IEnumerator FadeOutAndDestroy()
    {
        // 1. Espera inicial
        yield return new WaitForSeconds(initialDelay);

        // --- ETAPA 1: Desvanecer el Cubo (el Color Base) ---
        float timer = 0f;
        while (timer < fadeCubeDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / fadeCubeDuration; // 0 a 1

            // Hacemos que la transparencia (alpha) del color base baje a 0
            Color currentBaseColor = startBaseColor;
            currentBaseColor.a = Mathf.Lerp(startBaseColor.a, 0f, progress);
            fadeMaterial.color = currentBaseColor;
            
            yield return null; // Espera un frame
        }

        // Asegurarse de que el color base está 100% transparente
        Color finalBaseColor = startBaseColor;
        finalBaseColor.a = 0f;
        fadeMaterial.color = finalBaseColor;

        // --- ETAPA 2: Desvanecer el Brillo (la Emisión) ---
        timer = 0f; // Reiniciamos el temporizador
        while (timer < fadeGlowDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / fadeGlowDuration; // 0 a 1

            // Hacemos que el color de emisión se vuelva negro (se apague)
            Color currentEmissionColor = Color.Lerp(startEmissionColor, Color.black, progress);
            fadeMaterial.SetColor("_EmissionColor", currentEmissionColor);

            yield return null; // Espera un frame
        }

        // Asegurarse de que el brillo está 100% apagado
        fadeMaterial.SetColor("_EmissionColor", Color.black);

        Destroy(gameObject); // Destruir el mini-cubo
    }
}