using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class MotherSeedClicker : MonoBehaviour
{
    [Header("Reward")]
    [FormerlySerializedAs("stardustPerHit")]
    [SerializeField] private float seedPerHit = 1f;

    [Header("Cooldown")]
    [SerializeField] private float cooldownSeconds = 2f;
    [SerializeField] private Image cooldownRing; // optional world-space radial image

    [Header("Hit Detection")]
    [SerializeField] private string handTag = "PlayerHand";

    [Header("Events (optional VFX/SFX)")]
    public UnityEvent onClicked;
    public UnityEvent onBlockedByCooldown;
    public UnityEvent onCooldownFinished;

    private float nextReadyAt = 0f;
    private bool wasCoolingDown = false;

    private void Awake()
    {
        cooldownSeconds = Mathf.Max(0.01f, cooldownSeconds);
        UpdateRing(1f);
    }

    private void Update()
    {
        float remaining = Mathf.Max(0f, nextReadyAt - Time.time);
        bool cooling = remaining > 0f;

        float normalized = cooling ? 1f - (remaining / cooldownSeconds) : 1f;
        UpdateRing(normalized);

        if (wasCoolingDown && !cooling)
            onCooldownFinished?.Invoke();

        wasCoolingDown = cooling;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(handTag)) return;
        TryClick();
    }

    private void TryClick()
    {
        if (ResourceManager.I == null) return;

        if (Time.time < nextReadyAt)
        {
            onBlockedByCooldown?.Invoke();
            return;
        }

        ResourceManager.I.AddSeed(seedPerHit);
        nextReadyAt = Time.time + cooldownSeconds;
        onClicked?.Invoke();
    }

    private void UpdateRing(float fill01)
    {
        if (cooldownRing != null)
            cooldownRing.fillAmount = Mathf.Clamp01(fill01);
    }
}
