using UnityEngine;

public class CubeExplode : MonoBehaviour
{
    [Tooltip("Model del cub trencat (GunCubeShatterAnimation.FBX)")]
    public GameObject shatterPrefab;
    [Tooltip("Opcional: material per als trossos (ex. Materials/Gun Cube/GunCube.mat)")]
    public Material shatterMaterial;
    [Tooltip("Multiplica l'escala del model trencat per si no quadra amb el cub sencer")]
    public float scaleMultiplier = 1f;
    [Tooltip("Segons que duren els trossos abans d'esborrar-se")]
    public float destroyAfter = 1.5f;

    [Header("Si el model no té animació")]
    public float explosionForce = 3f;

    bool exploded;

    public void Explode()
    {
        if (exploded) return;
        exploded = true;

        if (shatterPrefab != null)
        {
            GameObject shards = Instantiate(shatterPrefab, transform.position, transform.rotation);
            shards.transform.localScale = Vector3.Scale(shards.transform.localScale, transform.lossyScale) * scaleMultiplier;

            if (shatterMaterial != null)
            {
                foreach (Renderer r in shards.GetComponentsInChildren<Renderer>())
                {
                    Material[] mats = r.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++) mats[i] = shatterMaterial;
                    r.sharedMaterials = mats;
                }
            }

            Animation anim = shards.GetComponentInChildren<Animation>();
            if (anim != null && anim.clip != null)
            {
                anim.Play();
                Destroy(shards, Mathf.Max(destroyAfter, anim.clip.length));
            }
            else
            {
                PushPieces(shards.transform);
                Destroy(shards, destroyAfter);
            }
        }
        else
        {
            Debug.LogWarning("[CubeExplode] Falta assignar el Shatter Prefab a l'Inspector", this);
        }

        Destroy(gameObject);
    }

    void PushPieces(Transform root)
    {
        foreach (MeshRenderer piece in root.GetComponentsInChildren<MeshRenderer>())
        {
            Rigidbody rb = piece.gameObject.AddComponent<Rigidbody>();
            rb.AddExplosionForce(explosionForce, root.position, 1f, 0.2f, ForceMode.Impulse);
        }
    }
}
