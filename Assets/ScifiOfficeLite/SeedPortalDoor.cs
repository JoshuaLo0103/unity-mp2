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
    public GameObject portalVisual;    
    public Collider portalTrigger;      // trigger collider to walk through

    [Header("Player detection")]
    public string playerTag = "Player"; 

    private bool unlocked = false;
    private bool loading = false;

    private bool subscribed = false;

    private void Awake()
    {
        // Start locked
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
        //Debug.Log("Checking unlock: " + (ResourceManager.I != null ? ResourceManager.I.seed.ToString() : "RM null"));

        if (unlocked || ResourceManager.I == null) return;

        if (ResourceManager.I.seed >= requiredSeeds)
        {
            //Debug.Log("Portal unlocked!");
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
        //Debug.Log("Triggered by: " + other.name);
        //Debug.Log("isunlocked: " + unlocked);
        //Debug.Log("isloading: " + loading);
        if (!unlocked || loading) return;
        //if (!other.CompareTag(playerTag)) return;
        //Debug.Log("teleporting");
        loading = true;
        SceneManager.LoadScene(targetSceneName);
    }
}