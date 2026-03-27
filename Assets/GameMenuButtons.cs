using UnityEngine;

public class GameMenuButtons : MonoBehaviour
{
    public void SaveGame()
    {
        SaveManager.I?.SaveGame();
        Debug.Log("Manual save triggered.");
    }

    public void ExitGame()
    {
        SaveManager.I?.SaveGame();
        Debug.Log("Exit button pressed. Game saved.");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}