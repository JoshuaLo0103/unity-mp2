using UnityEngine;

public static class OneShotSpatialAudio
{
    public static void Play(
        AudioClip clip,
        Vector3 position,
        float volume,
        float spatialBlend,
        float minDistance,
        float maxDistance,
        AudioRolloffMode rolloffMode)
    {
        if (clip == null)
            return;

        GameObject audioObject = new($"OneShotAudio_{clip.name}");
        audioObject.transform.position = position;

        AudioSource audioSource = audioObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.spatialBlend = spatialBlend;
        audioSource.minDistance = Mathf.Max(0.01f, minDistance);
        audioSource.maxDistance = Mathf.Max(audioSource.minDistance, maxDistance);
        audioSource.rolloffMode = rolloffMode;
        audioSource.dopplerLevel = 0f;

        audioSource.Play();
        Object.Destroy(audioObject, clip.length + 0.1f);
    }
}
