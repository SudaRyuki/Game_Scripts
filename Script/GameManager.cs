using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using Photon.Pun;
using TMPro;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public static bool IsGameStarted = false;

    #region ゲーム設定・UI参照[Header("Input Settings")]
    public InputActionAsset inputActionAsset;

    [Header("UI設定")]
    public GameObject countdownPanel;
    public TMP_Text countdownText; [Header("チーム1 Prefabの名前 (Resourcesフォルダ内)")]
    public string player1PrefabName;
    public string player3PrefabName; [Header("チーム2 Prefabの名前 (Resourcesフォルダ内)")]
    public string player2PrefabName;
    public string player4PrefabName;

    [Header("ギミック Prefabs")]
    public GameObject returnUfoPrefab;

    [Header("生成位置")]
    public Transform[] team1SpawnPoints;
    public Transform[] team2SpawnPoints;

    private int winScore;
    #endregion

    #region Unity Lifecycle (Awake, Start)
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        Time.timeScale = 1f;
        IsGameStarted = false; // 最初は動けないようにする
    }

    void Start()
    {
        string gameMode = "1v1";

        if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null)
        {
            // オンライン：ルームから設定を読み取る
            gameMode = (string)PhotonNetwork.CurrentRoom.CustomProperties["gm"];
        }
        else
        {
            // オフライン：保存データから読み取る
            gameMode = PlayerPrefs.GetString("GameMode", "1v1");

            // オフラインの時だけここで生成（オンラインはPlayerSpawnerなどが担当）
            if (gameMode == "1v1") Setup1v1_Offline();
            else Setup2v2_Offline();
        }

        // 勝利スコアの設定
        winScore = (gameMode == "1v1") ? 10 : 10;

        // スコアマネージャーに勝利条件を伝える
        if (ScoreManager.instance != null)
        {
            ScoreManager.instance.SetWinScore(winScore);
        }

        // カウントダウン開始
        StartCoroutine(StartGameSequence());
    }
    #endregion

    #region ゲーム進行 (カウントダウン)
    private IEnumerator StartGameSequence()
    {
        if (countdownPanel == null || countdownText == null) yield break;

        countdownPanel.SetActive(true);
        Image panelImage = countdownPanel.GetComponent<Image>();
        if (panelImage != null) panelImage.enabled = true; // 最初は背景を表示

        // 1. ルールの表示
        countdownText.text = $"First to {winScore} points wins!";
        yield return new WaitForSeconds(2.0f);

        // 2. カウントダウン
        string[] countdownSteps = { "3", "2", "1", "GO!" };
        foreach (string step in countdownSteps)
        {
            countdownText.text = step;
            yield return new WaitForSeconds(1.0f);
        }

        // 3. スタート！
        IsGameStarted = true;
        if (panelImage != null) panelImage.enabled = false;

        yield return new WaitForSeconds(1.0f);
        countdownPanel.SetActive(false);
    }
    #endregion

    #region オフラインプレイヤー生成処理
    private void Setup1v1_Offline()
    {
        Debug.Log("オフライン 1v1モードを開始します");

        // --- P1 (Team 1, WASD) ---
        SpawnPlayer1(player1PrefabName, "Keyboard_WASD", Keyboard.current, team1SpawnPoints[0].position, 1);

        // --- P2 (Team 2, IJKL) ---
        SpawnPlayer2(player2PrefabName, "Keyboard_IJKL", Keyboard.current, team2SpawnPoints[0].position, 2);
    }

    private void Setup2v2_Offline()
    {
        Debug.Log("オフライン 2v2モードを開始します");

        // --- Team 1 ---
        SpawnPlayer1(player1PrefabName, "Keyboard_WASD", Keyboard.current, team1SpawnPoints[0].position, 1); // P1
        SpawnPlayer1(player3PrefabName, "Keyboard_IJKL", Keyboard.current, team1SpawnPoints[1].position, 1); // P3

        // --- Team 2 ---
        if (Gamepad.all.Count > 0)
        {
            SpawnPlayer2(player2PrefabName, "Gamepad", Gamepad.all[0], team2SpawnPoints[0].position, 2); // P2
        }
        else Debug.LogError("P2用のゲームパッドがありません！");

        if (Gamepad.all.Count > 1)
        {
            SpawnPlayer2(player4PrefabName, "Gamepad", Gamepad.all[1], team2SpawnPoints[1].position, 2); // P4
        }
        else Debug.LogError("P4用のゲームパッドがありません！(2台目)");
    }

    // --- 生成用ヘルパーメソッド ---
    private void SpawnPlayer1(string prefabName, string controlScheme, InputDevice device, Vector3 spawnPos, int teamID)
    {
        var actions = Instantiate(inputActionAsset);
        actions.bindingMask = InputBinding.MaskByGroup(controlScheme);

        GameObject prefab = Resources.Load<GameObject>(prefabName);
        if (prefab == null) { Debug.LogError($"{prefabName} がResourcesフォルダに見つかりません！"); return; }

        GameObject go = Instantiate(prefab, spawnPos, Quaternion.identity);
        Player player = go.GetComponent<Player>();
        player.Initialize(actions, device);
        player.teamID = teamID;
    }

    private void SpawnPlayer2(string prefabName, string controlScheme, InputDevice device, Vector3 spawnPos, int teamID)
    {
        var actions = Instantiate(inputActionAsset);
        actions.bindingMask = InputBinding.MaskByGroup(controlScheme);

        GameObject prefab = Resources.Load<GameObject>(prefabName);
        if (prefab == null) { Debug.LogError($"{prefabName} がResourcesフォルダに見つかりません！"); return; }

        GameObject go = Instantiate(prefab, spawnPos, Quaternion.identity);
        Player2 player = go.GetComponent<Player2>();
        player.Initialize(actions, device);
        player.teamID = teamID;
    }
    #endregion

    #region UFO帰還処理 (Player1 / Player2)
    
    public void HandlePlayerReturn(Player playerToReturn, float delay)
    {
        StartCoroutine(RespawnPlayerCoroutine(playerToReturn, delay));
    }

    public void HandlePlayerReturn(Player2 playerToReturn, float delay)
    {
        StartCoroutine(RespawnPlayerCoroutine(playerToReturn, delay));
    }

    private IEnumerator RespawnPlayerCoroutine(Player playerToReturn, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (PhotonNetwork.InRoom && !PhotonNetwork.IsMasterClient) yield break;

        Vector3 spawnPos = new Vector3(-5f, 8f, 0);

        if (PhotonNetwork.InRoom)
        {
            GameObject ufo = PhotonNetwork.Instantiate("ReturnUFO", spawnPos, Quaternion.identity);
            int ufoID = ufo.GetComponent<PhotonView>().ViewID;
            playerToReturn.photonView.RPC("RpcBeginReturn", RpcTarget.All, ufoID);
        }
        else
        {
            GameObject ufo = Instantiate(returnUfoPrefab, spawnPos, Quaternion.identity);
            playerToReturn.StartReturnSequence(ufo.transform);
        }
    }

    private IEnumerator RespawnPlayerCoroutine(Player2 playerToReturn, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (PhotonNetwork.InRoom && !PhotonNetwork.IsMasterClient) yield break;

        Vector3 spawnPos = new Vector3(-5f, 8f, 0);

        if (PhotonNetwork.InRoom)
        {
            GameObject ufo = PhotonNetwork.Instantiate("ReturnUFO", spawnPos, Quaternion.identity);
            int ufoID = ufo.GetComponent<PhotonView>().ViewID;
            playerToReturn.photonView.RPC("RpcBeginReturn", RpcTarget.All, ufoID);
        }
        else
        {
            GameObject ufo = Instantiate(returnUfoPrefab, spawnPos, Quaternion.identity);
            playerToReturn.StartReturnSequence(ufo.transform);
        }
    }
    #endregion
}