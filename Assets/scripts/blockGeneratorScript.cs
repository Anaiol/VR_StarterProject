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

        timer += Time.deltaTime * GameSpeed.Multiplier;
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

    [Header("Waves (els controla el WaveManager)")]
    [Tooltip("Probabilitat (0-1) que surtin dos cubs alhora")]
    [Range(0f, 1f)] public float doubleSpawnChance = 0f;
    [Tooltip("Cubs especials: CubLent, CubRapid, CubCanvi")]
    public GameObject[] specialPrefabs;
    [Tooltip("Probabilitat (0-1) que un cub sigui especial")]
    [Range(0f, 1f)] public float specialChance = 0f;

    public void SetCubeMix(bool gun, bool sword)
    {
        bool aIsGun = blockPrefabA != null && blockPrefabA.GetComponentInChildren<blockPistolaScript>() != null;
        if (gun && sword) chanceA = 0.5f;
        else if (gun) chanceA = aIsGun ? 1f : 0f;
        else if (sword) chanceA = aIsGun ? 0f : 1f;
    }

    private void SpawnBlock()
    {
        int cells = Mathf.Max(1, columns) * Mathf.Max(1, rows);
        int first = Random.Range(0, cells);
        SpawnAt(first);

        if (cells > 1 && Random.value < doubleSpawnChance)
        {
            int second = (first + Random.Range(1, cells)) % cells;
            SpawnAt(second);
        }
    }

    private GameObject PickPrefab()
    {
        if (specialPrefabs != null && specialPrefabs.Length > 0 && Random.value < specialChance)
        {
            GameObject special = NextSpecial();
            if (special != null) return special;
        }
        return Random.value < chanceA ? blockPrefabA : blockPrefabB;
    }

    private readonly System.Collections.Generic.List<GameObject> specialBag = new System.Collections.Generic.List<GameObject>();
    private GameObject lastSpecial;

    private GameObject NextSpecial()
    {
        if (specialBag.Count == 0) RefillSpecialBag();
        if (specialBag.Count == 0) return null;

        GameObject next = specialBag[0];
        specialBag.RemoveAt(0);
        lastSpecial = next;
        return next;
    }

    private void RefillSpecialBag()
    {
        foreach (GameObject s in specialPrefabs)
            if (s != null) specialBag.Add(s);

        for (int i = specialBag.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (specialBag[i], specialBag[j]) = (specialBag[j], specialBag[i]);
        }

        if (specialBag.Count > 1 && specialBag[0] == lastSpecial)
        {
            int k = Random.Range(1, specialBag.Count);
            (specialBag[0], specialBag[k]) = (specialBag[k], specialBag[0]);
        }
    }

    private void SpawnAt(int cell)
    {
        GameObject prefab = PickPrefab();
        if (prefab == null)
        {
            Debug.LogWarning("BlockGenerator: falta asignar un prefab en el Inspector.", this);
            return;
        }

        int col = cell % Mathf.Max(1, columns);
        int row = cell / Mathf.Max(1, columns);

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