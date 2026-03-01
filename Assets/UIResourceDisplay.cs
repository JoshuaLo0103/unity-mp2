using TMPro;
using UnityEngine;

public class UIResourceDisplay : MonoBehaviour
{
    public enum ResourceType { Stardust, Aether }
    public ResourceType resourceType;

    public TextMeshPro text;

    void OnEnable()
    {
        if (ResourceManager.I != null)
            ResourceManager.I.OnChanged += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        if (ResourceManager.I != null)
            ResourceManager.I.OnChanged -= Refresh;
    }

    void Refresh()
    {
        if (text == null)
        {
            Debug.LogError($"[{name}] UIResourceDisplay: TMP text not assigned.", this);
            return;
        }

        if (ResourceManager.I == null)
        {
            text.text = (resourceType == ResourceType.Stardust) ? "Stardust: --" : "Aether: --";
            return;
        }

        if (resourceType == ResourceType.Stardust)
            text.text = $"Stardust: {ResourceManager.I.stardust:0}";
        else
            text.text = $"Aether: {ResourceManager.I.aether:0}";
    }
}