using UnityEngine;

public class CrystalSlotPowerUp : MonoBehaviour
{
    public PotPlanting targetPot;

    private void Reset()
    {
        targetPot = GetComponentInParent<PotPlanting>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (targetPot == null) return;
        if (!other.CompareTag("Crystal")) return;

        // If still being held, ignore
        Rigidbody rb = other.attachedRigidbody;
        if (rb != null && rb.isKinematic) return;

        bool applied = targetPot.TryApplyCrystal();
        if (!applied) return;

        // disappear
        Collider col = other.GetComponent<Collider>();
        if (col != null) col.enabled = false;
        Destroy(other.gameObject);
    }
}