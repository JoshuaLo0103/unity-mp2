using UnityEngine;

public class PotPlanting : MonoBehaviour
{
    [Header("Plant Output")]
    public double seedRateBase = 1.0;          // base contribution when planted
    public double perCrystalMultiplier = 1.5;  // each crystal multiplies this plant by this factor
    public double maxTotalMultiplier = 10.0;   // cap so it doesn't go infinite

    [Header("Visuals")]
    public GameObject plantVisual;

    [Header("Pot Visual")]
    public Transform potVisual;                // drag PotBig here (or whatever your pot mesh is)

    [Header("Plant Growth")]
    public float perCrystalScaleMultiplier = 1.15f; // each crystal grows plant by 15%
    public float maxScaleMultiplier = 2.0f;         // cap (2x original size)

    [Header("Pot Growth")]
    public float potScaleFactor = 1.0f;            // 1.0 = same growth as plant, 0.9 = slightly less

    private bool planted = false;

    // per-pot state
    private double currentMultiplier = 1.0;
    private double currentContribution = 0.0;
    private int crystalsApplied = 0;

    private Vector3 plantStartScale = Vector3.one;
    private Vector3 potStartScale = Vector3.one;

    private void Start()
    {
        if (plantVisual != null)
        {
            plantStartScale = plantVisual.transform.localScale;
            plantVisual.SetActive(false);
        }

        if (potVisual != null)
        {
            potStartScale = potVisual.localScale;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (planted) return;
        if (!other.CompareTag("Spore")) return;
        if (ResourceManager.I == null) return;

        // If still being held, ignore
        Rigidbody rb = other.attachedRigidbody;
        if (rb != null && rb.isKinematic) return;

        double cost = ResourceManager.I.CurrentSporeCost;
        if (!ResourceManager.I.TrySpendSeed(cost))
            return;

        planted = true;
        ResourceManager.I.plantedCount += 1;

        // start contribution
        currentMultiplier = 1.0;
        currentContribution = seedRateBase * currentMultiplier;
        ResourceManager.I.AddSeedRate(currentContribution);

        // show plant
        if (plantVisual != null)
            plantVisual.SetActive(true);

        // remove spore
        Collider col = other.GetComponent<Collider>();
        if (col != null) col.enabled = false;
        Destroy(other.gameObject);
    }

    // called by CrystalSlotPowerUp
    public bool TryApplyCrystal()
    {
        if (!planted) return false;
        if (ResourceManager.I == null) return false;

        // compute new multiplier with cap
        double newMultiplier = currentMultiplier * perCrystalMultiplier;
        if (newMultiplier > maxTotalMultiplier) newMultiplier = maxTotalMultiplier;

        // if already capped, do nothing
        if (newMultiplier <= currentMultiplier) return false;

        // add only the extra contribution for THIS pot
        double newContribution = seedRateBase * newMultiplier;
        double delta = newContribution - currentContribution;

        currentMultiplier = newMultiplier;
        currentContribution = newContribution;

        ResourceManager.I.AddSeedRate(delta);

        // grow visuals (capped)
        crystalsApplied += 1;
        GrowVisuals();

        return true;
    }

    private void GrowVisuals()
    {
        float targetScaleMult = Mathf.Pow(perCrystalScaleMultiplier, crystalsApplied);
        if (targetScaleMult > maxScaleMultiplier) targetScaleMult = maxScaleMultiplier;

        // plant grows
        if (plantVisual != null)
            plantVisual.transform.localScale = plantStartScale * targetScaleMult;

        // pot grows too
        if (potVisual != null)
            potVisual.localScale = potStartScale * (targetScaleMult * potScaleFactor);
    }
}