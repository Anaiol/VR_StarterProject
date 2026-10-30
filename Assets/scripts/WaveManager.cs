using System.Collections.Generic;
using UnityEngine;

// Defineix les onades (waves) de la partida. El ScoreManager les va activant una rere l'altra
// i, en acabar l'última, mostra el panell final.
// Posa'l a [MANAGERS] de la GameScene i arrossega el generador de cubs.
public class WaveManager : MonoBehaviour
{
    [System.Serializable]
    public class Wave
    {
        public string name = "Wave";
        [Tooltip("Durada de la wave en segons")]
        public float duration = 30f;
        [Tooltip("Cubs per segon")]
        public float spawnsPerSecond = 0.3f;
        public bool gunCubes = true;
        public bool swordCubes = false;
        [Tooltip("Probabilitat (0-1) que surtin dos cubs alhora")]
        [Range(0f, 1f)] public float doubleSpawnChance = 0f;
        [Tooltip("Multiplicador de velocitat dels cubs (1 = velocitat del prefab)")]
        public float cubeSpeed = 1f;
        [Tooltip("Probabilitat (0-1) que el cub sigui especial (lent, ràpid o canvi d'armes)")]
        [Range(0f, 1f)] public float specialChance = 0f;
    }

    public blockGeneratorScript generator;

    public List<Wave> waves = new List<Wave>
    {
        // Wave 1: introducció, lenta i d'un sol tipus
        new Wave { name = "Wave 1 - Introducció", duration = 30f, spawnsPerSecond = 0.3f,
                   gunCubes = true, swordCubes = false, cubeSpeed = 0.8f },
        // Wave 2: els dos tipus, més ràpid i amb cubs dobles
        new Wave { name = "Wave 2 - Combinació", duration = 40f, spawnsPerSecond = 0.6f,
                   gunCubes = true, swordCubes = true, doubleSpawnChance = 0.25f, cubeSpeed = 1f },
        // Wave 3: més velocitat i cubs especials
        new Wave { name = "Wave 3 - Caos", duration = 45f, spawnsPerSecond = 0.9f,
                   gunCubes = true, swordCubes = true, doubleSpawnChance = 0.35f, cubeSpeed = 1.3f,
                   specialChance = 0.15f },
    };

    public int Count => waves.Count;
    public Wave Get(int index) => waves[Mathf.Clamp(index, 0, waves.Count - 1)];

    // Configura el generador amb els valors de la wave
    public void ApplyWave(int index)
    {
        Wave w = Get(index);
        if (generator == null) return;

        generator.SpawnsPerSecond = w.spawnsPerSecond;
        generator.SetCubeMix(w.gunCubes, w.swordCubes);
        generator.doubleSpawnChance = w.doubleSpawnChance;
        generator.speedMultiplier = w.cubeSpeed;
        generator.specialChance = w.specialChance;
        Debug.Log("[WaveManager] Comença " + w.name);
    }

    void Update()
    {
        GameSpeed.Tick();
    }
}
