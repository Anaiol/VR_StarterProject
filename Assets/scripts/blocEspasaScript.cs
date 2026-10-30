using UnityEngine;

public class blocEspasaScript : MonoBehaviour
{
    public Vector3 direction;
    public float speed;
    [Tooltip("Seguretat: si el cub no toca la CubeMissZone, es destrueix igualment al cap d'aquests segons")]
    public float lifetime;

    bool done; // evita comptar el mateix cub dues vegades (l'espasa té 2 colliders, i encert + fallada)

    public void Start()
    {
        Invoke(nameof(Missed), lifetime);
    }

    // El cub ha passat sense que el tallin (el crida la CubeMissZone o el lifetime)
    public void Missed()
    {
        if (done) return;
        done = true;
        CancelInvoke(nameof(Missed));
        if (ScoreManager.Instance != null) ScoreManager.Instance.AddMiss();
        Destroy(gameObject);
    }

    private void Update()
    {
        transform.position += direction * speed * GameSpeed.Multiplier * Time.deltaTime;
    }

    private void hitted()
    {
        CancelInvoke(nameof(Missed));
        // El ScoreManager suma els punts i fa sonar el clip
        if (ScoreManager.Instance != null) ScoreManager.Instance.CubeDestroyed(ScoreManager.CubeType.Sword);
        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Sword") && !done)
        {
            done = true;
            hapticsScript.VibrateFor(other.transform); // vibra la mà de l'espasa
            hitted(); // avisa el ScoreManager (punts + so)
        }
    }
}
