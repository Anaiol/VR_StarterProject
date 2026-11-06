using UnityEngine;

public static class GameSpeed
{
    public static float Multiplier { get; private set; } = 1f;

    static float effectEndTime;

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

    public static void Tick()
    {
        if (effectEndTime > 0f && Time.time >= effectEndTime) Reset();
    }

    public static float RemainingEffectTime => effectEndTime > 0f ? Mathf.Max(0f, effectEndTime - Time.time) : 0f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnLoad() => Reset();
}
