using UnityEngine;
using UnityEngine.SceneManagement;

public class ResumePromptUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject panel;

    private void Awake()
    {
        gameObject.SetActive(true);

        if (panel != null)
            panel.SetActive(false);
        else
            Debug.LogError("ResumePromptUI: panel is not assigned.");
    }

    public void Show()
    {
        Debug.Log("ResumePromptUI.Show called");

        gameObject.SetActive(true);

        if (panel != null)
        {
            panel.SetActive(true);
        }
        else
        {
            Debug.LogError("ResumePromptUI.Show: panel is not assigned.");
        }
    }

    public void OnResumeClicked()
    {
        Debug.Log("Resume clicked");

        if (panel != null)
            panel.SetActive(false);

        SaveManager.I?.ResumeSavedGame();

        WelcomeBackPopup popup = FindFirstObjectByType<WelcomeBackPopup>(FindObjectsInactive.Include);
        if (popup != null)
            popup.TryShowPendingOfflineWelcome();
    }

    public void OnRestartClicked()
    {
        Debug.Log("Restart clicked");

        if (panel != null)
            panel.SetActive(false);

        SaveManager.I?.DeleteAllSaveData();

        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}