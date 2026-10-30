using UnityEngine;
using UnityEngine.XR;
using Autohand;

// Vibración de los mandos al destruir cubos.
// Ponlo en la Pistola y en la Espada para ajustar mano, duración y fuerza.
// Si un arma no lo tiene, se usa la detección automática con los valores por defecto.
public class hapticsScript : MonoBehaviour
{
    public enum HandSide { Auto, Left, Right }

    [Tooltip("Mano que vibra. Auto = se deduce de los padres (Hand de AutoHand o nombre 'left'/'right')")]
    public HandSide hand = HandSide.Auto;
    [Tooltip("Duración de la vibración en segundos")]
    public float duration = 0.15f;
    [Tooltip("Fuerza de la vibración (0-1)")]
    [Range(0f, 1f)] public float amplitude = 0.7f;

    public const float DefaultDuration = 0.15f;
    public const float DefaultAmplitude = 0.7f;

    public void Vibrate()
    {
        bool left = hand == HandSide.Auto ? IsLeft(transform) : hand == HandSide.Left;
        Vibrate(left, duration, amplitude);
    }

    // Hace vibrar la mano que sostiene "weapon" (usa su hapticsScript si lo tiene)
    public static void VibrateFor(Transform weapon)
    {
        if (weapon == null) return;

        hapticsScript h = weapon.GetComponentInParent<hapticsScript>();
        if (h != null) { h.Vibrate(); return; }

        Vibrate(IsLeft(weapon), DefaultDuration, DefaultAmplitude);
    }

    public static void Vibrate(bool left, float duration, float amplitude)
    {
        // 1) Vía AutoHand (el player XR tiene XRHandControllerLink)
        HandControllerLink link = left ? HandControllerLink.handLeft : HandControllerLink.handRight;
        if (link != null)
        {
            link.TryHapticImpulse(duration, amplitude);
            return;
        }

        // 2) Si no hay AutoHand, directamente al mando XR
        InputDevice device = InputDevices.GetDeviceAtXRNode(left ? XRNode.LeftHand : XRNode.RightHand);
        if (device.isValid && device.TryGetHapticCapabilities(out HapticCapabilities caps) && caps.supportsImpulse)
            device.SendHapticImpulse(0u, amplitude, duration);
    }

    // Busca una Hand de AutoHand en los padres; si no, mira los nombres (ej. "Controller (left)")
    static bool IsLeft(Transform t)
    {
        Autohand.Hand autoHand = t.GetComponentInParent<Autohand.Hand>();
        if (autoHand != null) return autoHand.left;

        for (Transform p = t; p != null; p = p.parent)
        {
            string n = p.name.ToLower();
            if (n.Contains("left")) return true;
            if (n.Contains("right")) return false;
        }
        return false; // por defecto, derecha
    }
}
