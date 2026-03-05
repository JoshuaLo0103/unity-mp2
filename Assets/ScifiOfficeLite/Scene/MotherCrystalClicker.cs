using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;
using System.Collections;

public class MotherCrystalClicker : MonoBehaviour
{
    [Header("Reward")]
    [FormerlySerializedAs("aetherPerHit")]
    [SerializeField] private float crystalPerHit = 1f;

    [Header("Cooldown")]
    [SerializeField] private float cooldownSeconds = 2f;
    [SerializeField] private Image cooldownRing; // optional world-space radial image
    [SerializeField] private Color ringCoolingColor = new(1f, 0.2f, 0.2f, 0.9f);
    [SerializeField] private Color ringReadyColor = new(0.2f, 1f, 0.4f, 0.9f);

    [Header("Hit Detection")]
    [SerializeField] private string handTag = "PlayerHand";

    [Header("Visual Flash")]
    [SerializeField] private Renderer[] flashRenderers;
    [SerializeField] private Color flashColor = new(1f, 0.2f, 0.2f, 1f);
    [SerializeField] private float flashDuration = 0.12f;

    [Header("Events (optional VFX/SFX)")]
    public UnityEvent onClicked;
    public UnityEvent onBlockedByCooldown;
    public UnityEvent onCooldownFinished;

    private float nextReadyAt = 0f;
    private bool wasCoolingDown = false;
    private Coroutine flashRoutine;
    private MaterialPropertyBlock mpb;
    private int[] colorPropertyIds;
    private Color[] baseColors;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private void Awake()
    {
        cooldownSeconds = Mathf.Max(0.01f, cooldownSeconds);
        flashDuration = Mathf.Max(0.01f, flashDuration);
        SetupFlashTargets();
        CacheBaseColors();
        UpdateVisuals(1f);
    }

    private void Update()
    {
        float remaining = Mathf.Max(0f, nextReadyAt - Time.time);
        bool cooling = remaining > 0f;

        float normalized = cooling ? 1f - (remaining / cooldownSeconds) : 1f;
        UpdateVisuals(normalized);

        if (wasCoolingDown && !cooling)
        {
            RestoreBaseColor();
            onCooldownFinished?.Invoke();
        }

        wasCoolingDown = cooling;
    }

    private void OnTriggerEnter(Collider other)
    {
        //if (!other.CompareTag(handTag)) return;
        Debug.Log($"[Clicker] OnTriggerEnter: me={name} other={other.name} tag={other.tag} layer={LayerMask.LayerToName(other.gameObject.layer)} isTrigger={other.isTrigger}");
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

        // ✅ Award crystals instead of seeds
        ResourceManager.I.AddCrystal(crystalPerHit);

        nextReadyAt = Time.time + cooldownSeconds;
        UpdateVisuals(0f);
        PlayFlash();
        onClicked?.Invoke();
    }

    private void UpdateVisuals(float fill01)
    {
        float clamped = Mathf.Clamp01(fill01);
        UpdateRing(clamped);
    }

    private void OnEnable()
    {
        Debug.Log($"[Clicker] Enabled on {name} | scene={UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
    }

    private void Start()
    {
        Debug.Log($"[Clicker] Start on {name} | hasCollider={TryGetComponent<Collider>(out _)}");
    }

    private void UpdateRing(float normalized)
    {
        if (cooldownRing == null) return;

        cooldownRing.fillAmount = normalized;
        cooldownRing.color = Color.Lerp(ringCoolingColor, ringReadyColor, normalized);
    }

    private void OnCollisionEnter(Collision collision)
    {
        //if (!collision.collider.CompareTag(handTag)) return;
        var other = collision.collider;
        Debug.Log($"[Clicker] OnCollisionEnter: me={name} other={other.name} tag={other.tag} layer={LayerMask.LayerToName(other.gameObject.layer)} relVel={collision.relativeVelocity.magnitude:F3}");
        TryClick();
    }

    public void ClickFromController()
    {
        TryClick();
    }

    private void SetupFlashTargets()
    {
        if (flashRenderers != null && flashRenderers.Length > 0) return;
        flashRenderers = GetComponentsInChildren<Renderer>(true);
    }

    private void CacheBaseColors()
    {
        if (flashRenderers == null)
            flashRenderers = new Renderer[0];

        int count = flashRenderers.Length;
        colorPropertyIds = new int[count];
        baseColors = new Color[count];
        mpb = new MaterialPropertyBlock();

        for (int i = 0; i < count; i++)
        {
            Renderer r = flashRenderers[i];
            colorPropertyIds[i] = ResolveColorPropertyId(r);
            baseColors[i] = ReadCurrentColor(r, colorPropertyIds[i]);
        }
    }

    private int ResolveColorPropertyId(Renderer renderer)
    {
        if (renderer == null || renderer.sharedMaterial == null)
            return -1;

        if (renderer.sharedMaterial.HasProperty(BaseColorId))
            return BaseColorId;

        if (renderer.sharedMaterial.HasProperty(ColorId))
            return ColorId;

        return -1;
    }

    private Color ReadCurrentColor(Renderer renderer, int propId)
    {
        if (renderer == null || propId < 0)
            return Color.white;

        Material mat = renderer.sharedMaterial;
        if (mat != null && mat.HasProperty(propId))
            return mat.GetColor(propId);

        return Color.white;
    }

    private void PlayFlash()
    {
        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        SetRendererColor(flashColor);
        float elapsed = 0f;

        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / flashDuration);
            Color c = Color.Lerp(flashColor, Color.white, t);
            SetRendererColor(c);
            yield return null;
        }

        RestoreBaseColor();
        flashRoutine = null;
    }

    private void SetRendererColor(Color tint)
    {
        if (flashRenderers == null || colorPropertyIds == null) return;

        for (int i = 0; i < flashRenderers.Length; i++)
        {
            Renderer r = flashRenderers[i];
            int propId = colorPropertyIds[i];
            if (r == null || propId < 0) continue;

            Color baseColor = baseColors[i];
            Color target = new(
                baseColor.r * tint.r,
                baseColor.g * tint.g,
                baseColor.b * tint.b,
                baseColor.a);

            r.GetPropertyBlock(mpb);
            mpb.SetColor(propId, target);
            r.SetPropertyBlock(mpb);
        }
    }

    private void RestoreBaseColor()
    {
        if (flashRenderers == null || colorPropertyIds == null) return;

        for (int i = 0; i < flashRenderers.Length; i++)
        {
            Renderer r = flashRenderers[i];
            int propId = colorPropertyIds[i];
            if (r == null || propId < 0) continue;

            r.GetPropertyBlock(mpb);
            mpb.SetColor(propId, baseColors[i]);
            r.SetPropertyBlock(mpb);
        }
    }
}