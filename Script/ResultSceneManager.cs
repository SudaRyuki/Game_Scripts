using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;

public class ResultSceneManager : MonoBehaviourPunCallbacks
{
   
    public TextMeshProUGUI winnerText;
    public static ResultSceneManager Instance;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }


    void Start()
    {
       
        string winner = GameResultData.winnerTeam;

        
        if (winnerText != null && !string.IsNullOrEmpty(winner))
        {
            winnerText.text = winner + " WIN!";
        }
        else
        {
            winnerText.text = "RESULT"; 
        }
    }


    public void GoToTitleScene()
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.Disconnect();
        }
        else
        {
            // もしオフラインなら、すぐにタイトルシーンに移動
            SceneManager.LoadScene("TitleScene");
        }
    }

   
    public override void OnDisconnected(DisconnectCause cause)
    {
        SceneManager.LoadScene("TitleScene");
    }
}