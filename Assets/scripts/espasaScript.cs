using UnityEngine;

// Posa aquest script a l'arrel del prefab Sword.
// Fa sonar el so de tall quan l'espasa destrueix un cub d'espasa.
public class espasaScript : MonoBehaviour
{
    [Header("So del tall")]
    [Tooltip("Arrossega aquí l'àudio del tall (ex. VR-Game-Assets/Audio/SliceSound.wav)")]
    public AudioClip sliceClip;
    [Range(0f, 1f)] public float sliceVolume = 1f;

    AudioSource sliceSound;

    void Awake()
    {
        // AudioSource per al tall (es fa servir el de l'espasa o se'n crea un)
        sliceSound = GetComponent<AudioSource>();
        if (sliceSound == null) sliceSound = gameObject.AddComponent<AudioSource>();
        sliceSound.playOnAwake = false;
        sliceSound.spatialBlend = 1f; // so 3D
    }

    public void PlaySlice()
    {
        // PlayOneShot permet que talls seguits se superposin sense tallar-se
        if (sliceClip != null) sliceSound.PlayOneShot(sliceClip, sliceVolume);
        Debug.Log("Cub tallat");
    }
}
