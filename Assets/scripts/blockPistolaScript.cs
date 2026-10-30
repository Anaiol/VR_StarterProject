using UnityEngine;

public class blockPistolaScript : MonoBehaviour
{
    public Vector3 direction;
    public float speed;
    public float lifetime;

    void Start()
    {
        Invoke(nameof(Missed), lifetime);
    }

    // El cubo ha pasado sin que le disparen
    private void Missed()
    {
        if (ScoreManager.Instance != null) ScoreManager.Instance.AddMiss();
        Destroy(gameObject);
    }

    private void Update()
    {
        transform.position += direction * speed * Time.deltaTime;
    }

    public void hitted()
    {
        CancelInvoke(nameof(Missed));
        // El ScoreManager suma els punts i fa sonar el clip
        if (ScoreManager.Instance != null) ScoreManager.Instance.CubeDestroyed(ScoreManager.CubeType.Gun);
        Destroy(gameObject);
    }
}