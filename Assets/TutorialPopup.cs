using UnityEngine;

public class TutorialPopup : MonoBehaviour
{
    public GameObject tutorialPanel;

    void Start()
    {
        if (PlayerPrefs.GetInt("TutorialShown", 0) == 0) {
            // Freeze gameplay
            Time.timeScale = 0f;
            tutorialPanel.SetActive(true);

            PlayerPrefs.SetInt("TutorialShown", 1);
            PlayerPrefs.Save();
        }
        else {
            tutorialPanel.SetActive(false);
        }
    }

    public void CloseTutorial()
    {
        tutorialPanel.SetActive(false);

        // Resume gameplay
        Time.timeScale = 1f;
    }
}