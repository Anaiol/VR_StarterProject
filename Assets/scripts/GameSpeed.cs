using UnityEngine;

// Velocitat global del joc (cubs i generador). No toca Time.timeScale perquè
// alentiria també les mans i la física de l'AutoHand.
// Els cubs especials de càmera lenta / ràpida la canvien durant uns segons.
public static class GameSpeed
{
    public static float Multiplier { get; private set; } = 1f;

    static float effectEndTime;

    // Aplica un multiplicador temporal (ex. 0.5 = la meitat de ràpid durant 'duration' segons)
    public static void SetTemporary(float multiplier, float duration)
    {
        Multiplier = Mathf.Max(0.05f, multiplier);
        effectEndTime = Time.time + duration;
    }

    public static void Reset()
    {
        Multiplier = 1f;
        effectEndTime = 0f;
    }

    // El crida el WaveManager cada frame per tornar a 1 quan s'acaba l'efecte
    public static void Tick()
    {
        if (effectEndTime > 0f && Time.time >= effectEndTime) Reset();
    }

    public static float RemainingEffectTime => effectEndTime > 0f ? Mathf.Max(0f, effectEndTime - Time.time) : 0f;

    // Al carregar una escena nova, el valor estàtic podria quedar d'abans
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnLoad() => Reset();
}
