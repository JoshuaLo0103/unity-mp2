using UnityEngine;

public class ResourceVisual3D : MonoBehaviour
{
    public enum ResourceType
    {
        Seed,
        Crystal
    }

    [Header("Resource")]
    public ResourceType resourceType;

    [Header("Particle System")]
    public ParticleSystem particles;

    [Header("Scaling")]
    public float minEmission = 2f;
    public float maxEmission = 50f;

    public float minSize = 0.05f;
    public float maxSize = 0.2f;

    public float minSpeed = 0.3f;
    public float maxSpeed = 1.5f;

    [Header("Resource Mapping")]
    public float resourceForMaxVisual = 200f;

    [Header("Optional Visual Root")]
    public Transform visualRoot;
    public float minScaleMultiplier = 1f;
    public float maxScaleMultiplier = 1.3f;

    private Vector3 baseScale;

    private void Start()
    {
        if (visualRoot == null)
            visualRoot = transform;

        baseScale = visualRoot.localScale;
    }

    private void Update()
    {
        if (ResourceManager.I == null || particles == null)
            return;

        float currentValue = GetCurrentResourceValue();
        float t = Mathf.Clamp01(currentValue / resourceForMaxVisual);

        UpdateParticles(t);
        UpdateScale(t);
    }

    private float GetCurrentResourceValue()
    {
        switch (resourceType)
        {
            case ResourceType.Seed:
                return (float)ResourceManager.I.seed;

            case ResourceType.Crystal:
                return (float)ResourceManager.I.crystal;

            default:
                return 0f;
        }
    }

    private void UpdateParticles(float t)
    {
        var emission = particles.emission;
        emission.rateOverTime = new ParticleSystem.MinMaxCurve(
            Mathf.Lerp(minEmission, maxEmission, t)
        );

        var main = particles.main;
        main.startSize = new ParticleSystem.MinMaxCurve(
            Mathf.Lerp(minSize, maxSize, t)
        );
        main.startSpeed = new ParticleSystem.MinMaxCurve(
            Mathf.Lerp(minSpeed, maxSpeed, t)
        );
    }

    private void UpdateScale(float t)
    {
        float scaleMultiplier = Mathf.Lerp(minScaleMultiplier, maxScaleMultiplier, t);
        visualRoot.localScale = baseScale * scaleMultiplier;
    }
}