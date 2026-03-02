using System.Collections;
using TMPro;
using UnityEngine;

public class UIResourcePanel : MonoBehaviour
{
    [Header("Text References")]
    public TMP_Text seedText;
    public TMP_Text crystalText;

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
            seedText.text = "Seed: --";
            crystalText.text = "Crystal: --";
            return;
        }

        seedText.text = $"Seed: {ResourceManager.I.seed:0}";
        crystalText.text = $"Crystal: {ResourceManager.I.crystal:0}";
    }
}