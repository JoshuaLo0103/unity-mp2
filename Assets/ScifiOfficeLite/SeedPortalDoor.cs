using UnityEngine;
using UnityEngine.SceneManagement;

public class SeedPortalDoor : MonoBehaviour
{
    [Header("Requirement")]
    public double requiredSeeds = 100;

    [Header("Scene to load")]
    public string targetSceneName;

    [Header("What to enable when unlocked")]
    public GameObject portalVisual;    
    public Collider portalTrigger;      // trigger collider to walk through

    [Header("Player detection")]
    public string playerTag = "Player"; 

    private bool unlocked = false;
    private bool loading = false;

    private void Awake()
    {
        // Start locked
        SetUnlocked(false);
    }

    private void OnEnable()
    {
        if (ResourceManager.I != null)
            ResourceManager.I.OnChanged += CheckUnlock;
    }

    private void Start()
    {
        CheckUnlock(); 
    }

    private void OnDisable()
    {
        if (ResourceManager.I != null)
            ResourceManager.I.OnChanged -= CheckUnlock;
    }

    private void CheckUnlock()
    {
        if (unlocked || ResourceManager.I == null) return;

        if (ResourceManager.I.seed >= requiredSeeds)
        {
            unlocked = true;
            SetUnlocked(true);
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
        //if (!other.CompareTag(playerTag)) return;

        loading = true;
        SceneManager.LoadScene(targetSceneName);
    }
}