using System.Collections;
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

    [Header("Surprised Eyes")]
    [SerializeField] private bool enableSurprisedEyes = true;
    [SerializeField] private float surpriseScaleMultiplier = 1.35f;
    [SerializeField] private float maxSurpriseScaleMultiplier = 3f;
    [SerializeField] private float surpriseExpandDuration = 0.08f;
    [SerializeField] [Range(0f, 1.5f)] private float surpriseSpacingScale = 0.75f;
    [SerializeField] private float surpriseVerticalLiftPerStep = 0.07f;
    [SerializeField] private float surpriseForwardPushPerStep = 0.015f;

    [Header("Surprised Mouth")]
    [SerializeField] private string mouthRootName = "mouth";
    [SerializeField] private bool enableSurprisedMouth = true;
    [SerializeField] private float mouthScaleInfluence = 0.85f;
    [SerializeField] private float mouthVerticalDropPerStep = 0.06f;
    [SerializeField] private float mouthForwardPushPerStep = 0.01f;

    [Header("Mouth Expression")]
    [SerializeField] private bool enableMouthEmotion = true;
    [SerializeField] private Vector3 smileRotationOffsetEuler = Vector3.zero;
    [SerializeField] private Vector3 smilePositionOffset = new Vector3(0f, 0.08f, 0.018f);
    [SerializeField] private Vector3 smileScaleMultiplier = new Vector3(1.02f, 2.0f, 0.1f);
    [SerializeField] private Vector3 frownRotationOffsetEuler = new Vector3(180f, 0f, 0f);
    [SerializeField] private Vector3 frownPositionOffset = new Vector3(0f, -0.075f, 0.018f);
    [SerializeField] private Vector3 frownScaleMultiplier = new Vector3(1.02f, 2.0f, 0.1f);
    [SerializeField] private float emotionEaseInDuration = 0.08f;
    [SerializeField] private float emotionHoldDuration = 1.6f;
    [SerializeField] private float emotionEaseOutDuration = 0.65f;

    private Transform primaryController;
    private Transform secondaryController;
    private Transform[] eyes;
    private Quaternion[] restLocalRotations;
    private Vector3[] restLocalScales;
    private Vector3[] restLocalPositions;
    private Vector3 restEyeCenterLocalPosition;
    private Transform mouthRoot;
    private Vector3 restMouthLocalScale = Vector3.one;
    private Vector3 restMouthLocalPosition = Vector3.zero;
    private Quaternion restMouthLocalRotation = Quaternion.identity;
    private Renderer[] eyeRenderers;
    private bool[] defaultRendererStates;
    private float nextControllerRefreshTime;
    private float nextBlinkTime;
    private float blinkEndTime;
    private bool isBlinking;
    private float persistentSurpriseScale = 1f;
    private float currentSurpriseScale = 1f;
    private Coroutine surpriseRoutine;
    private float currentMouthEmotion;
    private Coroutine mouthEmotionRoutine;

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
        surpriseScaleMultiplier = Mathf.Max(1f, surpriseScaleMultiplier);
        maxSurpriseScaleMultiplier = Mathf.Max(1f, maxSurpriseScaleMultiplier);
        surpriseExpandDuration = Mathf.Clamp(surpriseExpandDuration, 0.01f, 1f);
        surpriseSpacingScale = Mathf.Clamp(surpriseSpacingScale, 0f, 1.5f);
        surpriseVerticalLiftPerStep = Mathf.Max(0f, surpriseVerticalLiftPerStep);
        surpriseForwardPushPerStep = Mathf.Max(0f, surpriseForwardPushPerStep);
        mouthScaleInfluence = Mathf.Max(0f, mouthScaleInfluence);
        mouthVerticalDropPerStep = Mathf.Max(0f, mouthVerticalDropPerStep);
        mouthForwardPushPerStep = Mathf.Max(0f, mouthForwardPushPerStep);
        emotionEaseInDuration = Mathf.Clamp(emotionEaseInDuration, 0.01f, 1f);
        emotionHoldDuration = Mathf.Clamp(emotionHoldDuration, 0f, 2f);
        emotionEaseOutDuration = Mathf.Clamp(emotionEaseOutDuration, 0.01f, 2f);

        if (modelLookAxis.sqrMagnitude < 0.0001f)
            modelLookAxis = Vector3.down;
    }

    private void OnEnable()
    {
        JuicyFeedbackEvents.Happened += HandleJuicyFeedback;
        ScheduleNextBlink();
    }

    private void OnDisable()
    {
        JuicyFeedbackEvents.Happened -= HandleJuicyFeedback;
        SetEyesVisible(true);
        ApplySurpriseScale(persistentSurpriseScale);
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
        Transform searchRoot = transform.parent != null ? transform.parent : transform;
        mouthRoot = FindNamedChild(searchRoot, mouthRootName);

        if (mouthRoot != null)
        {
            restMouthLocalScale = mouthRoot.localScale;
            restMouthLocalPosition = mouthRoot.localPosition;
            restMouthLocalRotation = mouthRoot.localRotation;
        }
        else
        {
            restMouthLocalScale = Vector3.one;
            restMouthLocalPosition = Vector3.zero;
            restMouthLocalRotation = Quaternion.identity;
        }

        if (leftEye != null && rightEye != null)
        {
            eyes = new[] { leftEye, rightEye };
            restLocalRotations = new[] { leftEye.localRotation, rightEye.localRotation };
            restLocalScales = new[] { leftEye.localScale, rightEye.localScale };
            restLocalPositions = new[] { leftEye.localPosition, rightEye.localPosition };
            restEyeCenterLocalPosition = (leftEye.localPosition + rightEye.localPosition) * 0.5f;
            CacheRenderers();
            ApplySurpriseScale(persistentSurpriseScale);
            return;
        }

        eyes = new Transform[0];
        restLocalRotations = new Quaternion[0];
        restLocalScales = new Vector3[0];
        restLocalPositions = new Vector3[0];
        restEyeCenterLocalPosition = Vector3.zero;
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

    private void HandleJuicyFeedback(JuicyFeedbackType feedbackType)
    {
        if (!enableSurprisedEyes || eyes == null || eyes.Length == 0)
        {
            PlayMouthEmotion(feedbackType);
            return;
        }

        persistentSurpriseScale = Mathf.Min(
            Mathf.Max(1f, persistentSurpriseScale) * surpriseScaleMultiplier,
            maxSurpriseScaleMultiplier);

        if (surpriseRoutine != null)
            StopCoroutine(surpriseRoutine);

        surpriseRoutine = StartCoroutine(PlaySurpriseEyesRoutine());
        PlayMouthEmotion(feedbackType);
    }

    private IEnumerator PlaySurpriseEyesRoutine()
    {
        float startScale = currentSurpriseScale;
        yield return AnimateSurpriseScale(startScale, persistentSurpriseScale, surpriseExpandDuration, useBackEase: true);
        surpriseRoutine = null;
    }

    private IEnumerator AnimateSurpriseScale(float from, float to, float duration, bool useBackEase)
    {
        float safeDuration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;

        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            float easedT = useBackEase ? EaseOutBack(t) : t;
            ApplySurpriseScale(Mathf.LerpUnclamped(from, to, easedT));
            yield return null;
        }

        ApplySurpriseScale(to);
    }

    private void ApplySurpriseScale(float scaleMultiplier)
    {
        currentSurpriseScale = scaleMultiplier;

        float extraGrowth = Mathf.Max(0f, scaleMultiplier - 1f);
        float spreadFactor = 1f + (extraGrowth * surpriseSpacingScale);
        Vector3 liftOffset = Vector3.up * (surpriseVerticalLiftPerStep * extraGrowth);
        Vector3 forwardOffset = Vector3.forward * (surpriseForwardPushPerStep * extraGrowth);

        if (eyes != null && restLocalScales != null)
        {
            for (int i = 0; i < eyes.Length; i++)
            {
                Transform eye = eyes[i];
                if (eye == null || i >= restLocalScales.Length || i >= restLocalPositions.Length)
                    continue;

                eye.localScale = restLocalScales[i] * scaleMultiplier;
                Vector3 centeredOffset = restLocalPositions[i] - restEyeCenterLocalPosition;
                eye.localPosition = restEyeCenterLocalPosition + (centeredOffset * spreadFactor) + liftOffset + forwardOffset;
            }
        }

        if (enableSurprisedMouth && mouthRoot != null)
        {
            float mouthScaleMultiplier = 1f + (extraGrowth * mouthScaleInfluence);
            Vector3 baseScale = restMouthLocalScale * mouthScaleMultiplier;
            Vector3 basePosition = restMouthLocalPosition
                + (Vector3.down * (mouthVerticalDropPerStep * extraGrowth))
                + (Vector3.forward * (mouthForwardPushPerStep * extraGrowth));
            ApplyMouthEmotionToBase(basePosition, baseScale);
        }
        else if (mouthRoot != null)
        {
            ApplyMouthEmotionToBase(restMouthLocalPosition, restMouthLocalScale);
        }
    }

    private void PlayMouthEmotion(JuicyFeedbackType feedbackType)
    {
        if (!enableMouthEmotion || mouthRoot == null)
            return;

        float targetEmotion = IsNegativeFeedback(feedbackType) ? -1f : 1f;

        if (mouthEmotionRoutine != null)
            StopCoroutine(mouthEmotionRoutine);

        mouthEmotionRoutine = StartCoroutine(AnimateMouthEmotion(targetEmotion));
    }

    private IEnumerator AnimateMouthEmotion(float targetEmotion)
    {
        float startEmotion = currentMouthEmotion;
        yield return AnimateMouthEmotionValue(startEmotion, targetEmotion, emotionEaseInDuration);

        if (emotionHoldDuration > 0f)
            yield return new WaitForSeconds(emotionHoldDuration);

        yield return AnimateMouthEmotionValue(currentMouthEmotion, 0f, emotionEaseOutDuration);
        mouthEmotionRoutine = null;
    }

    private IEnumerator AnimateMouthEmotionValue(float from, float to, float duration)
    {
        float safeDuration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;

        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            float easedT = EaseOutBack(t);
            currentMouthEmotion = Mathf.LerpUnclamped(from, to, easedT);
            ApplySurpriseScale(currentSurpriseScale);
            yield return null;
        }

        currentMouthEmotion = to;
        ApplySurpriseScale(currentSurpriseScale);
    }

    private void ApplyMouthEmotionToBase(Vector3 basePosition, Vector3 baseScale)
    {
        if (mouthRoot == null)
            return;

        float emotionMagnitude = Mathf.Abs(currentMouthEmotion);
        bool isPositive = currentMouthEmotion >= 0f;
        Vector3 rotationOffset = isPositive ? smileRotationOffsetEuler : frownRotationOffsetEuler;
        Vector3 positionOffset = isPositive ? smilePositionOffset : frownPositionOffset;
        Vector3 scaleMultiplier = isPositive ? smileScaleMultiplier : frownScaleMultiplier;

        Quaternion emotionRotation = Quaternion.Euler(Vector3.LerpUnclamped(Vector3.zero, rotationOffset, emotionMagnitude));
        Vector3 emotionPosition = Vector3.LerpUnclamped(Vector3.zero, positionOffset, emotionMagnitude);
        Vector3 emotionScale = Vector3.LerpUnclamped(Vector3.one, scaleMultiplier, emotionMagnitude);

        mouthRoot.localRotation = restMouthLocalRotation * emotionRotation;
        mouthRoot.localPosition = basePosition + emotionPosition;
        mouthRoot.localScale = Vector3.Scale(baseScale, emotionScale);
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

    private static float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    private static bool IsNegativeFeedback(JuicyFeedbackType feedbackType)
    {
        return feedbackType == JuicyFeedbackType.CrystalDispensed;
    }

}
