using UnityEngine;

public class blockGeneratorScript : MonoBehaviour
{
    [Header("Prefabs (uno por tipo de cubo)")]
    [SerializeField] private GameObject blockPrefabA;
    [SerializeField] private GameObject blockPrefabB;

    [Header("Ritmo de generación")]
    [Tooltip("Bloques por segundo")]
    [SerializeField] private float spawnsPerSecond = 2f;
    [Tooltip("Probabilidad (0-1) de que salga el prefab A; el resto será B")]
    [Range(0f, 1f)]
    [SerializeField] private float chanceA = 0.5f;
    [SerializeField] private bool spawnOnStart = true;

    [Header("Zona de aparición (grid frente al jugador)")]
    [Tooltip("Número de columnas (eje X)")]
    [SerializeField] private int columns = 4;
    [Tooltip("Número de filas (eje Y)")]
    [SerializeField] private int rows = 3;
    [SerializeField] private float columnSpacing = 0.5f;
    [SerializeField] private float rowSpacing = 0.5f;
    [Tooltip("Altura de la fila más baja respecto al generador")]
    [SerializeField] private float baseHeight = 0.8f;

    [Header("Rotación")]
    [Tooltip("Rotación aleatoria en Z (como los cortes de Beat Saber)")]
    [SerializeField] private bool randomZRotation = false;

    private float timer;
    private bool spawning;

    private void Start()
    {
        // No pisar un StartSpawning() que ya haya hecho el ScoreManager
        spawning = spawning || spawnOnStart;
    }

    private void Update()
    {
        if (!spawning || spawnsPerSecond <= 0f) return;

        timer += Time.deltaTime;
        float interval = 1f / spawnsPerSecond;

        while (timer >= interval)
        {
            timer -= interval;
            SpawnBlock();
        }
    }

    // Multiplicador de velocidad de los cubos (1 = la velocidad del prefab)
    [HideInInspector] public float speedMultiplier = 1f;

    public float SpawnsPerSecond
    {
        get => spawnsPerSecond;
        set => spawnsPerSecond = Mathf.Max(0f, value);
    }

    public void StartSpawning() { timer = 0f; spawning = true; }
    public void StopSpawning() => spawning = false;

    private void SpawnBlock()
    {
        GameObject prefab = Random.value <= chanceA ? blockPrefabA : blockPrefabB;
        if (prefab == null)
        {
            Debug.LogWarning("BlockGenerator: falta asignar un prefab en el Inspector.", this);
            return;
        }

        int col = Random.Range(0, Mathf.Max(1, columns));
        int row = Random.Range(0, Mathf.Max(1, rows));

        // Centrar el grid horizontalmente respecto al generador
        float x = (col - (columns - 1) * 0.5f) * columnSpacing;
        float y = baseHeight + row * rowSpacing;

        Vector3 localOffset = new Vector3(x, y, 0f);
        Vector3 spawnPos = transform.TransformPoint(localOffset);

        Quaternion rot = prefab.transform.rotation;

        if (randomZRotation)
            rot *= Quaternion.Euler(0f, 0f, 90f * Random.Range(0, 4));

        // Dirección y movimiento los gestiona el script del propio prefab
        GameObject block = Instantiate(prefab, spawnPos, rot);
        ApplySpeed(block);
    }

    // Aplica el multiplicador de velocidad. El lifetime se reduce igual para que
    // el cubo recorra la misma distancia y no pase de largo del jugador.
    // (Start() del cubo aún no se ha ejecutado, así que usa ya el lifetime nuevo)
    private void ApplySpeed(GameObject block)
    {
        if (Mathf.Approximately(speedMultiplier, 1f) || speedMultiplier <= 0f) return;

        blockPistolaScript gun = block.GetComponentInChildren<blockPistolaScript>();
        if (gun != null)
        {
            gun.speed *= speedMultiplier;
            gun.lifetime /= speedMultiplier;
        }

        blocEspasaScript sword = block.GetComponentInChildren<blocEspasaScript>();
        if (sword != null)
        {
            sword.speed *= speedMultiplier;
            sword.lifetime /= speedMultiplier;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        for (int c = 0; c < columns; c++)
        {
            for (int r = 0; r < rows; r++)
            {
                float x = (c - (columns - 1) * 0.5f) * columnSpacing;
                float y = baseHeight + r * rowSpacing;
                Gizmos.DrawWireCube(transform.TransformPoint(new Vector3(x, y, 0f)), Vector3.one * 0.25f);
            }
        }
    }
}