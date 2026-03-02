using UnityEngine;

public class FloatingCrystal : MonoBehaviour
{
    [Header("Motion")]
    [SerializeField] private float bobAmplitude = 0.06f;
    [SerializeField] private float bobFrequency = 0.2f;
    [SerializeField] private bool rotateCrystal = false;
    [SerializeField] private float rotateDegPerSec = 8f;
    [SerializeField] private bool lockToStartPosition = true;

    [Header("Glow Pulse")]
    [SerializeField] private bool pulseEmission = true;
    [SerializeField] private bool useMaterialEmissionAsBase = true;
    [SerializeField] private Color emissionBase = new(0.45f, 0.55f, 0.65f, 1f);
    [SerializeField] private float pulseSpeed = 1.2f;
    [SerializeField] private float emissionMin = 1.0f;
    [SerializeField] private float emissionMax = 1.35f;

    private Vector3 startPos;
    private Quaternion startRot;
    private Renderer[] rends;
    private Color[] baseEmissionPerRenderer;
    private MaterialPropertyBlock mpb;
    private int emissionId;
    private float phase;

    void Awake()
    {
        startPos = transform.position;
        startRot = transform.rotation;
        rends = GetComponentsInChildren<Renderer>(true);
        baseEmissionPerRenderer = new Color[rends.Length];
        mpb = new MaterialPropertyBlock();
        emissionId = Shader.PropertyToID("_EmissionColor");
        phase = Random.Range(0f, Mathf.PI * 2f);

        CacheBaseEmissionColors();
    }

    void Update()
    {
        float t = Time.time + phase;

        Vector3 p = lockToStartPosition ? startPos : transform.position;
        p.y += Mathf.Sin(t * bobFrequency * Mathf.PI * 2f) * bobAmplitude;
        transform.position = p;

        if (rotateCrystal && Mathf.Abs(rotateDegPerSec) > 0.01f)
        {
            transform.Rotate(Vector3.up, rotateDegPerSec * Time.deltaTime, Space.World);
        }
        else
        {
            transform.rotation = startRot;
        }

        if (!pulseEmission)
        {
            return;
        }

        float k = 0.5f + 0.5f * Mathf.Sin(t * pulseSpeed);
        float emissionMul = Mathf.Lerp(emissionMin, emissionMax, k);

        for (int i = 0; i < rends.Length; i++)
        {
            Color baseColor = useMaterialEmissionAsBase ? baseEmissionPerRenderer[i] : emissionBase;
            Color e = baseColor * emissionMul;
            rends[i].GetPropertyBlock(mpb);
            mpb.SetColor(emissionId, e);
            rends[i].SetPropertyBlock(mpb);
        }
    }

    private void CacheBaseEmissionColors()
    {
        for (int i = 0; i < rends.Length; i++)
        {
            Color cached = emissionBase;
            Material[] mats = rends[i].sharedMaterials;
            for (int m = 0; m < mats.Length; m++)
            {
                Material mat = mats[m];
                if (mat == null || !mat.HasProperty(emissionId))
                {
                    continue;
                }

                Color candidate = mat.GetColor(emissionId);
                if (candidate.maxColorComponent > 0.001f)
                {
                    cached = candidate;
                    break;
                }
            }

            baseEmissionPerRenderer[i] = cached;
        }
    }
}
