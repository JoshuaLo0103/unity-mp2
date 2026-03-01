using System;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager I { get; private set; }

    [Header("Resources")]
    public double stardust;
    public double aether;

    [Header("Rates (per second)")]
    public double stardustRate;
    public double aetherRate;

    public event Action OnChanged;

    private void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        stardust += stardustRate * dt;
        aether   += aetherRate * dt;
        OnChanged?.Invoke();
    }

    public bool TrySpendStardust(double amount)
    {
        if (stardust < amount) return false;
        stardust -= amount;
        OnChanged?.Invoke();
        return true;
    }

    public void AddStardustRate(double delta)
    {
        stardustRate += delta;
        OnChanged?.Invoke();
    }

    public void AddAetherRate(double delta)
    {
        aetherRate += delta;
        OnChanged?.Invoke();
    }
}