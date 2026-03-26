using UnityEngine;
using Photon.Pun;

public class SpeedUpItemSpawner : MonoBehaviour
{
    public string itemPrefabName;
    public float spawnInterval = 80f;
    public Vector2 spawnRangeX = new Vector2(-8f, 8f);
    public Vector2 spawnRangeY = new Vector2(-3f, 1.5f);

    void Start()
    {
        // オフラインまたはオンラインのホストだけが、アイテム生成を開始する
        if (!PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient)
        {
            InvokeRepeating(nameof(SpawnItem), 80f, spawnInterval);
        }
    }

    void SpawnItem()
    {
        if (!GameManager.IsGameStarted) return;

        Vector3 spawnPosition = new Vector3(
            Random.Range(spawnRangeX.x, spawnRangeX.y),
            Random.Range(spawnRangeY.x, spawnRangeY.y),
            0f
        );

        // もしオンラインなら
        if (PhotonNetwork.InRoom)
        {
            // ネットワーク越しに生成する
            PhotonNetwork.Instantiate(itemPrefabName, spawnPosition, Quaternion.identity);
        }
        else // オフラインなら
        {
            // 通常のInstantiateで生成する
            GameObject prefabToSpawn = Resources.Load<GameObject>(itemPrefabName);
            if (prefabToSpawn != null)
            {
                Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
            }
           
        }
    }
}