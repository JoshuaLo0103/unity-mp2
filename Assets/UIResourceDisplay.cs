using System.Collections;
using TMPro;
using UnityEngine;

public class UIResourcePanel : MonoBehaviour
{
    [Header("Text References")]
    public TMP_Text seedText;
    public TMP_Text crystalText;
    public TMP_Text sporeCostText;

    private bool subscribed = false;

    private void OnEnable()
    {
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
        while (ResourceManager.I == null)
            yield return null;

        if (!subscribed)
        {
            ResourceManager.I.OnChanged += Refresh;
            subscribed = true;
        }

        Refresh();
    }

    private void Refresh()
    {
        if (ResourceManager.I == null)
        {
            SetTextIfAssigned(seedText, "Seed: --");
            SetTextIfAssigned(crystalText, "Crystal: --");
            SetTextIfAssigned(sporeCostText, "Next Spore: --");
            return;
        }

        SetTextIfAssigned(seedText, $"Seed: {ResourceManager.I.seed:0}");
        SetTextIfAssigned(crystalText, $"Crystal: {ResourceManager.I.crystal:0}");
        SetTextIfAssigned(sporeCostText, $"Next Spore: {ResourceManager.I.CurrentSporeCost:0}");
    }

    private static void SetTextIfAssigned(TMP_Text target, string value)
    {
        if (target == null)
            return;

        target.text = value;
    }
}
