using UnityEngine;

public class TutorialPopup : MonoBehaviour
{
    public GameObject tutorialPanel;

    void Start()
    {
        // Freeze gameplay
        Time.timeScale = 0f;
        tutorialPanel.SetActive(true);
    }

    public void CloseTutorial()
    {
        tutorialPanel.SetActive(false);

        // Resume gameplay
        Time.timeScale = 1f;
    }
}