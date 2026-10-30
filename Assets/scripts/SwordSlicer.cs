using UnityEngine;
using EzySlice;

// Talla els cubs d'espasa amb Ezy-Slice seguint el moviment de l'espasa.
// Posa'l a l'arrel del prefab Sword. El pla de tall conté la fulla i la direcció
// del moviment: si fas un tall horitzontal, el cub es parteix horitzontalment, etc.
public class SwordSlicer : MonoBehaviour
{
    [Tooltip("Fulla de l'espasa (si està buit, busca un fill anomenat 'Blade')")]
    public Transform blade;
    [Tooltip("Material de l'interior del tall (ex. VR-Game-Assets/Materials/CyberpunkCube_SliceFX.mat)")]
    public Material crossSectionMaterial;

    [Header("Trossos")]
    [Tooltip("Velocitat amb què se separen els dos trossos")]
    public float separationSpeed = 1.5f;
    [Tooltip("Segons abans d'esborrar els trossos")]
    public float pieceLifetime = 2f;
    [Tooltip("Velocitat mínima de la fulla per fer servir la seva direcció; per sota es talla en vertical")]
    public float minSwingSpeed = 0.3f;

    // Eix llarg de la fulla en coordenades locals de 'blade' (es calcula sol amb la malla)
    Vector3 bladeAxisLocal = Vector3.forward;
    float bladeHalfLength = 0.5f;
    Vector3 bladeCenterLocal;

    Vector3 lastCenter;
    Vector3 velocity;

    void Awake()
    {
        if (blade == null)
        {
            Transform t = transform.Find("Blade");
            blade = t != null ? t : transform;
        }

        // La dimensió més llarga de la malla és la direcció de la fulla
        MeshFilter mf = blade.GetComponentInChildren<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            Bounds b = mf.sharedMesh.bounds;
            Vector3 s = b.size;
            if (s.x >= s.y && s.x >= s.z) { bladeAxisLocal = Vector3.right; bladeHalfLength = s.x * 0.5f; }
            else if (s.y >= s.z)          { bladeAxisLocal = Vector3.up;    bladeHalfLength = s.y * 0.5f; }
            else                          { bladeAxisLocal = Vector3.forward; bladeHalfLength = s.z * 0.5f; }
            bladeCenterLocal = b.center;
        }
        lastCenter = BladeCenter;
    }

    public Vector3 BladeCenter => blade.TransformPoint(bladeCenterLocal);
    public Vector3 BladeDirection => blade.TransformDirection(bladeAxisLocal).normalized;
    public Vector3 Velocity => velocity;

    void Update()
    {
        // Velocitat de la fulla (suavitzada perquè el tracking del comandament fa petits salts)
        Vector3 c = BladeCenter;
        if (Time.deltaTime > 0f)
            velocity = Vector3.Lerp(velocity, (c - lastCenter) / Time.deltaTime, 0.5f);
        lastCenter = c;
    }

    // Talla 'target' en dos trossos. Retorna false si no s'ha pogut tallar.
    public bool Slice(GameObject target)
    {
        Vector3 bladeDir = BladeDirection;
        Vector3 swing = velocity;
        Vector3 center = target.GetComponent<Renderer>() != null ? target.GetComponent<Renderer>().bounds.center : target.transform.position;

        // Normal del pla = perpendicular a la fulla i al moviment
        Vector3 normal = Vector3.zero;
        if (swing.magnitude >= minSwingSpeed)
            normal = Vector3.Cross(bladeDir, swing);
        if (normal.sqrMagnitude < 0.0001f)
            normal = Vector3.Cross(bladeDir, Vector3.up); // sense moviment: tall vertical seguint la fulla
        if (normal.sqrMagnitude < 0.0001f)
            normal = transform.right;
        normal.Normalize();

        // Punt de la fulla més proper al centre del cub
        Vector3 bc = BladeCenter;
        float along = Mathf.Clamp(Vector3.Dot(center - bc, bladeDir), -bladeHalfLength, bladeHalfLength);
        Vector3 point = bc + bladeDir * along;

        GameObject[] pieces = target.SliceInstantiate(point, normal, crossSectionMaterial);
        // Si el pla no toca la malla (la fulla ha entrat de costat), tallem pel centre del cub
        if (pieces == null) pieces = target.SliceInstantiate(center, normal, crossSectionMaterial);
        if (pieces == null) return false;

        // Velocitat que portava el cub, perquè els trossos continuïn endavant
        Vector3 cubeVelocity = Vector3.zero;
        blocEspasaScript cube = target.GetComponent<blocEspasaScript>();
        if (cube != null) cubeVelocity = cube.direction.normalized * cube.speed * GameSpeed.Multiplier;

        for (int i = 0; i < pieces.Length; i++)
        {
            GameObject p = pieces[i];
            if (p == null) continue;
            p.layer = target.layer;

            MeshCollider col = p.AddComponent<MeshCollider>();
            col.convex = true;

            Rigidbody rb = p.AddComponent<Rigidbody>();
            float side = i == 0 ? 1f : -1f; // upper hull cap a la normal, lower hull al revés
            rb.linearVelocity = cubeVelocity * 0.5f + normal * side * separationSpeed + swing * 0.2f;
            rb.angularVelocity = Random.insideUnitSphere * 4f;

            Destroy(p, pieceLifetime);
        }
        return true;
    }
}
