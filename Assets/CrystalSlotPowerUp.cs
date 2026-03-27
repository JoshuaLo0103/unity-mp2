using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class CrystalSlotPowerUp : MonoBehaviour
{
    public PotPlanting targetPot;

    [Header("Haptics")]
    [SerializeField] private float powerUpHapticAmplitude = 0.9f;
    [SerializeField] private float powerUpHapticDuration = 1f;

    [Header("Sound")]
    [SerializeField] private AudioClip powerUpSuccessClip;
    [SerializeField] [Range(0f, 1f)] private float powerUpSoundVolume = 1f;
    [SerializeField] [Range(0f, 1f)] private float powerUpSoundSpatialBlend = 1f;
    [SerializeField] private float powerUpSoundMinDistance = 3f;
    [SerializeField] private float powerUpSoundMaxDistance = 18f;
    [SerializeField] private AudioRolloffMode powerUpSoundRolloffMode = AudioRolloffMode.Linear;

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

        SendPowerUpSuccessHaptics(other);
        PlayPowerUpSuccessSound();
        JuicyFeedbackEvents.Raise(JuicyFeedbackType.PlantPowerUp);

        // disappear
        Collider col = other.GetComponent<Collider>();
        if (col != null) col.enabled = false;
        Destroy(other.gameObject);
    }

    private void SendPowerUpSuccessHaptics(Collider crystalCollider)
    {
        GrabHapticRelay hapticRelay = crystalCollider.GetComponentInParent<GrabHapticRelay>();
        if (hapticRelay != null)
        {
            hapticRelay.SendHapticImpulse(powerUpHapticAmplitude, powerUpHapticDuration);
            return;
        }

        XRGrabInteractable grabInteractable = crystalCollider.GetComponentInParent<XRGrabInteractable>();
        if (grabInteractable == null)
            return;

        IXRSelectInteractor selectingInteractor =
            grabInteractable.GetOldestInteractorSelecting() ?? grabInteractable.firstInteractorSelecting;

        if (selectingInteractor is XRBaseInputInteractor inputInteractor)
            inputInteractor.SendHapticImpulse(powerUpHapticAmplitude, powerUpHapticDuration);
    }

    private void PlayPowerUpSuccessSound()
    {
        OneShotSpatialAudio.Play(
            powerUpSuccessClip,
            transform.position,
            powerUpSoundVolume,
            powerUpSoundSpatialBlend,
            powerUpSoundMinDistance,
            powerUpSoundMaxDistance,
            powerUpSoundRolloffMode);
    }
}
