using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class TrophyMilestonePrinter : MonoBehaviour
{
    [System.Serializable]
    public class TrophyMilestone
    {
        public string label = "Bronze Trophy";
        public double requiredLifetimeSeed = 1000d;
        public double requiredCrystal = 0d;
        public GameObject trophyPrefab;
        public Transform shelfSlot;
        [HideInInspector] public bool unlocked;
    }

    [Header("References")]
    [SerializeField] private ResourceManager resourceManager;
    [SerializeField] private Transform printerSpawnPoint;
    [SerializeField] private Transform viewerAnchor;

    [Header("Showcase Motion")]
    [SerializeField] private float viewerDistance = 1.25f;
    [SerializeField] private float viewerHeightOffset = -0.05f;
    [SerializeField] private float spawnToViewerSeconds = 1.2f;
    [SerializeField] private float viewerHoldSeconds = 1.75f;
    [SerializeField] private float viewerToShelfSeconds = 1.1f;

    [Header("Rotation")]
    [SerializeField] private Vector3 trophyRotationOffsetEuler = Vector3.zero;
    [SerializeField] private Vector3 viewerRotationOffsetEuler = Vector3.zero;

    [Header("Milestones")]
    [SerializeField] private TrophyMilestone[] milestones =
    {
        new TrophyMilestone { label = "Bronze Trophy", requiredLifetimeSeed = 1000d, requiredCrystal = 0d },
        new TrophyMilestone { label = "Silver Trophy", requiredLifetimeSeed = 5000d, requiredCrystal = 0d },
        new TrophyMilestone { label = "Gold Trophy", requiredLifetimeSeed = 10000d, requiredCrystal = 0d }
    };

    [Header("Events")]
    public UnityEvent onTrophyPrinted;
    public UnityEvent onTrophyPlaced;

    private bool subscribed;
    private bool sequenceRunning;

    private void Reset()
    {
        printerSpawnPoint = transform;
    }

    private void OnEnable()
    {
        StartCoroutine(SubscribeWhenReady());
    }

    private void OnDisable()
    {
        if (subscribed && resourceManager != null)
            resourceManager.OnChanged -= CheckMilestones;

        subscribed = false;
    }

    private IEnumerator SubscribeWhenReady()
    {
        while (resourceManager == null)
        {
            resourceManager = ResourceManager.I;
            if (resourceManager == null)
                yield return null;
        }

        if (!subscribed)
        {
            resourceManager.OnChanged += CheckMilestones;
            subscribed = true;
        }

        CheckMilestones();
    }

    private void CheckMilestones()
    {
        if (resourceManager == null || sequenceRunning || milestones == null)
            return;

        for (int i = 0; i < milestones.Length; i++)
        {
            TrophyMilestone milestone = milestones[i];
            if (milestone.unlocked) continue;
            if (resourceManager.lifetimeSeedProduced < milestone.requiredLifetimeSeed) continue;
            if (resourceManager.crystal < milestone.requiredCrystal) continue;

            StartCoroutine(RunUnlockSequence(i));
            return;
        }
    }

    private IEnumerator RunUnlockSequence(int milestoneIndex)
    {
        sequenceRunning = true;

        TrophyMilestone milestone = milestones[milestoneIndex];
        milestone.unlocked = true;

        if (milestone.trophyPrefab == null)
        {
            Debug.LogWarning($"Milestone '{milestone.label}' has no trophy prefab assigned.");
            sequenceRunning = false;
            CheckMilestones();
            yield break;
        }

        if (milestone.shelfSlot == null)
        {
            Debug.LogWarning($"Milestone '{milestone.label}' has no shelf slot assigned.");
            sequenceRunning = false;
            CheckMilestones();
            yield break;
        }

        Vector3 spawnPos = printerSpawnPoint != null ? printerSpawnPoint.position : transform.position;
        Quaternion baseRotationOffset = Quaternion.Euler(trophyRotationOffsetEuler);
        Quaternion viewerRotationOffset = Quaternion.Euler(viewerRotationOffsetEuler);
        Quaternion spawnRot = (printerSpawnPoint != null ? printerSpawnPoint.rotation : transform.rotation) * baseRotationOffset;

        GameObject trophyInstance = Instantiate(milestone.trophyPrefab, spawnPos, spawnRot);
        onTrophyPrinted?.Invoke();

        Transform currentViewerAnchor = ResolveViewerAnchor();
        Vector3 viewerPos = ComputeViewerShowcasePosition(currentViewerAnchor);
        Quaternion viewerRot = ComputeFacingRotation(viewerPos, currentViewerAnchor) * baseRotationOffset * viewerRotationOffset;

        yield return MoveTransform(trophyInstance.transform, spawnPos, viewerPos, spawnRot, viewerRot, spawnToViewerSeconds);
        yield return new WaitForSeconds(viewerHoldSeconds);

        Vector3 shelfPos = milestone.shelfSlot.position;
        Quaternion shelfRot = milestone.shelfSlot.rotation * baseRotationOffset;
        yield return MoveTransform(trophyInstance.transform, viewerPos, shelfPos, viewerRot, shelfRot, viewerToShelfSeconds);

        trophyInstance.transform.SetPositionAndRotation(shelfPos, shelfRot);
        trophyInstance.transform.SetParent(milestone.shelfSlot, true);
        onTrophyPlaced?.Invoke();

        sequenceRunning = false;
        CheckMilestones();
    }

    private IEnumerator MoveTransform(
        Transform target,
        Vector3 fromPos,
        Vector3 toPos,
        Quaternion fromRot,
        Quaternion toRot,
        float duration)
    {
        if (target == null)
            yield break;

        float safeDuration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;

        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            float smooth = t * t * (3f - 2f * t);

            target.position = Vector3.LerpUnclamped(fromPos, toPos, smooth);
            target.rotation = Quaternion.SlerpUnclamped(fromRot, toRot, smooth);
            yield return null;
        }

        target.position = toPos;
        target.rotation = toRot;
    }

    private Transform ResolveViewerAnchor()
    {
        if (viewerAnchor != null)
            return viewerAnchor;

        if (Camera.main != null)
            return Camera.main.transform;

        return transform;
    }

    private Vector3 ComputeViewerShowcasePosition(Transform anchor)
    {
        Vector3 forward = anchor.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f)
            forward = anchor.forward;

        forward.Normalize();
        Vector3 basePos = anchor.position + (forward * viewerDistance);
        basePos.y = anchor.position.y + viewerHeightOffset;
        return basePos;
    }

    private Quaternion ComputeFacingRotation(Vector3 position, Transform anchor)
    {
        Vector3 toViewer = anchor.position - position;
        toViewer.y = 0f;
        if (toViewer.sqrMagnitude < 0.001f)
            toViewer = -anchor.forward;

        return Quaternion.LookRotation(toViewer.normalized, Vector3.up);
    }
}
