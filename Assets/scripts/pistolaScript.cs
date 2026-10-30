using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class pistolaScript : MonoBehaviour
{
    public enum AimMode
    {
        ToPoint,          // dirección = de muzzle hacia la posición de aimDirection
        TransformForward  // dirección = eje Z (forward) de aimDirection
    }

    [Header("Transforms")]
    [Tooltip("Punto de salida del rayo (punta del cañón)")]
    public Transform muzzle;
    [Tooltip("Segundo transform que marca la dirección (ver Aim Mode). Si está vacío, se usa muzzle.forward")]
    public Transform aimDirection;
    public AimMode aimMode = AimMode.ToPoint;

    [Header("Raycast")]
    public float range = 100f;
    public LayerMask hitMask = ~0;
    public QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;
    [Tooltip("Ignora los colliders de la pistola y de sus padres (mano)")]
    public bool ignoreOwnColliders = true;

    [Header("Disparo")]
    public float fireRate = 0.2f;          // segundos entre disparos
    public bool automatic = false;

    [Header("Input")]
    public InputActionReference leftTriggerAction;
    [Tooltip("Gallet de la mà dreta (Auto Hand/Trigger Pressed (R)), per quan la pistola canvia de mà")]
    public InputActionReference rightTriggerAction;
    [Tooltip("Quin gallet dispara. El WeaponSwapper el canvia quan intercanvia les armes")]
    public bool useRightHand = false;

    InputActionReference ActiveTrigger => useRightHand && rightTriggerAction != null ? rightTriggerAction : leftTriggerAction;

    public void SetRightHand(bool right) => useRightHand = right;

    [Header("Línea de puntería")]
    public bool showLine = true;
    public float lineWidth = 0.01f;
    public Color lineColor = Color.red;
    [Tooltip("Color cuando el rayo apunta a un bloque")]
    public Color lineHitColor = Color.green;
    [Tooltip("Longitud de la línea. 0 = usar Range")]
    public float lineMaxLength = 0f;

    [Header("So del tret")]
    [Tooltip("Arrossega aquí l'àudio del tret (ex. VR-Game-Assets/Audio/gunSound.wav)")]
    public AudioClip shotClip;
    [Range(0f, 1f)] public float shotVolume = 1f;
    [Tooltip("Opcional: AudioSource on sona el tret. Si està buit, se'n crea un a la pistola")]
    private AudioSource shotSound;

    [Header("Extras")]
    public ParticleSystem muzzleFlash;
    public bool drawDebugRay = true;

    [Header("Debug")]
    public bool debugLogs = true;

    LineRenderer line;
    float nextFireTime;
    readonly RaycastHit[] hitsBuffer = new RaycastHit[16];
    readonly DistanceComparer comparer = new DistanceComparer();

    class DistanceComparer : IComparer<RaycastHit>
    {
        public int Compare(RaycastHit a, RaycastHit b)
        {
            return a.distance.CompareTo(b.distance);
        }
    }

    void Awake()
    {
        if (muzzle == null) muzzle = transform;

        // Si hi ha clip però no AudioSource, en creem un (so 3D a la pistola)
        if (shotSound == null && shotClip != null)
        {
            shotSound = GetComponent<AudioSource>();
            if (shotSound == null) shotSound = gameObject.AddComponent<AudioSource>();
            shotSound.playOnAwake = false;
            shotSound.spatialBlend = 1f;
        }

        line = GetComponent<LineRenderer>();
        if (line == null)
        {
            Debug.LogWarning("[Pistola] No hay LineRenderer en este GameObject, la línea no se mostrará");
        }
        else
        {
            line.positionCount = 2;
            line.useWorldSpace = true;
        }
    }

    void OnEnable()
    {
        foreach (InputActionReference r in new[] { leftTriggerAction, rightTriggerAction })
        {
            if (r == null) continue;
            r.action.Enable();
            r.action.performed += OnTrigger;
        }
    }

    void OnDisable()
    {
        foreach (InputActionReference r in new[] { leftTriggerAction, rightTriggerAction })
        {
            if (r == null) continue;
            r.action.performed -= OnTrigger;
        }
    }

    void Update()
    {
        InputActionReference trigger = ActiveTrigger;
        if (automatic && trigger != null && trigger.action.IsPressed())
            Fire();

        UpdateLine();
    }

    void OnTrigger(InputAction.CallbackContext ctx)
    {
        // Només dispara el gallet de la mà on hi ha la pistola
        InputActionReference trigger = ActiveTrigger;
        if (trigger == null || ctx.action != trigger.action) return;
        if (!automatic) Fire();
    }

    Vector3 GetDirection()
    {
        if (aimDirection == null)
            return muzzle.forward;

        if (aimMode == AimMode.ToPoint)
        {
            Vector3 toPoint = aimDirection.position - muzzle.position;
            // Si los dos transforms están en el mismo punto no hay dirección válida
            if (toPoint.sqrMagnitude > 0.000001f)
                return toPoint.normalized;

            if (debugLogs)
                Debug.LogWarning("[Pistola] muzzle y aimDirection están en la misma posición, uso muzzle.forward");
            return muzzle.forward;
        }

        return aimDirection.forward;
    }

    // Devuelve el primer impacto válido (ignora colliders propios)
    bool TryGetFirstHit(Vector3 origin, Vector3 dir, out RaycastHit result)
    {
        int count = Physics.RaycastNonAlloc(origin, dir, hitsBuffer, range, hitMask, triggerInteraction);
        Array.Sort(hitsBuffer, 0, count, comparer);

        for (int i = 0; i < count; i++)
        {
            if (ignoreOwnColliders && IsOwnCollider(hitsBuffer[i].collider.transform))
            {
                if (debugLogs)
                    Debug.Log("[Pistola] Ignorado collider propio: " + hitsBuffer[i].collider.name);
                continue;
            }

            result = hitsBuffer[i];
            return true;
        }

        result = default;
        return false;
    }

    void UpdateLine()
    {
        if (line == null) return;

        line.enabled = showLine;
        if (!showLine) return;

        Vector3 origin = muzzle.position;
        Vector3 dir = GetDirection();
        float length = lineMaxLength > 0f ? lineMaxLength : range;

        Vector3 end = origin + dir * length;
        bool aimingAtBlock = false;

        if (TryGetFirstHit(origin, dir, out RaycastHit hit))
        {
            if (hit.distance < length) end = hit.point;
            aimingAtBlock = hit.collider.GetComponentInParent<blockPistolaScript>() != null;
        }

        Color c = aimingAtBlock ? lineHitColor : lineColor;
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;
        line.startColor = c;
        line.endColor = c;
        line.SetPosition(0, origin);
        line.SetPosition(1, end);
    }

    public void Fire()
    {
        if (Time.time < nextFireTime) return;
        nextFireTime = Time.time + fireRate;

        Vector3 dir = GetDirection();

        if (drawDebugRay)
            Debug.DrawRay(muzzle.position, dir * range, Color.red, 0.2f);

        if (TryGetFirstHit(muzzle.position, dir, out RaycastHit hit))
        {
            blockPistolaScript block = hit.collider.GetComponentInParent<blockPistolaScript>();

            if (debugLogs)
                Debug.Log("[Pistola] Primer impacto: " + hit.collider.name
                    + " | capa: " + LayerMask.LayerToName(hit.collider.gameObject.layer)
                    + " | distancia: " + hit.distance.ToString("F2")
                    + " | tiene blockPistolaScript: " + (block != null));

            if (block != null)
            {
                block.hitted();
                hapticsScript.VibrateFor(transform); // vibra la mano de la pistola
            }
        }
        else if (debugLogs)
        {
            Debug.Log("[Pistola] El rayo no ha tocado ningún collider (revisa Hit Mask, rango y que los cubos tengan Collider)");
        }

        // PlayOneShot permet que els trets ràpids se superposin sense tallar-se
        if (shotSound != null)
        {
            if (shotClip != null) shotSound.PlayOneShot(shotClip, shotVolume);
            else {shotSound.Play();Debug.Log("Clip executat");}

        }
        if (muzzleFlash != null) muzzleFlash.Play();
        if (debugLogs) Debug.Log("Disparo realizado");
    }

    bool IsOwnCollider(Transform t)
    {
        // Colliders de la pistola, de sus hijos o de sus padres (la mano)
        return t.IsChildOf(transform) || transform.IsChildOf(t);
    }
}