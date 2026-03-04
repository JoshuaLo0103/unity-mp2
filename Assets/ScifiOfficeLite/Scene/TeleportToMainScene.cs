using UnityEngine;
using UnityEngine.SceneManagement;

public class TeleportToMainScene : MonoBehaviour
{
    public string sceneToLoad = "MainScene";
    public string spawnId = "Return";

    //private bool used = false;

    private void OnTriggerEnter(Collider other)
    {
        //if (used) return;

        //used = true;
        SpawnRequest.spawnId = spawnId;
        SceneManager.LoadScene(sceneToLoad);
    }
}