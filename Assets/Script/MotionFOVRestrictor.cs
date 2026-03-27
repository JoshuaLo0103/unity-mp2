using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Comfort;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;

[DisallowMultipleComponent]
public class MotionFOVRestrictor : MonoBehaviour
{
    private sealed class ControllerDefaultProvider : ITunnelingVignetteProvider
    {
        private readonly TunnelingVignetteController controller;

        public ControllerDefaultProvider(TunnelingVignetteController controller)
        {
            this.controller = controller;
        }

        public VignetteParameters vignetteParameters => controller != null ? controller.defaultParameters : null;
    }

    [Header("References")]
    [SerializeField] private XROrigin xrOrigin;
    [SerializeField] private TunnelingVignetteController tunnelingVignette;
    [SerializeField] private MeshRenderer tunnelingVignetteRenderer;
    [SerializeField] private DynamicMoveProvider moveProvider;
    [SerializeField] private ContinuousTurnProvider continuousTurnProvider;
    [SerializeField] private SnapTurnProvider snapTurnProvider;
    [SerializeField] private TeleportationProvider teleportationProvider;
    [SerializeField] private CharacterController characterController;

    [Header("Activation")]
    [SerializeField] private float moveInputThreshold = 0.1f;
    [SerializeField] private float movementSpeedThreshold = 0.08f;
    [SerializeField] private float velocityThreshold = 0.08f;
    [SerializeField] private float teleportDistanceThreshold = 1f;
    [SerializeField] private bool useContinuousTurnComfort;
    [SerializeField] private bool useSnapTurnComfort;
    [SerializeField] private bool useTeleportComfort;
    [SerializeField] private bool debugForceVignette;

    private ControllerDefaultProvider defaultProvider;
    private bool vignetteQueued;
    private bool hasLastRigPosition;
    private Vector3 lastRigPosition;
    private float pendingDisableTimer = -1f;

    private void Reset()
    {
        ResolveReferences();
    }

    private void Awake()
    {
        ResolveReferences();
        EnsureProvider();
    }

    private void OnEnable()
    {
        ResolveReferences();
        EnsureProvider();
        SetVignetteVisualState(false);
        CacheRigPosition();
        vignetteQueued = false;
        pendingDisableTimer = -1f;
    }

    private void OnDisable()
    {
        pendingDisableTimer = -1f;
        vignetteQueued = false;
        SetVignetteVisualState(false);
    }

