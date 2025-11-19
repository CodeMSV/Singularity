using UnityEngine;
using System.Collections;

[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(AudioSource))]
public class RisingObstacle : MonoBehaviour
{
    [SerializeField] private float riseSpeed = 2f;
    [SerializeField] private AudioClip riseSFX;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;

    private AudioSource audioSource;
    private float stayDuration;
    private Vector3 surfacePosition;
    private Vector3 hiddenPosition;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.spatialBlend = 0f; 
        audioSource.playOnAwake = false;
    }

    public void Initialize(float duration, Vector3 spawnPoint, Vector3 size)
    {
        transform.localScale = size;
        stayDuration = duration;

        float pivotOffset = size.y / 2f;
        
        surfacePosition = new Vector3(spawnPoint.x, spawnPoint.y + pivotOffset, spawnPoint.z);
        
        hiddenPosition = new Vector3(spawnPoint.x, spawnPoint.y - pivotOffset - 0.5f, spawnPoint.z);

        transform.position = hiddenPosition;

        StartCoroutine(Sequence());
    }

    private IEnumerator Sequence()
    {
        PlaySound();
        yield return MoveTo(surfacePosition);

        yield return new WaitForSeconds(stayDuration);

        PlaySound();
        yield return MoveTo(hiddenPosition);

        Destroy(gameObject);
    }

    private IEnumerator MoveTo(Vector3 target)
    {
        while (Vector3.Distance(transform.position, target) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, riseSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = target;
    }

    private void PlaySound()
    {
        if (audioSource != null && riseSFX != null)
        {
            audioSource.PlayOneShot(riseSFX, sfxVolume);
        }
    }
}