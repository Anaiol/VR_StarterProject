using UnityEngine;

public class blockPistolaScript : MonoBehaviour
{
    public Vector3 direction;
    public float speed;
    [Tooltip("Seguretat: si el cub no toca la CubeMissZone, es destrueix igualment al cap d'aquests segons")]
    public float lifetime;

    bool done;

    void Start()
    {
        Invoke(nameof(Missed), lifetime);
    }

    public void Missed()
    {
        if (done) return;
        done = true;
        CancelInvoke(nameof(Missed));
        if (ScoreManager.Instance != null && GetComponent<SpecialCube>() == null) ScoreManager.Instance.AddMiss();
        Destroy(gameObject);
    }

    private void Update()
    {
        transform.position += direction * speed * GameSpeed.Multiplier * Time.deltaTime;
    }

    public void hitted()
    {
        if (done) return;
        done = true;
        CancelInvoke(nameof(Missed));
        // El ScoreManager suma els punts i fa sonar el clip
        if (ScoreManager.Instance != null) ScoreManager.Instance.CubeDestroyed(ScoreManager.CubeType.Gun);

        SpecialCube special = GetComponent<SpecialCube>();
        if (special != null) special.Activate();

        CubeExplode explode = GetComponent<CubeExplode>();
        if (explode != null) explode.Explode();
        else Destroy(gameObject);
    }
}
