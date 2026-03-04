using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class CrystalDispenser : MonoBehaviour
{
    [Header("Spawn")]
    public GameObject crystalPrefab;     // PrismCrystal prefab
    public Transform spawnPoint;         // where it appears

    [Header("Cost")]
    public double seedCost = 25;

    [Header("Anti-spam")]
    public float cooldownSeconds = 0.5f;
    private float nextTimeAllowed = 0f;

    // called by XR Simple Interactable
    public void TryDispense()
    {
        Debug.Log("DISPENSER ACTIVATED");
        if (Time.time < nextTimeAllowed) return;
        nextTimeAllowed = Time.time + cooldownSeconds;

        if (ResourceManager.I == null) return;

        // Must be able to pay seed to get a crystal
        if (!ResourceManager.I.TrySpendSeed(seedCost))
            return;

        Instantiate(crystalPrefab, spawnPoint.position, spawnPoint.rotation);
    }
}