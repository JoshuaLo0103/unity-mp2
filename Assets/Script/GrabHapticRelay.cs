using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(XRGrabInteractable))]
public class GrabHapticRelay : MonoBehaviour
{
    private XRGrabInteractable grabInteractable;
    private XRBaseInputInteractor lastInputInteractor;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
    }

    private void OnEnable()
    {
        grabInteractable.selectEntered.AddListener(OnSelectEntered);
    }

    private void OnDisable()
    {
        grabInteractable.selectEntered.RemoveListener(OnSelectEntered);
    }

    public bool SendHapticImpulse(float amplitude, float duration)
    {
        XRBaseInputInteractor inputInteractor =
            grabInteractable.GetOldestInteractorSelecting() as XRBaseInputInteractor ?? lastInputInteractor;

        if (inputInteractor == null)
            return false;

        return inputInteractor.SendHapticImpulse(amplitude, duration);
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        lastInputInteractor = args.interactorObject as XRBaseInputInteractor;
    }
}
