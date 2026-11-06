using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class CubeMissZone : MonoBehaviour
{
    void Awake()
    {
        GetComponent<BoxCollider>().isTrigger = true;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    void OnTriggerEnter(Collider other)
    {
        blockPistolaScript gun = other.GetComponentInParent<blockPistolaScript>();
        if (gun != null) { gun.Missed(); return; }

        blocEspasaScript sword = other.GetComponentInParent<blocEspasaScript>();
        if (sword != null) sword.Missed();
    }

    void OnDrawGizmos()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null) return;
        Gizmos.color = new Color(1f, 0f, 0f, 0.25f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(box.center, box.size);
    }
}
