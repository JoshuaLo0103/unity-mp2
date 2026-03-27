using System.Collections;
using TMPro;
using UnityEngine;

public class WelcomeBackPopup : MonoBehaviour
{
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private ParticleSystem welcomeParticles;
    [SerializeField] private AudioSource welcomeAudio;
    [SerializeField] private float showDuration = 2.5f;

    private Coroutine connectRoutine;
    private Coroutine popupRoutine;

    private void Awake()
    {
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    private void OnEnable()
    {
        if (connectRoutine != null)
            StopCoroutine(connectRoutine);

        connectRoutine = StartCoroutine(ConnectWhenReady());
    }

    private IEnumerator ConnectWhenReady()
    {
        while (ResourceManager.I == null)
            yield return null;

        ResourceManager.I.OnOfflineProgressApplied -= ShowOfflineGains;
        ResourceManager.I.OnOfflineProgressApplied += ShowOfflineGains;

        TryShowPendingOfflineWelcome();

        connectRoutine = null;
    }

    private void OnDisable()
    {
        if (ResourceManager.I != null)
            ResourceManager.I.OnOfflineProgressApplied -= ShowOfflineGains;
    }

    private void ShowOfflineGains(double seedGain, double crystalGain)
    {
        if (SaveManager.I != null && SaveManager.I.IsWaitingForResumeChoice)
            return;

        if (seedGain <= 0d && crystalGain <= 0d)
            return;

        string message = $"Welcome back, Botanist!\n+{Mathf.RoundToInt((float)seedGain)} Seeds";
        if (crystalGain > 0d)
            message += $"\n+{Mathf.RoundToInt((float)crystalGain)} Crystals";

        if (popupRoutine != null)
            StopCoroutine(popupRoutine);

        popupRoutine = StartCoroutine(ShowPopup(message));

        ResourceManager.I?.ClearPendingOfflineWelcome();
    }

    public void TryShowPendingOfflineWelcome()
    {
        if (ResourceManager.I == null)
            return;

        if (SaveManager.I != null && SaveManager.I.IsWaitingForResumeChoice)
            return;

        if (ResourceManager.I.hasPendingOfflineWelcome)
        {
            ShowOfflineGains(
                ResourceManager.I.lastOfflineSeedGain,
                ResourceManager.I.lastOfflineCrystalGain
            );
        }
    }

    private IEnumerator ShowPopup(string message)
    {
        if (messageText != null)
            messageText.text = message;

        if (welcomeParticles != null)
        {
            welcomeParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            welcomeParticles.Play();
        }

        if (welcomeAudio != null)
            welcomeAudio.Play();

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        yield return new WaitForSeconds(showDuration);

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        popupRoutine = null;
    }
}