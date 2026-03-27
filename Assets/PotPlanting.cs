using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

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

    [Header("Crystal Particles")]
    public ParticleSystem crystalParticles;

    [Header("Haptics")]
    [SerializeField] private float plantHapticAmplitude = 0.7f;
    [SerializeField] private float plantHapticDuration = 0.5f;

    [Header("Sound")]
    [SerializeField] private AudioClip plantSuccessClip;
    [SerializeField][Range(0f, 1f)] private float plantSoundVolume = 1f;
    [SerializeField][Range(0f, 1f)] private float plantSoundSpatialBlend = 1f;
    [SerializeField] private float plantSoundMinDistance = 2f;
    [SerializeField] private float plantSoundMaxDistance = 14f;
    [SerializeField] private AudioRolloffMode plantSoundRolloffMode = AudioRolloffMode.Linear;

    private bool planted;
    private double currentMultiplier = 1.0;
    private double currentContribution;
    private int crystalsApplied;
    private Vector3 plantStartScale = Vector3.one;
    private Vector3 potStartScale = Vector3.one;
    private Vector3 plantStartLocalPosition = Vector3.zero;
    private Coroutine plantAnimationCoroutine;
    private Coroutine crystalAnimationCoroutine;

    public bool IsPlanted => planted;

    private void Start()
    {
        if (plantVisual != null)
        {
            plantStartScale = plantVisual.transform.localScale;
            plantStartLocalPosition = plantVisual.transform.localPosition;
            plantVisual.SetActive(false);
        }

        if (potVisual != null)
            potStartScale = potVisual.localScale;

        if (plantParticles != null)
            plantParticles.Stop();

        if (crystalParticles != null)
            crystalParticles.Stop();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (planted || !other.CompareTag("Spore") || ResourceManager.I == null)
            return;

        Rigidbody rb = other.attachedRigidbody;
        if (rb != null && rb.isKinematic)
            return;

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
        SendPlantSuccessHaptics(other);
        PlayPlantSuccessSound();
        JuicyFeedbackEvents.Raise(JuicyFeedbackType.PlantSpore);

        Collider col = other.GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        Destroy(other.gameObject);
    }

    public bool TryApplyCrystal()
    {
        return TryApplyPurchasedMultiplier(perCrystalMultiplier);
    }

    public bool CanApplyPurchasedMultiplier(double factor)
    {
        if (!planted || ResourceManager.I == null || factor <= 1d)
            return false;

        double newMultiplier = currentMultiplier * factor;
        if (newMultiplier > maxTotalMultiplier)
            newMultiplier = maxTotalMultiplier;

        return newMultiplier > currentMultiplier;
    }

    public bool TryApplyPurchasedMultiplier(double factor)
    {
        if (!CanApplyPurchasedMultiplier(factor))
            return false;

        double newMultiplier = currentMultiplier * factor;
        if (newMultiplier > maxTotalMultiplier)
            newMultiplier = maxTotalMultiplier;

        double newContribution = seedRateBase * newMultiplier;
        double delta = newContribution - currentContribution;

        currentMultiplier = newMultiplier;
        currentContribution = newContribution;

        ResourceManager.I.AddSeedRate(delta);

        crystalsApplied += 1;
        AnimateCrystalGrowth();
        PlayCrystalFeedback();

        return true;
    }

    private void PlayPlantingFeedback()
    {
        if (plantParticles != null)
            plantParticles.Play();
    }

    private void PlayCrystalFeedback()
    {
        if (crystalParticles != null)
            crystalParticles.Play();
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

    private static float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3) + c1 * Mathf.Pow(t - 1f, 2);
    }

    private void SendPlantSuccessHaptics(Collider sporeCollider)
    {
        GrabHapticRelay hapticRelay = sporeCollider.GetComponentInParent<GrabHapticRelay>();
        if (hapticRelay != null)
        {
            hapticRelay.SendHapticImpulse(plantHapticAmplitude, plantHapticDuration);
            return;
        }

        XRGrabInteractable grabInteractable = sporeCollider.GetComponentInParent<XRGrabInteractable>();
        if (grabInteractable == null)
            return;

        IXRSelectInteractor selectingInteractor =
            grabInteractable.GetOldestInteractorSelecting() ?? grabInteractable.firstInteractorSelecting;

        if (selectingInteractor is XRBaseInputInteractor inputInteractor)
            inputInteractor.SendHapticImpulse(plantHapticAmplitude, plantHapticDuration);
    }

    private void PlayPlantSuccessSound()
    {
        OneShotSpatialAudio.Play(
            plantSuccessClip,
            transform.position,
            plantSoundVolume,
            plantSoundSpatialBlend,
            plantSoundMinDistance,
            plantSoundMaxDistance,
            plantSoundRolloffMode);
    }
}