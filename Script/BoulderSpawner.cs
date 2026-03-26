using UnityEngine;
using System.Collections;
using Photon.Pun;

public class BoulderSpawner : MonoBehaviourPunCallbacks
{
    [Header("ギミック用Prefab")]
    
    public string boulderPrefabName = "Boulder";
    public GameObject pebbleEffectPrefab;

    [Header("設定")]
    [SerializeField] private float initialDelay = 5f;
    [SerializeField] private float repeatRate = 10f;
    [SerializeField] private float warningTime = 2.5f;
    [SerializeField] private float boulderSpeed = 15f;
    [SerializeField] private bool moveFromRightToLeft = true;
    [SerializeField] private float boulderRotationSpeed = 360f;

    [Header("カメラシェイク")]
    public CameraShake cameraShake;
    [SerializeField] private float shakeMagnitude = 0.1f;

    [Header("位置")]
    public Transform warningPoint;
    public Transform spawnPoint;

    void Start()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient)
        {
            InvokeRepeating(nameof(StartGimmickSequence), initialDelay, repeatRate);
        }
    }

    private void StartGimmickSequence()
    {
        if (!GameManager.IsGameStarted) return;

        if (PhotonNetwork.InRoom)
        {
            // オンラインならRPCで全員の画面で警告と揺れを開始させる
            photonView.RPC(nameof(RpcStartBoulderSequence), RpcTarget.All);
        }
        else
        {
            // オフラインなら：自分だけで開始
            StartCoroutine(GimmickCoroutine());
        }
    }

    [PunRPC]
    private void RpcStartBoulderSequence()
    {
        // 全員のPCで警告演出を開始
        StartCoroutine(GimmickCoroutine());
    }

    private IEnumerator GimmickCoroutine()
    {
        GameObject currentPebbleEffect = null;

        //警告エフェクト
        if (pebbleEffectPrefab != null && warningPoint != null)
        {
            currentPebbleEffect = Instantiate(pebbleEffectPrefab, warningPoint.position, Quaternion.identity);
        }

        //カメラを揺らす
        if (cameraShake != null)
        {
            cameraShake.StartShake(warningTime, shakeMagnitude);
        }

        yield return new WaitForSeconds(warningTime);

        if (currentPebbleEffect != null)
        {
            Destroy(currentPebbleEffect);
        }

        //岩を生成
        if (!PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient)
        {
            if (spawnPoint != null)
            {
                GameObject boulder = null;

                if (PhotonNetwork.InRoom)
                {
                    // オンライン：ネットワーク生成
                    boulder = PhotonNetwork.Instantiate(boulderPrefabName, spawnPoint.position, Quaternion.identity);

                    float direction = moveFromRightToLeft ? -1f : 1f;
                    photonView.RPC(nameof(RpcSetBoulderPhysics), RpcTarget.All, boulder.GetComponent<PhotonView>().ViewID, direction);
                }
                else
                {
                    // オフライン：通常生成
                    GameObject prefabToSpawn = Resources.Load<GameObject>(boulderPrefabName);
                    if (prefabToSpawn != null)
                    {
                        boulder = Instantiate(prefabToSpawn, spawnPoint.position, Quaternion.identity);
                        SetBoulderPhysicsLocally(boulder);
                    }
                }

                if (!PhotonNetwork.InRoom && boulder != null)
                {
                    Destroy(boulder, 5f);
                }
               
            }
        }
    }

    // オンライン用の物理同期RPC
    [PunRPC]
    private void RpcSetBoulderPhysics(int viewID, float direction)
    {
        PhotonView targetView = PhotonView.Find(viewID);
        if (targetView != null)
        {
            Rigidbody2D rb = targetView.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = new Vector2(boulderSpeed * direction, 0);
                rb.angularVelocity = boulderRotationSpeed * -direction;
            }
        }
    }

    // オフライン用の物理設定
    private void SetBoulderPhysicsLocally(GameObject boulder)
    {
        Rigidbody2D rb = boulder.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            float direction = moveFromRightToLeft ? -1f : 1f;
            rb.linearVelocity = new Vector2(boulderSpeed * direction, 0);
            rb.angularVelocity = boulderRotationSpeed * -direction;
        }
    }
}