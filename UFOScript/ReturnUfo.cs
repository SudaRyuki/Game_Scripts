using UnityEngine;
using Photon.Pun; 

public class ReturnUfo : MonoBehaviourPunCallbacks
{
    [Header("設定")]
    public float lifeTime = 2.5f; 
    public float downSpeed = 3f;  

    void Start()
    {
        if (PhotonNetwork.InRoom)
        {
            if (photonView.IsMine)
            {
                Invoke(nameof(DestroyNetworkObject), lifeTime);
            }
        }
        else
        {
            // 2.5秒後に削除
            Destroy(gameObject, lifeTime);
        }
    }

    void Update()
    {
        // ゆっくり下降する
        transform.Translate(Vector3.down * downSpeed * Time.deltaTime);
    }

    // ネットワーク越しに消去するためのメソッド
    private void DestroyNetworkObject()
    {
        if (PhotonNetwork.InRoom && photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }
}