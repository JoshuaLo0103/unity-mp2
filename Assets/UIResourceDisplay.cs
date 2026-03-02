using System.Collections;
using TMPro;
using UnityEngine;

public class UIResourceDisplay : MonoBehaviour
{
    public enum ResourceType { Seed, Crystal }
    public ResourceType resourceType;

    public TMP_Text text;

    private bool subscribed = false;

    private void OnEnable()
    {
        // Start a routine that waits for ResourceManager to exist
        StartCoroutine(ConnectWhenReady());
    }

    private void OnDisable()
    {
        if (subscribed && ResourceManager.I != null)
            ResourceManager.I.OnChanged -= Refresh;

        subscribed = false;
    }

    private IEnumerator ConnectWhenReady()
    {
        // Wait until ResourceManager.I is assigned
        while (ResourceManager.I == null)
            yield return null;

        // Subscribe once
        if (!subscribed)
        {
            ResourceManager.I.OnChanged += Refresh;
            subscribed = true;
        }

        Refresh(); // update immediately once connected
    }

    private void Refresh()
    {
        if (text == null)
        {
            Debug.LogError($"[{name}] UIResourceDisplay: TMP text not assigned.", this);
            return;
        }

        if (ResourceManager.I == null)
        {
            text.text = (resourceType == ResourceType.Seed) ? "Seed: --" : "Crystal: --";
            return;
        }

        if (resourceType == ResourceType.Seed)
            text.text = $"Seed: {ResourceManager.I.seed:0}";
        else
            text.text = $"Crystal: {ResourceManager.I.crystal:0}";
    }
}
