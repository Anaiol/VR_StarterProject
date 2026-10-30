using UnityEngine;

// Zona darrere del jugador: tot cub que hi entri compta com a fallada i es destrueix.
// Crea un GameObject buit a la GameScene, posa-hi un BoxCollider gran (darrere del jugador,
// que tapi tota la zona on arriben els cubs) i afegeix aquest script.
[RequireComponent(typeof(BoxCollider))]
public class CubeMissZone : MonoBehaviour
{
    void Awake()
    {
        GetComponent<BoxCollider>().isTrigger = true;

        // Els cubs no tenen Rigidbody: perquè salti OnTriggerEnter, la zona n'ha de tenir un
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
