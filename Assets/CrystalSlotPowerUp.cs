using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class CrystalSlotPowerUp : MonoBehaviour
{
    public PotPlanting targetPot;

    [Header("Haptics")]
    [SerializeField] private float powerUpHapticAmplitude = 0.9f;
    [SerializeField] private float powerUpHapticDuration = 0.16f;

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
}
