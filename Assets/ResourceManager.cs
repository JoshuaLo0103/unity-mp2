using System;
using UnityEngine;
using UnityEngine.Serialization;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager I { get; private set; }

    [Header("Resources")]
    [FormerlySerializedAs("stardust")]
    public double seed;
    [FormerlySerializedAs("aether")]
    public double crystal;

    public bool seedPortalUnlocked = false;
    
    [Header("Rates (per second)")]
    [FormerlySerializedAs("stardustRate")]
    public double seedRate;
    [FormerlySerializedAs("aetherRate")]
    public double crystalRate;

    [Header("Lifetime Progression")]
    public double lifetimeSeedProduced;

    [Header("Planting Costs")]
    public double baseSporeCost = 10;
    public double sporeCostGrowth = 1.15;
    public int plantedCount = 0;

    public double CurrentSporeCost =>
        baseSporeCost * Math.Pow(sporeCostGrowth, plantedCount);

    public event Action OnChanged;

    private void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        double seedDelta = seedRate * dt;
        seed += seedDelta;
        if (seedDelta > 0d)
            lifetimeSeedProduced += seedDelta;

        crystal += crystalRate * dt;
        OnChanged?.Invoke();
    }

    public bool TrySpendSeed(double amount)
    {
        if (seed < amount) return false;
        seed -= amount;
        OnChanged?.Invoke();
        return true;
    }

    public void AddSeed(double amount)
    {
        if (amount <= 0d) return;
        seed += amount;
        lifetimeSeedProduced += amount;
        OnChanged?.Invoke();
    }

    public void AddCrystal(double amount)
    {
        if (amount <= 0d) return;
        crystal += amount;
        OnChanged?.Invoke();
    }

    public void AddSeedRate(double delta)
    {
        seedRate += delta;
        OnChanged?.Invoke();
    }

    public void AddCrystalRate(double delta)
    {
        crystalRate += delta;
        OnChanged?.Invoke();
    }

    // Compatibility helpers so existing UnityEvents or older scripts still work.
    [Obsolete("Use TrySpendSeed instead.")]
    public bool TrySpendStardust(double amount) => TrySpendSeed(amount);

    [Obsolete("Use AddSeed instead.")]
    public void AddStardust(double amount) => AddSeed(amount);

    [Obsolete("Use AddCrystal instead.")]
    public void AddAether(double amount) => AddCrystal(amount);

    [Obsolete("Use AddSeedRate instead.")]
    public void AddStardustRate(double delta) => AddSeedRate(delta);

    [Obsolete("Use AddCrystalRate instead.")]
    public void AddAetherRate(double delta) => AddCrystalRate(delta);
}
