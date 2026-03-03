using UnityEngine;

public class PotPlanting : MonoBehaviour
{
    [Header("Plant Output")]
    public double seedRateIncrease = 1.0;

    private bool planted = false;

    private void OnTriggerEnter(Collider other)
    {
        if (planted) return;
        if (!other.CompareTag("Spore")) return;
        if (ResourceManager.I == null) return;

        // If the spore is still being held / kinematic, ignore
        Rigidbody rb = other.attachedRigidbody;
        if (rb != null && rb.isKinematic) return;

        double cost = ResourceManager.I.CurrentSporeCost;

        if (!ResourceManager.I.TrySpendSeed(cost))
            return;

        planted = true;

        ResourceManager.I.plantedCount += 1;
        ResourceManager.I.AddSeedRate(seedRateIncrease);

        // Prevent double-trigger weirdness then remove spore
        Collider col = other.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Destroy(other.gameObject);
    }
}