using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;
using System.Collections;

public class SpecialLaser : MonoBehaviourPunCallbacks, IPunObservable 
{
    public int shooterTeamID;
    public float laserDirection = 1f; [Header("ビームの演出設定")]
    public float laserLifeTime = 2.0f;
    public float maxLaserLength = 30f;
    public float extendSpeed = 50f;

    private List<GameObject> hitOpponents = new List<GameObject>();
    private bool isExtending = true;

    // 撃った本人のPCで最初に呼ばれる設定メソッド
    public void SetInitialData(int teamID, float dir)
    {
        this.shooterTeamID = teamID;
        this.laserDirection = dir;
        ApplyDirection(); // すぐに向きを反映
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(shooterTeamID);
            stream.SendNext(laserDirection);
        }
        else
        {
            this.shooterTeamID = (int)stream.ReceiveNext();
            this.laserDirection = (float)stream.ReceiveNext(); 
            ApplyDirection(); 
        }
    }

    private void ApplyDirection()
    {
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * laserDirection;
        transform.localScale = scale;
    }

    void Start()
    {
        if (!PhotonNetwork.InRoom || photonView.IsMine)
        {
            StartCoroutine(DestroyLaserCoroutine(laserLifeTime));
        }
    }

    void Update()
    {
        if (isExtending)
        {
            Vector3 currentScale = transform.localScale;
           
            float sign = laserDirection;
            float newLength = Mathf.Abs(currentScale.x) + (extendSpeed * Time.deltaTime);

            if (newLength >= maxLaserLength)
            {
                newLength = maxLaserLength;
                isExtending = false;
            }

            transform.localScale = new Vector3(newLength * sign, currentScale.y, currentScale.z);
        }
    }

    IEnumerator DestroyLaserCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (PhotonNetwork.InRoom) PhotonNetwork.Destroy(gameObject);
        else Destroy(gameObject);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (PhotonNetwork.InRoom && !photonView.IsMine) return;

        Player player = other.GetComponent<Player>();
        if (player != null && player.teamID != this.shooterTeamID && !hitOpponents.Contains(player.gameObject))
        {
            hitOpponents.Add(player.gameObject);
            if (PhotonNetwork.InRoom) player.GetComponent<PhotonView>().RPC(nameof(Player.TakeDamage), RpcTarget.All);
            else player.TakeDamage();
        }

        Player2 player2 = other.GetComponent<Player2>();
        if (player2 != null && player2.teamID != this.shooterTeamID && !hitOpponents.Contains(player2.gameObject))
        {
            hitOpponents.Add(player2.gameObject);
            if (PhotonNetwork.InRoom) player2.GetComponent<PhotonView>().RPC(nameof(Player2.TakeDamage), RpcTarget.All);
            else player2.TakeDamage();
        }
    }
}