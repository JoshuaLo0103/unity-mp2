using UnityEngine;
using UnityEngine.SceneManagement;

public class TutorialPopup : MonoBehaviour
{
    public static TutorialPopup I;

    private const string TutorialSeenKey = "TUTORIAL_SEEN";

    [Header("Assign in scene")]
    public GameObject tutorialPanel;

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        RefreshTutorialState();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (tutorialPanel == null)
        {
            TutorialPanelMarker marker = FindFirstObjectByType<TutorialPanelMarker>(FindObjectsInactive.Include);
            if (marker != null)
                tutorialPanel = marker.gameObject;
        }

        RefreshTutorialState();
    }

    public void RefreshTutorialState()
    {
        bool tutorialSeen = PlayerPrefs.GetInt(TutorialSeenKey, 0) == 1;

        if (!tutorialSeen)
            ShowTutorial();
        else
            HideTutorial();
    }

    private void ShowTutorial()
    {
        Time.timeScale = 0f;

        if (tutorialPanel != null)
            tutorialPanel.SetActive(true);
    }

    private void HideTutorial()
    {
        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);

        Time.timeScale = 1f;
    }

    public void CloseTutorial()
    {
        PlayerPrefs.SetInt(TutorialSeenKey, 1);
        PlayerPrefs.Save();

        HideTutorial();
    }
}