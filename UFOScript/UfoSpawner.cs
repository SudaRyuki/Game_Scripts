using UnityEngine;
using Photon.Pun;

public class UfoSpawner : MonoBehaviour
{
    [Header("設定")]
    public string ufoPrefabName = "UFO";
    public float initialDelay = 10f;
    public float repeatRate = 20f;

    [Header("出現位置")]
    public Transform leftSpawnPoint;
    public Transform rightSpawnPoint;

    void Start()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient)
        {
            InvokeRepeating(nameof(SpawnUfo), initialDelay, repeatRate);
        }
    }

    void SpawnUfo()
    {
        Transform spawnPoint = leftSpawnPoint;
        GameObject ufo = null;

        if (PhotonNetwork.InRoom)
        {
            // オンラインならネットワーク越しに生成
            ufo = PhotonNetwork.Instantiate(ufoPrefabName, spawnPoint.position, Quaternion.identity);
        }
        else
        {
            // オフラインなら通常生成
            GameObject prefabToSpawn = Resources.Load<GameObject>(ufoPrefabName);
            if (prefabToSpawn != null)
            {
                ufo = Instantiate(prefabToSpawn, spawnPoint.position, Quaternion.identity);
            }
        }

        // 生成に成功した場合の共通処理
        if (ufo != null)
        {
            Rigidbody2D rb = ufo.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                float direction = 1f;
                rb.linearVelocity = new Vector2(3f * direction, 0);
            }

            // 破壊処理
            if (PhotonNetwork.InRoom)
            {
                // オンラインでの破壊はUfoController側で行う
            }
            else
            {
                // オフラインなら直接削除
                Destroy(ufo, 15f);
            }
        }
    }
}