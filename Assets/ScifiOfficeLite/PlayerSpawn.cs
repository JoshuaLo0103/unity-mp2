using UnityEngine;

public class PlayerSpawn : MonoBehaviour
{
    void Start()
    {
        if (string.IsNullOrEmpty(SpawnRequest.spawnId))
            return;

        var points = FindObjectsOfType<SpawnPoint>();
        foreach (var p in points)
        {
            if (p.id == SpawnRequest.spawnId)
            {
                transform.position = p.transform.position;
                transform.rotation = p.transform.rotation;
                break;
            }
        }

        //SpawnRequest.spawnId = null;
    }
}