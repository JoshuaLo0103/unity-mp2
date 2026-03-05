using UnityEngine;

public class TutorialPopup : MonoBehaviour
{
    public static TutorialPopup I;

    public GameObject tutorialPanel;

    private bool tutorialShown = false;

    void Awake()
    {
        // Singleton + persist across scenes
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        ShowIfNeeded();
    }

    private void ShowIfNeeded()
    {
        if (!tutorialShown)
        {
            Time.timeScale = 0f;
            tutorialPanel.SetActive(true);
            tutorialShown = true;
        }
        else
        {
            tutorialPanel.SetActive(false);
        }
    }

    public void CloseTutorial()
    {
        tutorialPanel.SetActive(false);
        Time.timeScale = 1f;
    }
}