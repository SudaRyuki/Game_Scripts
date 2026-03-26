using UnityEngine;
using Photon.Pun;

public class AlienBeam : MonoBehaviour
{
    public int shooterTeamID;

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(shooterTeamID);
        }
        else
        {
            this.shooterTeamID = (int)stream.ReceiveNext();
        }
    }
   
    [PunRPC]
    public void SetTeamID(int id)
    {
        this.shooterTeamID = id;
    }

    void Start()
    {
        // 5秒後に自動で消える
        if (GetComponent<PhotonView>().IsMine)
        {
            Destroy(gameObject, 5f);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PhotonView pv = GetComponent<PhotonView>();

        // 当たり判定を実行するべきか、最初に判断する
        bool shouldProcessCollision = false;
        if (PhotonNetwork.InRoom) // オンラインの場合
        {
            // 自分が撃った弾なら、当たり判定を行う
            if (pv != null && pv.IsMine)
            {
                shouldProcessCollision = true;
            }
        }
        else // オフラインの場合
        {
            // オフラインなら、常に当たり判定を行う
            shouldProcessCollision = true;
        }

        if (!shouldProcessCollision)
        {
            return;
        }

        // オフライン、またはオンラインのオーナーだけが実行する

        // Playerに当たった場合
        Player player = other.GetComponent<Player>();
        if (player != null && player.teamID != this.shooterTeamID)
        {
            // オフラインなら直接、オンラインならRPCでTakeDamageを呼ぶ
            if (PhotonNetwork.InRoom)
            {
                player.GetComponent<PhotonView>().RPC(nameof(Player.TakeDamage), RpcTarget.All);
            }
            else
            {
                player.TakeDamage();
            }

            // オフラインなら直接、オンラインならネットワーク越しにビームを破壊
            if (PhotonNetwork.InRoom) PhotonNetwork.Destroy(gameObject);
            else Destroy(gameObject);

            return; 
        }

        // Player2に当たった場合
        Player2 player2 = other.GetComponent<Player2>();
        if (player2 != null && player2.teamID != this.shooterTeamID)
        {
            if (PhotonNetwork.InRoom)
            {
                player2.GetComponent<PhotonView>().RPC(nameof(Player2.TakeDamage), RpcTarget.All);
            }
            else
            {
                player2.TakeDamage();
            }

            if (PhotonNetwork.InRoom) PhotonNetwork.Destroy(gameObject);
            else Destroy(gameObject);
        }
    }
}