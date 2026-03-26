using UnityEngine;
using Photon.Pun;

public class ShieldSpawner : MonoBehaviour
{
    public string shieldPrefabName = "WoodenBuckler";

    public float spawnInterval = 30f;
    public Vector2 spawnAreaMin;
    public Vector2 spawnAreaMax;

    private float timer;

    void Update()
    {
        if (!GameManager.IsGameStarted) return;

        // オフライン、またはオンラインのホストだけがタイマーを進める
        if (!PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient)
        {
            timer += Time.deltaTime;

            if (timer >= spawnInterval)
            {
                SpawnShield();
                timer = 0f;
            }
        }
    }

    void SpawnShield()
    {
        Vector2 randomPosition = new Vector2(
            Random.Range(spawnAreaMin.x, spawnAreaMax.x),
            Random.Range(spawnAreaMin.y, spawnAreaMax.y)
        );

        if (PhotonNetwork.InRoom)
        {
            // ネットワーク越しに生成する
            PhotonNetwork.Instantiate(shieldPrefabName, randomPosition, Quaternion.identity);
        }
        else // オフラインなら
        {
            // 通常の方法で生成する
            GameObject prefabToSpawn = Resources.Load<GameObject>(shieldPrefabName);
            if (prefabToSpawn != null)
            {
                Instantiate(prefabToSpawn, randomPosition, Quaternion.identity);
            }
            
        }
    }
}