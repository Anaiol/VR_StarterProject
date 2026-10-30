using UnityEngine;

// Cub especial (Wave 3): es pot destruir amb la pistola o amb l'espasa i activa un efecte.
// Va al costat d'un blockPistolaScript (els prefabs CubLent, CubRapid i CubCanvi són còpies de CubPistola).
[RequireComponent(typeof(blockPistolaScript))]
public class SpecialCube : MonoBehaviour
{
    public enum Effect { SlowMotion, FastForward, SwapWeapons }

    public Effect effect = Effect.SlowMotion;
    [Tooltip("Segons que dura l'efecte")]
    public float duration = 5f;
    [Tooltip("Multiplicador de velocitat per a SlowMotion / FastForward")]
    public float speedMultiplier = 0.5f;
    [Tooltip("Color del cub per distingir-lo dels normals")]
    public Color tint = Color.cyan;

    void Start()
    {
        // Pinta el cub sense crear materials nous (el shader CyberCube fa servir _HologramColor)
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            r.GetPropertyBlock(block);
            block.SetColor("_HologramColor", tint);
            block.SetColor("_Color", tint);
            block.SetColor("_BaseColor", tint);
            r.SetPropertyBlock(block);
        }
    }

    // L'espasa també el pot destruir
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Sword")) return;
        hapticsScript.VibrateFor(other.transform);
        GetComponent<blockPistolaScript>().hitted();
    }

    // El crida blockPistolaScript.hitted()
    public void Activate()
    {
        switch (effect)
        {
            case Effect.SlowMotion:
            case Effect.FastForward:
                GameSpeed.SetTemporary(speedMultiplier, duration);
                break;
            case Effect.SwapWeapons:
                if (WeaponSwapper.Instance != null) WeaponSwapper.Instance.SwapFor(duration);
                else Debug.LogWarning("[SpecialCube] No hi ha cap WeaponSwapper a l'escena");
                break;
        }
        Debug.Log($"[SpecialCube] Efecte {effect} durant {duration}s");
    }
}
