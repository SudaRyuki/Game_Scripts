using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    [Header("Main UI")]
    public TMP_Text statusText;
    public Button online1v1Button;
    public Button online2v2Button; [Header("Selection UI")]
    public GameObject selectionPanel;
    public TMP_InputField roomNameInput;
    public Button randomMatchButton;
    public Button customMatchButton;
    public Button startCustomButton; [Header("Matching UI (Loading)")]
    public GameObject matchingPanel;
    public TMP_Text matchingInfoText;
    public Button cancelButton;

    [Header("Room UI (Lobby)")]
    public GameObject roomPanel;
    public TMP_Text[] playerNameTexts;
    public TMP_Text[] playerTeamTexts;
    public TMP_Text[] playerStatusTexts;
    public Button readyButton;
    public Button teamChangeButton;
    public GameObject dummyIcon1v1;

    [Header("Color Customization")]
    public Image[] playerIconImages;      
    public Button[] colorLeftButtons;     
    public Button[] colorRightButtons;    
    public Material hueMaterialBase;

    private float[] colorHues = { 0f, 30f, 60f, 90f, 120f, 150f, 180f, 210f, 240f, 270f, 300f };

    public static float[] ColorHues ={ 0f, 30f, 60f, 90f, 120f, 150f, 180f, 210f, 240f, 270f, 300f};

    private string selectedGameMode;
    private bool isConnecting = false;
    private bool isCustomMatch = false;
    public static NetworkManager instance;
    public static NetworkManager Instance { get { return instance; } }


    void Start()
    {
        PhotonNetwork.SendRate = 60;
        PhotonNetwork.SerializationRate = 60;
        PhotonNetwork.AutomaticallySyncScene = true;
        UpdateStatus("Ready for Online Battle");

        if (selectionPanel != null) selectionPanel.SetActive(false);
        if (matchingPanel != null) matchingPanel.SetActive(false);
        if (roomPanel != null) roomPanel.SetActive(false);
    }

    // --- メイン画面のボタン ---
    public void OnOnline1v1ButtonClick() { selectedGameMode = "1v1"; OpenSelectionPanel(); }
    public void OnOnline2v2ButtonClick() { selectedGameMode = "2v2"; OpenSelectionPanel(); }

    private void OpenSelectionPanel()
    {
        if (selectionPanel != null) selectionPanel.SetActive(true);
        online1v1Button.interactable = false;
        online2v2Button.interactable = false;
        ResetSelectionUI();
    }

    private void ResetSelectionUI()
    {
        if (randomMatchButton != null) randomMatchButton.gameObject.SetActive(true);
        if (customMatchButton != null) customMatchButton.gameObject.SetActive(true);
        if (roomNameInput != null) roomNameInput.gameObject.SetActive(false);
        if (startCustomButton != null) startCustomButton.gameObject.SetActive(false);
        if (roomNameInput != null) roomNameInput.text = "";
    }

    public void OnRandomMatchClick()
    {
        isCustomMatch = false;
        if (selectionPanel != null) selectionPanel.SetActive(false);
        Connect();
    }

    public void OnCustomModeSelectClick()
    {
        if (randomMatchButton != null) randomMatchButton.gameObject.SetActive(false);
        if (customMatchButton != null) customMatchButton.gameObject.SetActive(false);
        if (roomNameInput != null) roomNameInput.gameObject.SetActive(true);
        if (startCustomButton != null) startCustomButton.gameObject.SetActive(true);
    }

    public void OnStartCustomConnectClick()
    {
        if (string.IsNullOrEmpty(roomNameInput.text)) return;
        isCustomMatch = true;
        if (selectionPanel != null) selectionPanel.SetActive(false);
        Connect();
    }

    public void OnCloseSelectionPanelClick()
    {
        if (selectionPanel != null) selectionPanel.SetActive(false);
        online1v1Button.interactable = true;
        online2v2Button.interactable = true;
    }

    // 接続処理
    private void Connect()
    {
        if (isConnecting) return;
        isConnecting = true;

        if (matchingPanel != null) matchingPanel.SetActive(true);
        UpdateMatchingUI("サーバーに接続中...");

        string myName = PlayerPrefs.GetString("PlayerName", "Player_" + Random.Range(1000, 9999));
        PhotonNetwork.NickName = myName;

        if (PhotonNetwork.IsConnected)
        {
            UpdateStatus("Re-joining Lobby...");
            PhotonNetwork.JoinLobby();
        }
        else
        {
            UpdateStatus("Connecting to Server...");
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public void OnCancelButtonClick()
    {
        isConnecting = false;
        PhotonNetwork.Disconnect();
        if (matchingPanel != null) matchingPanel.SetActive(false);
        if (roomPanel != null) roomPanel.SetActive(false);
        online1v1Button.interactable = true;
        online2v2Button.interactable = true;
    }

    public override void OnConnectedToMaster()
    {
        if (!isConnecting) return;

        UpdateMatchingUI("Connected! Joining Lobby...");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        if (!isConnecting) return;

        if (isCustomMatch)
        {
            UpdateMatchingUI("カスタムルームに入ります！" + roomNameInput.text);
            RoomOptions roomOptions = new RoomOptions();
            roomOptions.MaxPlayers = (selectedGameMode == "1v1") ? (byte)2 : (byte)4;
            roomOptions.CustomRoomProperties = new ExitGames.Client.Photon.Hashtable { { "gm", selectedGameMode } };
            roomOptions.IsVisible = false;
            PhotonNetwork.JoinOrCreateRoom(roomNameInput.text, roomOptions, TypedLobby.Default);
        }
        else
        {
            UpdateMatchingUI("Playerを探しています " + selectedGameMode);
            var expectedCustomRoomProperties = new ExitGames.Client.Photon.Hashtable { { "gm", selectedGameMode } };
            PhotonNetwork.JoinRandomRoom(expectedCustomRoomProperties, 0);
        }
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        UpdateMatchingUI("部屋を立てます！");
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = (selectedGameMode == "1v1") ? (byte)2 : (byte)4;
        roomOptions.CustomRoomProperties = new ExitGames.Client.Photon.Hashtable { { "gm", selectedGameMode } };
        roomOptions.CustomRoomPropertiesForLobby = new string[] { "gm" };
        PhotonNetwork.CreateRoom(null, roomOptions);
    }


    // 待機画面用のロジック

    public override void OnJoinedRoom()
    {
        UpdateMatchingUI("部屋に入室しました！");

        if (matchingPanel != null) matchingPanel.SetActive(false);
        if (roomPanel != null) roomPanel.SetActive(true);

        if (selectedGameMode == "2v2")
        {
            if (teamChangeButton != null) teamChangeButton.gameObject.SetActive(true);
            if (dummyIcon1v1 != null) dummyIcon1v1.SetActive(false);
        }
        else // 1v1の時
        {
            if (teamChangeButton != null) teamChangeButton.gameObject.SetActive(false);
            if (dummyIcon1v1 != null) dummyIcon1v1.SetActive(true);
        }

        ExitGames.Client.Photon.Hashtable initialProps = new ExitGames.Client.Photon.Hashtable();
        initialProps["isReady"] = false;
        initialProps["team"] = (PhotonNetwork.CurrentRoom.PlayerCount % 2 == 1) ? 1 : 2;
        initialProps["color"] = 0; // 色番号0番を初期設定
        PhotonNetwork.LocalPlayer.SetCustomProperties(initialProps);
        UpdateRoomUI();
    }

    public void OnColorLeftClick() { ChangeColor(-1); }
    public void OnColorRightClick() { ChangeColor(1); }


    private void ChangeColor(int direction)
    {
        ExitGames.Client.Photon.Hashtable props = PhotonNetwork.LocalPlayer.CustomProperties;
        int currentColor = 0;
        if (props.ContainsKey("color")) currentColor = (int)props["color"];

        currentColor += direction;

        if (currentColor < 0) currentColor = colorHues.Length - 1;
        if (currentColor >= colorHues.Length) currentColor = 0;

        props["color"] = currentColor;
        props["isReady"] = false; // 色を変えたら準備完了を一度解除する

        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    public void OnReadyButtonClick()
    {
        ExitGames.Client.Photon.Hashtable props = PhotonNetwork.LocalPlayer.CustomProperties;
        bool isReady = false;
        if (props.ContainsKey("isReady")) isReady = (bool)props["isReady"];

        props["isReady"] = !isReady;
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    //「チーム変更」ボタンを押したときに呼ばれる処理
    public void OnTeamChangeButtonClick()
    {
        ExitGames.Client.Photon.Hashtable props = PhotonNetwork.LocalPlayer.CustomProperties;

        int currentTeam = 1;
        if (props.ContainsKey("team")) currentTeam = (int)props["team"];

        // チームを反転させる（1なら2へ、2なら1へ）
        props["team"] = (currentTeam == 1) ? 2 : 1;

        // チームを変えたらReadyを解除する
        props["isReady"] = false;

        // 変更をサーバーに送信
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    public override void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        UpdateRoomUI();
        CheckAllPlayersReady();
    }

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer) { UpdateRoomUI(); }
    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer) { UpdateRoomUI(); }

    private void UpdateRoomUI()
    {
        for (int i = 0; i < playerNameTexts.Length; i++)
        {
            if (playerNameTexts[i] != null) playerNameTexts[i].text = "待機中...";
            if (playerTeamTexts[i] != null) playerTeamTexts[i].text = "";
            if (playerStatusTexts[i] != null) playerStatusTexts[i].text = "";

            if (playerIconImages[i] != null) playerIconImages[i].gameObject.SetActive(false);
            if (colorLeftButtons[i] != null) colorLeftButtons[i].gameObject.SetActive(false);
            if (colorRightButtons[i] != null) colorRightButtons[i].gameObject.SetActive(false);
        }

        Photon.Realtime.Player[] players = PhotonNetwork.PlayerList;

        for (int i = 0; i < players.Length; i++)
        {
            if (i >= playerNameTexts.Length) break;

            if (playerNameTexts[i] != null)
            {
                playerNameTexts[i].text = players[i].NickName;
                if (players[i].IsLocal) playerNameTexts[i].color = Color.yellow;
                else playerNameTexts[i].color = Color.white;
            }

            if (playerIconImages[i] != null) playerIconImages[i].gameObject.SetActive(true);

            int colorId = 0;
            if (players[i].CustomProperties.ContainsKey("color")) colorId = (int)players[i].CustomProperties["color"];

            if (playerIconImages[i] != null)
            {
                playerIconImages[i].gameObject.SetActive(true);
                if (hueMaterialBase != null)
                {
                    // 選んだ色になる
                    Material mat = new Material(hueMaterialBase);
                    mat.SetFloat("_ShiftValue", colorHues[colorId]);
                    playerIconImages[i].material = mat;
                }
            }

            bool isMe = players[i].IsLocal;
            if (colorLeftButtons[i] != null) colorLeftButtons[i].gameObject.SetActive(isMe);
            if (colorRightButtons[i] != null) colorRightButtons[i].gameObject.SetActive(isMe);

            // Ready状態の表示
            bool isReady = false;
            if (players[i].CustomProperties.ContainsKey("isReady"))
            {
                isReady = (bool)players[i].CustomProperties["isReady"];
            }
            if (playerStatusTexts[i] != null)
            {
                if (isReady) playerStatusTexts[i].text = "<color=#00FF00>準備完了！</color>";
                else playerStatusTexts[i].text = "<color=#888888>準備中...</color>";
            }

            // チーム状態の表示
            int team = 1;
            if (players[i].CustomProperties.ContainsKey("team"))
            {
                team = (int)players[i].CustomProperties["team"];
            }
            if (playerTeamTexts[i] != null)
            {
                playerTeamTexts[i].text = "Team " + team;
                // チームごとに色を変えてわかりやすくする
                if (team == 1) playerTeamTexts[i].color = new Color(1f, 0.5f, 0.5f); // 薄い赤
                else playerTeamTexts[i].color = new Color(0.5f, 0.8f, 1f); // 薄い青
            }
        }
    }

    private void CheckAllPlayersReady()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (PhotonNetwork.CurrentRoom.PlayerCount != PhotonNetwork.CurrentRoom.MaxPlayers) return;

        // チームごとの人数を数えるための変数
        int team1Count = 0;
        int team2Count = 0;

        foreach (var p in PhotonNetwork.PlayerList)
        {
            // 一人でも準備中ならここで処理を終える
            if (!p.CustomProperties.ContainsKey("isReady") || !(bool)p.CustomProperties["isReady"])
            {
                return;
            }

            // どのチームにいるかカウントする
            if (p.CustomProperties.ContainsKey("team"))
            {
                int team = (int)p.CustomProperties["team"];
                if (team == 1) team1Count++;
                else if (team == 2) team2Count++;
            }
        }

        // モードごとに、正しい人数に分かれているかチェックする
        if (selectedGameMode == "1v1")
        {
            if (team1Count != 1 || team2Count != 1)
            {
                Debug.Log("チームの人数が偏っているため、スタートできません！(1v1)");
                return; // 人数が合わなければ、ここで処理を止める
            }
        }
        else if (selectedGameMode == "2v2")
        {
            if (team1Count != 2 || team2Count != 2)
            {
                Debug.Log("チームの人数が偏っているため、スタートできません！(2v2)");
                return; // 人数が合わなければ、ここで処理を止める
            }
        }
        
        
        Debug.Log("全員準備完了！ゲームを開始します！");
        PhotonNetwork.CurrentRoom.IsOpen = false;
        Invoke(nameof(LoadGameScene), 1f);
    }

    private void LoadGameScene() { PhotonNetwork.LoadLevel("GameScene"); }

    public override void OnDisconnected(DisconnectCause cause)
    {
        isConnecting = false;
        if (matchingPanel != null) matchingPanel.SetActive(false);
        if (roomPanel != null) roomPanel.SetActive(false);
        online1v1Button.interactable = true;
        online2v2Button.interactable = true;
    }

    private void UpdateStatus(string message) { Debug.Log(message); }
    private void UpdateMatchingUI(string message) { if (matchingInfoText != null) matchingInfoText.text = message; Debug.Log(message); }

    // 待機画面で「戻る」ボタンを押した時に呼ばれる
    public void OnLeaveRoomButtonClick()
    {
        // もし部屋の中にいるなら、退出の命令を出す
        if (PhotonNetwork.InRoom)
        {
            UpdateMatchingUI("部屋から退出中...");
            // サーバーから切断するのではなく、部屋だけを抜ける
            PhotonNetwork.LeaveRoom();
        }
    }

    // 部屋から完全に抜け終わった後に、Photonから自動で呼ばれる
    public override void OnLeftRoom()
    {
        Debug.Log("部屋を退出しました。");

        if (roomPanel != null) roomPanel.SetActive(false);
        if (matchingPanel != null) matchingPanel.SetActive(false);

        isConnecting = false;

        if (online1v1Button != null) online1v1Button.interactable = true;
        if (online2v2Button != null) online2v2Button.interactable = true;

        PhotonNetwork.LocalPlayer.CustomProperties.Clear();
    }
}