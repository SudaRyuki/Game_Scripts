using UnityEngine;
using Photon.Pun;

public class PlayerItemCollector : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        Player player = collision.GetComponent<Player>();
        Player2 player2 = collision.GetComponent<Player2>();

        // 接触した相手がプレイヤーでなければ処理を中断
        if (player == null && player2 == null) return;

        PhotonView pv = collision.GetComponent<PhotonView>();

        // PhotonViewがない、または自分が操作しているプレイヤーでなければ処理を中断
        if (pv == null || !pv.IsMine) return;

        // タグが "Item" の場合のみ取得処理を行う
        if (CompareTag("Item"))
        {
            if (player != null)
            {
                ScoreManager.instance.AddScore("Player", 1);
            }
            else if (player2 != null)
            {
                ScoreManager.instance.AddScore("Player2", 1);
            }

            // アイテムをネットワーク上から削除
            PhotonNetwork.Destroy(gameObject);
        }
    }
}