    private void Update()
    {
        bool shouldShow = ShouldShowVignette();
        if (shouldShow)
        {
            pendingDisableTimer = -1f;
            SetVignetteVisualState(true);
            SetQueuedState(true);
            return;
        }

        if (vignetteQueued)
        {
            SetQueuedState(false);
            pendingDisableTimer = GetDisableDelay();
        }

        if (pendingDisableTimer >= 0f)
        {
            pendingDisableTimer -= Time.unscaledDeltaTime;
            if (pendingDisableTimer <= 0f)
            {
                pendingDisableTimer = -1f;
                SetVignetteVisualState(false);
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ResolveReferences();
    }
#endif

    private bool ShouldShowVignette()
    {
        if (debugForceVignette)
            return true;

        // Prefer real move-stick input over inferred motion so the comfort effect
        // can fully clear when the player releases locomotion.
        if (HasMoveInput())
            return true;

        if (moveProvider == null)
        {
            if (MeasurePlanarSpeed() > movementSpeedThreshold)
                return true;

            if (characterController != null)
            {
                Vector3 planarVelocity = characterController.velocity;
                planarVelocity.y = 0f;
                if (planarVelocity.sqrMagnitude > velocityThreshold * velocityThreshold)
                    return true;
            }
        }

        if (useContinuousTurnComfort && continuousTurnProvider != null && continuousTurnProvider.isLocomotionActive)
            return true;

        if (useSnapTurnComfort && snapTurnProvider != null && snapTurnProvider.isLocomotionActive)
            return true;

        if (useTeleportComfort && teleportationProvider != null && teleportationProvider.isLocomotionActive)
            return true;

        return false;
    }

    private void ResolveReferences()
    {
        if (xrOrigin == null)
            xrOrigin = GetComponent<XROrigin>();

        if (tunnelingVignette == null && xrOrigin != null && xrOrigin.Camera != null)
            tunnelingVignette = xrOrigin.Camera.GetComponentInChildren<TunnelingVignetteController>(true);

        if (tunnelingVignetteRenderer == null && tunnelingVignette != null)
            tunnelingVignetteRenderer = tunnelingVignette.GetComponent<MeshRenderer>();

        if (moveProvider == null)
            moveProvider = GetComponentInChildren<DynamicMoveProvider>(true);

        if (continuousTurnProvider == null)
            continuousTurnProvider = GetComponentInChildren<ContinuousTurnProvider>(true);

        if (snapTurnProvider == null)
            snapTurnProvider = GetComponentInChildren<SnapTurnProvider>(true);

        if (teleportationProvider == null)
            teleportationProvider = GetComponentInChildren<TeleportationProvider>(true);

        if (characterController == null)
            characterController = GetComponent<CharacterController>();
    }

    private void EnsureProvider()
    {
        if (tunnelingVignette == null || defaultProvider != null)
            return;

        defaultProvider = new ControllerDefaultProvider(tunnelingVignette);
    }

    private void SetQueuedState(bool shouldShow, bool force = false)
    {
        if (tunnelingVignette == null)
            return;

        EnsureProvider();
        if (defaultProvider == null)
            return;

        if (!force && shouldShow == vignetteQueued)
            return;

        if (shouldShow)
        {
            tunnelingVignette.BeginTunnelingVignette(defaultProvider);
            vignetteQueued = true;
        }
        else
        {
            tunnelingVignette.EndTunnelingVignette(defaultProvider);
            vignetteQueued = false;
        }
    }

    private float GetDisableDelay()
    {
        if (defaultProvider?.vignetteParameters == null)
            return 0.25f;

        return Mathf.Max(
            0.05f,
            defaultProvider.vignetteParameters.easeOutDelayTime +
            defaultProvider.vignetteParameters.easeOutTime +
            0.02f);
    }

    private bool HasMoveInput()
    {
        if (moveProvider == null)
            return false;

        Vector2 leftInput = moveProvider.leftHandMoveInput.ReadValue();
        Vector2 rightInput = moveProvider.rightHandMoveInput.ReadValue();
        float thresholdSqr = moveInputThreshold * moveInputThreshold;

        return leftInput.sqrMagnitude > thresholdSqr || rightInput.sqrMagnitude > thresholdSqr;
    }

    private void SetVignetteVisualState(bool visible)
    {
        if (tunnelingVignette != null)
            tunnelingVignette.gameObject.SetActive(visible);

        if (tunnelingVignetteRenderer != null)
            tunnelingVignetteRenderer.enabled = visible;
    }

    private void CacheRigPosition()
    {
        Transform rigTransform = xrOrigin != null ? xrOrigin.Origin.transform : transform;
        lastRigPosition = rigTransform.position;
        hasLastRigPosition = true;
    }

    private float MeasurePlanarSpeed()
    {
        Transform rigTransform = xrOrigin != null ? xrOrigin.Origin.transform : transform;
        Vector3 currentPosition = rigTransform.position;

        if (!hasLastRigPosition)
        {
            lastRigPosition = currentPosition;
            hasLastRigPosition = true;
            return 0f;
        }

        Vector3 delta = currentPosition - lastRigPosition;
        lastRigPosition = currentPosition;
        delta.y = 0f;

        float distance = delta.magnitude;
        if (distance > teleportDistanceThreshold)
            return 0f;

        return distance / Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
    }
}
