using UnityEngine;

public class AnthroEyesControllerFollower : MonoBehaviour
{
    [Header("Eye Roots")]
    [SerializeField] private string leftEyeName = "LeftEye";
    [SerializeField] private string rightEyeName = "RightEye";

    [Header("Target Controllers")]
    [SerializeField] private string primaryControllerName = "Right Controller";
    [SerializeField] private string secondaryControllerName = "Left Controller";
    [SerializeField] private bool useClosestController = true;
    [SerializeField] private float controllerRefreshInterval = 0.5f;

    [Header("Look Motion")]
    [SerializeField] private Vector3 modelLookAxis = Vector3.down;
    [SerializeField] private float maxLookAngle = 75f;
    [SerializeField] private float followSpeed = 12f;

    [Header("Blinking")]
    [SerializeField] private bool enableBlinking = true;
    [SerializeField] private float minBlinkInterval = 2.5f;
    [SerializeField] private float maxBlinkInterval = 6f;
    [SerializeField] private float blinkDuration = 0.08f;

    private Transform primaryController;
    private Transform secondaryController;
    private Transform[] eyes;
    private Quaternion[] restLocalRotations;
    private Renderer[] eyeRenderers;
    private bool[] defaultRendererStates;
    private float nextControllerRefreshTime;
    private float nextBlinkTime;
    private float blinkEndTime;
    private bool isBlinking;

    private void Awake()
    {
        CacheEyes();
        RefreshControllers(force: true);
    }

    private void OnValidate()
    {
        controllerRefreshInterval = Mathf.Max(0.1f, controllerRefreshInterval);
        maxLookAngle = Mathf.Clamp(maxLookAngle, 0f, 85f);
        followSpeed = Mathf.Max(0f, followSpeed);
        minBlinkInterval = Mathf.Max(0.1f, minBlinkInterval);
        maxBlinkInterval = Mathf.Max(minBlinkInterval, maxBlinkInterval);
        blinkDuration = Mathf.Clamp(blinkDuration, 0.01f, 1f);

        if (modelLookAxis.sqrMagnitude < 0.0001f)
            modelLookAxis = Vector3.down;
    }

    private void OnEnable()
    {
        ScheduleNextBlink();
    }

    private void OnDisable()
    {
        SetEyesVisible(true);
        isBlinking = false;
    }

    private void Update()
    {
        if (eyes == null || eyes.Length == 0)
            CacheEyes();

        RefreshControllers(force: false);

        Transform target = ChooseTargetController();
        float blend = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);

        for (int i = 0; i < eyes.Length; i++)
        {
            Transform eye = eyes[i];
            if (eye == null)
                continue;

            Quaternion desiredLocalRotation = restLocalRotations[i];
            if (target != null)
            {
                Transform eyeParent = eye.parent != null ? eye.parent : transform;
                Vector3 defaultLocalDirection = (restLocalRotations[i] * modelLookAxis).normalized;
                Vector3 targetLocalDirection = eyeParent.InverseTransformDirection((target.position - eye.position).normalized);
                Vector3 clampedLocalDirection = Vector3.RotateTowards(
                    defaultLocalDirection,
                    targetLocalDirection,
                    maxLookAngle * Mathf.Deg2Rad,
                    0f);

                if (clampedLocalDirection.sqrMagnitude > 0.0001f && defaultLocalDirection.sqrMagnitude > 0.0001f)
                {
                    Quaternion offset = Quaternion.FromToRotation(defaultLocalDirection, clampedLocalDirection);
                    desiredLocalRotation = offset * restLocalRotations[i];
                }
            }

            eye.localRotation = Quaternion.Slerp(eye.localRotation, desiredLocalRotation, blend);
        }

        UpdateBlinking();
    }

    private void CacheEyes()
    {
        Transform leftEye = FindNamedChild(transform, leftEyeName);
        Transform rightEye = FindNamedChild(transform, rightEyeName);

        if (leftEye != null && rightEye != null)
        {
            eyes = new[] { leftEye, rightEye };
            restLocalRotations = new[] { leftEye.localRotation, rightEye.localRotation };
            CacheRenderers();
            return;
        }

        eyes = new Transform[0];
        restLocalRotations = new Quaternion[0];
        eyeRenderers = new Renderer[0];
        defaultRendererStates = new bool[0];
    }

    private void CacheRenderers()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        eyeRenderers = renderers;
        defaultRendererStates = new bool[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            defaultRendererStates[i] = renderers[i] != null && renderers[i].enabled;
        }
    }

    private void RefreshControllers(bool force)
    {
        if (!force && Time.time < nextControllerRefreshTime)
            return;

        primaryController = FindNamedSceneTransform(primaryControllerName);
        secondaryController = FindNamedSceneTransform(secondaryControllerName);
        nextControllerRefreshTime = Time.time + controllerRefreshInterval;
    }

    private Transform ChooseTargetController()
    {
        if (!useClosestController || secondaryController == null)
            return primaryController != null ? primaryController : secondaryController;

        if (primaryController == null)
            return secondaryController;

        float primaryDistance = (primaryController.position - transform.position).sqrMagnitude;
        float secondaryDistance = (secondaryController.position - transform.position).sqrMagnitude;
        return primaryDistance <= secondaryDistance ? primaryController : secondaryController;
    }

    private void UpdateBlinking()
    {
        if (!enableBlinking)
        {
            SetEyesVisible(true);
            isBlinking = false;
            return;
        }

        if (eyeRenderers == null || eyeRenderers.Length == 0)
            return;

        if (isBlinking)
        {
            if (Time.time >= blinkEndTime)
            {
                SetEyesVisible(true);
                isBlinking = false;
                ScheduleNextBlink();
            }

            return;
        }

        if (Time.time >= nextBlinkTime)
        {
            SetEyesVisible(false);
            isBlinking = true;
            blinkEndTime = Time.time + blinkDuration;
        }
    }

    private void ScheduleNextBlink()
    {
        nextBlinkTime = Time.time + Random.Range(minBlinkInterval, maxBlinkInterval);
    }

    private void SetEyesVisible(bool visible)
    {
        if (eyeRenderers == null)
            return;

        for (int i = 0; i < eyeRenderers.Length; i++)
        {
            Renderer renderer = eyeRenderers[i];
            if (renderer == null)
                continue;

            renderer.enabled = visible && defaultRendererStates != null && i < defaultRendererStates.Length
                ? defaultRendererStates[i]
                : visible;
        }
    }

    private static Transform FindNamedChild(Transform root, string targetName)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child != root && child.name == targetName)
                return child;
        }

        return null;
    }

    private static Transform FindNamedSceneTransform(string targetName)
    {
        if (string.IsNullOrWhiteSpace(targetName))
            return null;

        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsSortMode.None);
        foreach (Transform candidate in transforms)
        {
            if (candidate == null || candidate.name != targetName)
                continue;

            if (!candidate.gameObject.activeInHierarchy)
                continue;

            return candidate;
        }

        return null;
    }
}
