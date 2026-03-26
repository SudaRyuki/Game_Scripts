using UnityEngine;
using System.Collections;
using Photon.Pun;

public class AutoDestroy : MonoBehaviour
{
    public float lifeTime = 2.0f;

    void Start()
    {
        StartCoroutine(DestroyCoroutine());
    }

    IEnumerator DestroyCoroutine()
    {
        yield return new WaitForSeconds(lifeTime);

        PhotonView pv = GetComponent<PhotonView>();

        // ネットワーク同期するオブジェクトの場合
        if (pv != null)
        {
            if (PhotonNetwork.InRoom)
            {
                // オンラインなら、オーナーだけが破壊の権利を持つ
                if (pv.IsMine)
                {
                    PhotonNetwork.Destroy(gameObject);
                }
                
            }
            else
            {
                // オフラインなら削除
                Destroy(gameObject);
            }
        }
        // オフラインの場合
        else
        {
            Destroy(gameObject);
        }
    }
}