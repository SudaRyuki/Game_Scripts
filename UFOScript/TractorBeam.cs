using UnityEngine;
using Photon.Pun;

public class TractorBeam : MonoBehaviour
{
    public UfoController ufoController;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (ufoController == null) return;

        // 当たり判定UFOのオーナーだけが務める
        if (PhotonNetwork.InRoom && !ufoController.photonView.IsMine) return;

        Player player = other.GetComponent<Player>();
        Player2 player2 = other.GetComponent<Player2>();

        if (player != null)
        {
            // ホストが捕まえたと判断したら
            ufoController.OnPlayerCaptured();
            // 全員のPCにいるこのプレイヤーにUFOについていく命令を送る
            player.GetAbductedByUFO(ufoController.transform);
            
            return;
        }

        if (player2 != null)
        {
            ufoController.OnPlayerCaptured();
            player2.GetAbductedByUFO(ufoController.transform);
            
        }
    }
}