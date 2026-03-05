using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SeedPortalDoor : MonoBehaviour
{
    [Header("Requirement")]
    public double requiredSeeds = 100;

    [Header("Scene to load")]
    public string targetSceneName;

    [Header("What to enable when unlocked")]
    public GameObject portalVisual;     // visuals (mesh/vfx). KEEP THIS as a CHILD, not the object with this script
    public Collider portalTrigger;      // trigger collider to walk through

    [Header("Player detection")]
    public string playerTag = "Player";

    private bool unlocked = false;
    private bool loading = false;
    private bool subscribed = false;

    private void Awake()
    {
        loading = false;
        unlocked = (ResourceManager.I != null && ResourceManager.I.seedPortalUnlocked);
        SetUnlocked(unlocked);
    }

    private void OnEnable()
    {
        // Always start; coroutine will wait until ResourceManager exists
        StartCoroutine(ConnectWhenReady());
    }

    private IEnumerator ConnectWhenReady()
    {
        while (ResourceManager.I == null)
            yield return null;

        if (!subscribed)
        {
            ResourceManager.I.OnChanged += CheckUnlock;
            subscribed = true;
        }

        CheckUnlock(); // run once after connecting
    }

    private void Start()
    {
        // In case RM already exists and OnChanged won't fire immediately
        CheckUnlock();
    }

    private void OnDisable()
    {
        if (subscribed && ResourceManager.I != null)
            ResourceManager.I.OnChanged -= CheckUnlock;

        subscribed = false;
    }

    private void CheckUnlock()
    {
        if (unlocked || ResourceManager.I == null) return;

        if (ResourceManager.I.seed >= requiredSeeds)
        {
            ResourceManager.I.TrySpendSeed(requiredSeeds);
            unlocked = true;
            ResourceManager.I.seedPortalUnlocked = true;
            SetUnlocked(true);
            //Debug.Log($"[Portal] Unlocked at seed={ResourceManager.I.seed} (req={requiredSeeds})");
        }
        else
        {
            // Optional debug:
            // Debug.Log($"[Portal] Locked seed={ResourceManager.I.seed}/{requiredSeeds}");
        }
    }

    private void SetUnlocked(bool on)
    {
        if (portalVisual != null) portalVisual.SetActive(on);
        if (portalTrigger != null) portalTrigger.enabled = on;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!unlocked || loading) return;

        // XR: often a child collider enters; accept if any parent is tagged Player
        bool isPlayer = other.CompareTag(playerTag) || (other.transform.root != null && other.transform.root.CompareTag(playerTag));
        if (!isPlayer) return;

        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            Debug.LogError("[Portal] targetSceneName is empty.");
            return;
        }

        loading = true;
        Debug.Log($"[Portal] Loading scene '{targetSceneName}'");
        SceneManager.LoadScene(targetSceneName);
    }
}