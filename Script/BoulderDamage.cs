using UnityEngine;
using Photon.Pun;

public class BoulderDamage : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        // オンラインの場合、当たり判定はホストだけが行う
        if (PhotonNetwork.InRoom && !PhotonNetwork.IsMasterClient) return;

        // Playerに当たったかチェック
        Player player = other.gameObject.GetComponent<Player>();
        if (player != null)
        {
            if (player.IsVulnerable())
            {
                if (PhotonNetwork.InRoom)
                {
                    player.GetComponent<PhotonView>().RPC(nameof(Player.TakeDamage), RpcTarget.All);
                }
                else
                {
                    player.TakeDamage();
                }
            }
            return;
        }

        // Player2に当たったかチェック
        Player2 player2 = other.gameObject.GetComponent<Player2>();
        if (player2 != null)
        {
            if (player2.IsVulnerable())
            {
                if (PhotonNetwork.InRoom)
                {
                    player2.GetComponent<PhotonView>().RPC(nameof(Player2.TakeDamage), RpcTarget.All);
                }
                else
                {
                    player2.TakeDamage();
                }
            }
        }
    }
}