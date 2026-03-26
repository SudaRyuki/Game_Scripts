using UnityEngine;
using System.Collections;
using Photon.Pun;

public class UfoController : MonoBehaviourPunCallbacks
{
    public GameObject tractorBeamObject;
    public Transform beamSpawnPoint;
    public float moveSpeed = 3f;
    public float escapeSpeed = 5f;
    public float beamAttemptTime = 2.0f;

    public AudioClip beamSound;
    private AudioSource audioSource;
    private Rigidbody2D rb;

    private bool hasFired = false;
    private bool playerCaptured = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();

        if (tractorBeamObject != null)
        {
            tractorBeamObject.GetComponent<TractorBeam>().ufoController = this;
            tractorBeamObject.SetActive(false);
        }

        if (rb != null) rb.linearVelocity = new Vector2(moveSpeed, 0);
    }

    void Update()
    {
        if (PhotonNetwork.InRoom && !photonView.IsMine) return;

        if (!hasFired && Mathf.Abs(transform.position.x) < 0.5f)
        {
            hasFired = true;

            if (PhotonNetwork.InRoom)
            {
                photonView.RPC(nameof(RpcStartBeamSequence), RpcTarget.All);
            }
            else
            {
                StartCoroutine(BeamSequenceCoroutine());
            }
        }
    }

    [PunRPC]
    private void RpcStartBeamSequence()
    {
        StartCoroutine(BeamSequenceCoroutine());
    }

    private IEnumerator BeamSequenceCoroutine()
    {
        if (rb != null) rb.linearVelocity = Vector2.zero;

        if (tractorBeamObject != null)
        {
            tractorBeamObject.transform.position = beamSpawnPoint.position;
            tractorBeamObject.SetActive(true);
            if (audioSource != null && beamSound != null)
            {
                audioSource.PlayOneShot(beamSound);
            }
        }

        yield return new WaitForSeconds(beamAttemptTime);

        if (!playerCaptured)
        {
            if (tractorBeamObject != null) tractorBeamObject.SetActive(false);
            if (rb != null) rb.linearVelocity = new Vector2(moveSpeed, 0);
        }
    }

    public void OnPlayerCaptured()
    {
        if (PhotonNetwork.InRoom && !photonView.IsMine) return;
        if (playerCaptured) return;
        playerCaptured = true;

        if (PhotonNetwork.InRoom)
        {
            photonView.RPC(nameof(RpcStartAbductionFlight), RpcTarget.All);
        }
        else
        {
            if (tractorBeamObject != null)
            {
                tractorBeamObject.SetActive(false);
            }
          
            StartCoroutine(AbductionFlightCoroutine());
        }
    }

    [PunRPC]
    private void RpcStartAbductionFlight()
    {
        playerCaptured = true;

        // オンライン時はこのRPCを通じて全員の画面でビームが消える
        if (tractorBeamObject != null)
        {
            tractorBeamObject.SetActive(false);
        }

        StartCoroutine(AbductionFlightCoroutine());
    }

    private IEnumerator AbductionFlightCoroutine()
    {
        if (rb != null) rb.linearVelocity = new Vector2(0, escapeSpeed);

        yield return new WaitForSeconds(3.0f);

        if (tractorBeamObject != null) tractorBeamObject.SetActive(false);

        if (PhotonNetwork.InRoom)
        {
            if (photonView.IsMine) PhotonNetwork.Destroy(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}