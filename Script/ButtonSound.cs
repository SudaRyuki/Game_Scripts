using UnityEngine;

public class ButtonSound : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip clip;

    [Header("Pitch Random Range")]
    [Range(0.8f, 1.2f)]
    public float minPitch = 0.95f;

    [Range(0.8f, 1.2f)]
    public float maxPitch = 1.05f;

    public void PlaySound()
    {
        float randomPitch = Random.Range(minPitch, maxPitch);
        audioSource.pitch = randomPitch;

        audioSource.PlayOneShot(clip);

        // çƒê∂å„Ç…ñﬂÇ∑
        audioSource.pitch = 1f;
    }
}