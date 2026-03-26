using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using Photon.Pun;


public class ScoreManager : MonoBehaviour
{
    [Header("Audio")]
    public AudioClip scoreItemSound;
    private AudioSource audioSource;

    public static ScoreManager instance;
    public TextMeshProUGUI playerScoreText;
    public TextMeshProUGUI player2ScoreText;
    private PhotonView photonView;
    private int playerScore = 0;
    private int player2Score = 0;

    private int scoreToWin;

    void Awake()
    {
        if (instance == null)
            instance = this;
        photonView = GetComponent<PhotonView>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    
    void Start()
    {
        // ゲームが始まった瞬間にUIを正しい表記に更新する
        playerScoreText.text = "Team1:"+playerScore;
        player2ScoreText.text = "Team2:"+player2Score;
    }

    public void SetWinScore(int score)
    {
        scoreToWin = score;
        Debug.Log($"勝利スコアが {scoreToWin} に設定されました。");
    }

    public void AddScore(string playerName, int amount)
    {
        //オンラインならRPC経由で、オフラインなら直接スコアを変更
        if (PhotonNetwork.InRoom)
        {
            photonView.RPC(nameof(RpcAddScore), RpcTarget.All, playerName, amount);
        }
        else
        {
            RpcAddScore(playerName, amount);
        }
    }

    [PunRPC]
    private void RpcAddScore(string playerName, int amount)
    {
        if (amount > 0 && audioSource != null && scoreItemSound != null)
        {
            audioSource.PlayOneShot(scoreItemSound);
        }

        if (playerName == "Player")
        {
            playerScore = Mathf.Max(0, playerScore + amount);
            playerScoreText.text = "Team1:" + playerScore;

            // 勝利判定
            if (playerScore >= scoreToWin)
            {
                WinGame("Team 1");
            }
        }
        else if (playerName == "Player2")
        {
            player2Score = Mathf.Max(0, player2Score + amount);
            player2ScoreText.text = "Team2:" + player2Score;

            if (player2Score >= scoreToWin)
            {
                WinGame("Team 2");
            }
        }
    }

    private void WinGame(string winnerTeamName)
    {
        Debug.Log($"{winnerTeamName} の勝利！");
        GameResultData.winnerTeam = winnerTeamName;

        int myTeamID = 0;
        Player myP = FindObjectOfType<Player>();
        Player2 myP2 = FindObjectOfType<Player2>();

        if (myP != null && myP.GetComponent<Photon.Pun.PhotonView>().IsMine) myTeamID = myP.teamID;
        else if (myP2 != null && myP2.GetComponent<Photon.Pun.PhotonView>().IsMine) myTeamID = myP2.teamID;

        if ((winnerTeamName == "Team 1" && myTeamID == 1) || (winnerTeamName == "Team 2" && myTeamID == 2))
        {
            if (LootLockerManager.instance != null)
            {
                int totalWins = PlayerPrefs.GetInt("TotalWinsCount", 0);

                totalWins++;

                PlayerPrefs.SetInt("TotalWinsCount", totalWins);
                // 勝利の合計をサーバーに送る
                LootLockerManager.instance.SubmitScore(totalWins);

            }
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene("ResultScene");
    }

    public int GetScore(string playerName)
    {
        if (playerName == "Player") return playerScore;
        else if (playerName == "Player2") return player2Score;
        return 0;
    }
}