using UnityEngine;

public class blocEspasaScript : MonoBehaviour
{
public Vector3 direction;
    public float speed;
    public float lifetime;
    private bool isHit;


    public void Start()
    {
        Invoke(nameof(Missed), lifetime);
    }

    // El cubo ha pasado sin que lo corten
    private void Missed()
    {
        if (ScoreManager.Instance != null) ScoreManager.Instance.AddMiss();
        Destroy(gameObject);
    }

    private void Update()
    {
        transform.position += direction * speed * Time.deltaTime;
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
        if (other.CompareTag("Sword") && !isHit)
        {
            isHit = true; // la espada tiene 2 colliders: evita vibrar dos veces
            hapticsScript.VibrateFor(other.transform); // vibra la mano de la espada
            hitted(); // avisa el ScoreManager (punts + so)
        }
    }
}