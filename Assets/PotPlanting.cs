using System.Collections;
using UnityEngine;

public class PotPlanting : MonoBehaviour
{
    [Header("Plant Output")]
    public double seedRateBase = 1.0;
    public double perCrystalMultiplier = 1.5;
    public double maxTotalMultiplier = 10.0;

    [Header("Visuals")]
    public GameObject plantVisual;

    [Header("Pot Visual")]
    public Transform potVisual;

    [Header("Plant Growth")]
    public float perCrystalScaleMultiplier = 1.15f;
    public float maxScaleMultiplier = 2.0f;

    [Header("Pot Growth")]
    public float potScaleFactor = 1.0f;

    [Header("Planting Ease Animation")]
    public float plantPopDuration = 1.0f;
    public float startPlantScaleMultiplier = 0.02f;
    public float riseDistance = 0.12f;

    [Header("Crystal Ease Animation")]
    public float crystalGrowDuration = 0.4f;

    [Header("Planting Particles")]
    public ParticleSystem plantParticles;

    private bool planted = false;

    // per-pot state
    private double currentMultiplier = 1.0;
    private double currentContribution = 0.0;
    private int crystalsApplied = 0;

    private Vector3 plantStartScale = Vector3.one;
    private Vector3 potStartScale = Vector3.one;
    private Vector3 plantStartLocalPosition = Vector3.zero;

    private Coroutine plantAnimationCoroutine;
    private Coroutine crystalAnimationCoroutine;

    private void Start()
    {
        if (plantVisual != null)
        {
            plantStartScale = plantVisual.transform.localScale;
            plantStartLocalPosition = plantVisual.transform.localPosition;
            plantVisual.SetActive(false);
        }

        if (potVisual != null)
        {
            potStartScale = potVisual.localScale;
        }

        if (plantParticles != null)
        {
            plantParticles.Stop();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (planted) return;
        if (!other.CompareTag("Spore")) return;
        if (ResourceManager.I == null) return;

        Rigidbody rb = other.attachedRigidbody;
        if (rb != null && rb.isKinematic) return;

        double cost = ResourceManager.I.CurrentSporeCost;
        if (!ResourceManager.I.TrySpendSeed(cost))
            return;

        planted = true;
        ResourceManager.I.plantedCount += 1;

        currentMultiplier = 1.0;
        currentContribution = seedRateBase * currentMultiplier;
        ResourceManager.I.AddSeedRate(currentContribution);

        if (plantVisual != null)
        {
            plantVisual.SetActive(true);

            if (plantAnimationCoroutine != null)
                StopCoroutine(plantAnimationCoroutine);

            plantAnimationCoroutine = StartCoroutine(AnimatePlantOnPlanting());
        }

        PlayPlantingFeedback();

        Collider col = other.GetComponent<Collider>();
        if (col != null) col.enabled = false;
        Destroy(other.gameObject);
    }

    public bool TryApplyCrystal()
    {
        if (!planted) return false;
        if (ResourceManager.I == null) return false;

        double newMultiplier = currentMultiplier * perCrystalMultiplier;
        if (newMultiplier > maxTotalMultiplier) newMultiplier = maxTotalMultiplier;

        if (newMultiplier <= currentMultiplier) return false;

        double newContribution = seedRateBase * newMultiplier;
        double delta = newContribution - currentContribution;

        currentMultiplier = newMultiplier;
        currentContribution = newContribution;

        ResourceManager.I.AddSeedRate(delta);

        crystalsApplied += 1;
        AnimateCrystalGrowth();

        return true;
    }

    private void PlayPlantingFeedback()
    {
        if (plantParticles != null)
        {
            plantParticles.Play();
        }
    }

    private IEnumerator AnimatePlantOnPlanting()
    {
        Vector3 startScale = plantStartScale * startPlantScaleMultiplier;
        Vector3 targetScale = plantStartScale;

        Vector3 startPos = plantStartLocalPosition + Vector3.down * riseDistance;
        Vector3 targetPos = plantStartLocalPosition;

        plantVisual.transform.localScale = startScale;
        plantVisual.transform.localPosition = startPos;

        float elapsed = 0f;

        while (elapsed < plantPopDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / plantPopDuration);
            float easedT = EaseOutBack(t);

            plantVisual.transform.localScale = Vector3.LerpUnclamped(startScale, targetScale, easedT);
            plantVisual.transform.localPosition = Vector3.LerpUnclamped(startPos, targetPos, easedT);

            yield return null;
        }

        plantVisual.transform.localScale = targetScale;
        plantVisual.transform.localPosition = targetPos;

        plantAnimationCoroutine = null;
    }

    private void AnimateCrystalGrowth()
    {
        float targetScaleMult = Mathf.Pow(perCrystalScaleMultiplier, crystalsApplied);
        if (targetScaleMult > maxScaleMultiplier)
            targetScaleMult = maxScaleMultiplier;

        Vector3 plantTargetScale = plantStartScale * targetScaleMult;
        Vector3 potTargetScale = potStartScale * (targetScaleMult * potScaleFactor);

        if (crystalAnimationCoroutine != null)
            StopCoroutine(crystalAnimationCoroutine);

        crystalAnimationCoroutine = StartCoroutine(AnimateGrowthToTarget(plantTargetScale, potTargetScale));
    }

    private IEnumerator AnimateGrowthToTarget(Vector3 plantTargetScale, Vector3 potTargetScale)
    {
        Vector3 plantInitialScale = plantVisual != null ? plantVisual.transform.localScale : Vector3.one;
        Vector3 potInitialScale = potVisual != null ? potVisual.localScale : Vector3.one;

        float elapsed = 0f;

        while (elapsed < crystalGrowDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / crystalGrowDuration);
            float easedT = EaseOutBack(t);

            if (plantVisual != null)
            {
                plantVisual.transform.localScale =
                    Vector3.LerpUnclamped(plantInitialScale, plantTargetScale, easedT);
            }

            if (potVisual != null)
            {
                potVisual.localScale =
                    Vector3.LerpUnclamped(potInitialScale, potTargetScale, easedT);
            }

            yield return null;
        }

        if (plantVisual != null)
            plantVisual.transform.localScale = plantTargetScale;

        if (potVisual != null)
            potVisual.localScale = potTargetScale;

        crystalAnimationCoroutine = null;
    }

    private float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3) + c1 * Mathf.Pow(t - 1f, 2);
    }
}