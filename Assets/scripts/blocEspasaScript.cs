using UnityEngine;

public class blocEspasaScript : MonoBehaviour
{
    public Vector3 direction;
    public float speed;
    [Tooltip("Seguretat: si el cub no toca la CubeMissZone, es destrueix igualment al cap d'aquests segons")]
    public float lifetime;

    bool done;

    public void Start()
    {
        Invoke(nameof(Missed), lifetime);
    }

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
            hapticsScript.VibrateFor(other.transform);

            SwordSlicer slicer = other.GetComponentInParent<SwordSlicer>();
            if (slicer != null) slicer.Slice(gameObject);

            hitted();
        }
    }
}
