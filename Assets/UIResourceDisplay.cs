using TMPro;
using UnityEngine;

public class UIResourceDisplay : MonoBehaviour
{
    public enum ResourceType { Stardust, Aether }
    public ResourceType resourceType;

    public TextMeshPro text;

    void OnEnable()
    {
        ResourceManager.I.OnChanged += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        ResourceManager.I.OnChanged -= Refresh;
    }

    void Refresh()
    {
        if (resourceType == ResourceType.Stardust)
            text.text = $"Stardust: {ResourceManager.I.stardust:0}";
        else
            text.text = $"Aether: {ResourceManager.I.aether:0}";
    }
}