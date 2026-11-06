using UnityEngine;

public class WeaponSwapper : MonoBehaviour
{
    public static WeaponSwapper Instance { get; private set; }

    public pistolaScript pistol;
    public espasaScript sword;

    [Tooltip("Marca-ho per portar la pistola a la mà dreta (es pot canviar mentre jugues)")]
    public bool pistolInRightHand = false;

    [Header("Ajust de la posició a la mà contrària")]
    [Tooltip("Gir extra de la pistola quan va a l'altra mà. Per defecte 180° sobre Z (el canó) perquè no quedi al revés. Es pot ajustar mentre jugues")]
    public Vector3 swappedPistolRotation = new Vector3(0f, 0f, 180f);
    public Vector3 swappedPistolOffset = Vector3.zero;
    [Tooltip("Gir extra de l'espasa quan va a l'altra mà")]
    public Vector3 swappedSwordRotation = Vector3.zero;
    public Vector3 swappedSwordOffset = Vector3.zero;

    Transform pistolParent, swordParent;
    Vector3 pistolPos, swordPos;
    Quaternion pistolRot, swordRot;
    bool pistolStartsRight;

    bool currentlySwapped;
    float temporaryUntil = -1f;

    void Awake() => Instance = this;

    void Start()
    {
        if (pistol == null) pistol = FindFirstObjectByType<pistolaScript>();
        if (sword == null) sword = FindFirstObjectByType<espasaScript>();
        if (pistol == null || sword == null)
        {
            Debug.LogWarning("[WeaponSwapper] No trobo la pistola o l'espasa a l'escena", this);
            enabled = false;
            return;
        }

        pistolParent = pistol.transform.parent;
        pistolPos = pistol.transform.localPosition;
        pistolRot = pistol.transform.localRotation;
        swordParent = sword.transform.parent;
        swordPos = sword.transform.localPosition;
        swordRot = sword.transform.localRotation;

        pistolStartsRight = pistol.useRightHand;
        Apply(pistolInRightHand != pistolStartsRight);
    }

    void Update()
    {
        if (temporaryUntil > 0f && Time.time >= temporaryUntil)
        {
            temporaryUntil = -1f;
            pistolInRightHand = !pistolInRightHand;
        }

        bool wanted = pistolInRightHand != pistolStartsRight;
        if (wanted != currentlySwapped || wanted) Apply(wanted);
    }

    public void Swap() => pistolInRightHand = !pistolInRightHand;

    public void SwapFor(float seconds)
    {
        if (temporaryUntil > 0f)
        {
            temporaryUntil = Time.time + seconds;
            return;
        }
        pistolInRightHand = !pistolInRightHand;
        temporaryUntil = Time.time + seconds;
    }

    void Apply(bool swapped)
    {
        currentlySwapped = swapped;
        if (swapped)
        {
            Place(pistol.transform, swordParent, Mirror(pistolPos) + swappedPistolOffset,
                  Mirror(pistolRot) * Quaternion.Euler(swappedPistolRotation));
            Place(sword.transform, pistolParent, Mirror(swordPos) + swappedSwordOffset,
                  Mirror(swordRot) * Quaternion.Euler(swappedSwordRotation));
        }
        else
        {
            Place(pistol.transform, pistolParent, pistolPos, pistolRot);
            Place(sword.transform, swordParent, swordPos, swordRot);
        }
        pistol.SetRightHand(swapped ? !pistolStartsRight : pistolStartsRight);
    }

    static void Place(Transform t, Transform parent, Vector3 pos, Quaternion rot)
    {
        t.SetParent(parent, false);
        t.localPosition = pos;
        t.localRotation = rot;
    }

    static Vector3 Mirror(Vector3 p) => new Vector3(-p.x, p.y, p.z);
    static Quaternion Mirror(Quaternion q) => new Quaternion(q.x, -q.y, -q.z, q.w);
}
