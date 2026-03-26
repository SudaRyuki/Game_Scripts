using UnityEngine;
using Photon.Pun; 

public class ScreenWrapper : MonoBehaviour
{
    private PhotonView photonView; 
    private float screenMinX;
    private float screenMaxX;
    private float objectWidth;

    void Start()
    {
        photonView = GetComponent<PhotonView>();

        Camera mainCamera = Camera.main;
        screenMinX = mainCamera.ViewportToWorldPoint(new Vector3(0, 0, 0)).x;
        screenMaxX = mainCamera.ViewportToWorldPoint(new Vector3(1, 0, 0)).x;

        SpriteRenderer spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            objectWidth = spriteRenderer.bounds.size.x;
        }
    }

    void Update()
    {
        if (photonView != null && !photonView.IsMine && PhotonNetwork.InRoom)
        {
            return;
        }

        Vector3 currentPosition = transform.position;
        bool wrapped = false; 

        if (currentPosition.x > screenMaxX + (objectWidth / 2))
        {
            currentPosition.x = screenMinX - (objectWidth / 2);
            wrapped = true;
        }
        else if (currentPosition.x < screenMinX - (objectWidth / 2))
        {
            currentPosition.x = screenMaxX + (objectWidth / 2);
            wrapped = true;
        }

        if (wrapped)
        {
           
            transform.position = currentPosition;

            
            if (photonView != null && PhotonNetwork.InRoom)
            {
                photonView.RPC(nameof(RpcWarp), RpcTarget.Others, currentPosition);
            }
        }
    }

    
    [PunRPC]
    private void RpcWarp(Vector3 newPosition)
    {
       
        transform.position = newPosition;
    }
}