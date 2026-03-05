using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SeedPortalDoor : MonoBehaviour
{
    [Header("Requirement")]
    public double requiredSeeds = 100;

    [Header("Unlock Cost Behavior")]
    public bool consumeSeedsOnFirstUnlock = true;
    public bool persistUnlockState = true;
    public string unlockSaveKey = "";

    [Header("Scene to load")]
    public string targetSceneName;

    [Header("What to enable when unlocked")]
    public GameObject portalVisual;    
    public Collider portalTrigger;      // trigger collider to walk through

    [Header("Player detection")]
    public string playerTag = "Player"; 

    private bool unlocked = false;
    private bool loading = false;
    private bool subscribed = false;
    private string resolvedUnlockKey;

    private void Awake()
    {
        resolvedUnlockKey = BuildUnlockKey();

        if (persistUnlockState && PlayerPrefs.GetInt(resolvedUnlockKey, 0) == 1)
        {
            unlocked = true;
            SetUnlocked(true);
            return;
        }

        unlocked = false;
        SetUnlocked(false);
    }

    private void OnEnable()
    {
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
            if (consumeSeedsOnFirstUnlock && !ResourceManager.I.TrySpendSeed(requiredSeeds))
                return;

            unlocked = true;
            SetUnlocked(true);
            SaveUnlockState();
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
        if (!other.CompareTag(playerTag)) return;

        loading = true;
        SceneManager.LoadScene(targetSceneName);
    }

    private string BuildUnlockKey()
    {
        if (!string.IsNullOrWhiteSpace(unlockSaveKey))
            return unlockSaveKey;

        return $"{SceneManager.GetActiveScene().path}:{GetTransformPath(transform)}:Unlocked";
    }

    private static string GetTransformPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = $"{t.name}/{path}";
        }

        return path;
    }

    private void SaveUnlockState()
    {
        if (!persistUnlockState)
            return;

        PlayerPrefs.SetInt(resolvedUnlockKey, 1);
        PlayerPrefs.Save();
    }
}
