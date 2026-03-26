using UnityEngine;
using LootLocker.Requests;
using TMPro;

public class LootLockerManager : MonoBehaviour
{
    public static LootLockerManager instance;

    [Header("UI設定")]
    public GameObject nameInputPanel;
    public TMP_InputField nameInputField;
    public GameObject titleButtonsGroup;
    public TMP_Text errorText;

    [Header("ランキング設定")]
    public string leaderboardKey = "wins";
    public GameObject leaderboardPanel;
    public TMP_Text[] rankingTextSlots;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // シーン移動で消えないようにする
        }
        else
        {
            // タイトルに戻った際、新しいUIをインスタンスに紐付け直す
            instance.nameInputPanel = this.nameInputPanel;
            instance.nameInputField = this.nameInputField;
            instance.titleButtonsGroup = this.titleButtonsGroup;
            instance.errorText = this.errorText;
            instance.leaderboardPanel = this.leaderboardPanel;
            instance.rankingTextSlots = this.rankingTextSlots;

            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        if (instance != this) return;

        if (errorText != null) errorText.text = "";

        string savedName = PlayerPrefs.GetString("PlayerName", "");
        if (!string.IsNullOrEmpty(savedName)) ShowTitleButtons();
        else
        {
            if (nameInputPanel != null) nameInputPanel.SetActive(true);
            if (titleButtonsGroup != null) titleButtonsGroup.SetActive(false);
        }

        Login();
    }

    void Login()
    {
        LootLockerSDKManager.StartGuestSession((response) => {
            if (response.success) Debug.Log("LootLockerログイン成功！");
            else Debug.LogError("ログイン失敗");
        });
    }

    public void SubmitNameButton()
    {
        string inputName = nameInputField.text;
        if (inputName.Length < 3)
        {
            if (errorText != null) { errorText.gameObject.SetActive(true); errorText.text = "Name must be at least 3 characters."; }
            return;
        }
        LootLockerSDKManager.SetPlayerName(inputName, (response) => {
            if (response.success) { PlayerPrefs.SetString("PlayerName", inputName); ShowTitleButtons(); }
        });
    }

    //合計勝利数
    public void SubmitScore(int score)
    {
        LootLockerSDKManager.SubmitScore("", score, leaderboardKey, (response) => {
            if (response.success) Debug.Log("サーバーへスコア送信成功！ 現在の合計勝利数: " + score);
            else Debug.LogError("スコア送信失敗");
        });
    }

    public void ShowLeaderboard()
    {
        if (leaderboardPanel != null) leaderboardPanel.SetActive(true);
        foreach (var slot in rankingTextSlots) { if (slot != null) slot.text = "---"; }

        LootLockerSDKManager.GetScoreList(leaderboardKey, 10, 0, (response) =>
        {
            if (response.success && response.items != null && response.items.Length > 0)
            {
                // データがある場合は通常通り表示
                LootLockerLeaderboardMember[] items = response.items;
                for (int i = 0; i < rankingTextSlots.Length; i++)
                {
                    if (rankingTextSlots[i] == null) continue;
                    if (i < items.Length)
                    {
                        var member = items[i];
                        string n = string.IsNullOrEmpty(member.player.name) ? "Unknown" : member.player.name;

                        string displayText = n + "  (" + member.score + ")";
                        string myName = PlayerPrefs.GetString("PlayerName", "");

                        if (n == myName)
                        {
                            rankingTextSlots[i].text = "<b><color=#FFD700>" + displayText + " (YOU)</color></b>";
                        }
                        else
                        {
                            rankingTextSlots[i].text = displayText;
                        }
                    }
                    else
                    {
                        rankingTextSlots[i].text = "---";
                    }
                }
            }
            else
            {
                //ランキング部分
                Debug.Log("ランキングデータが0件、または取得できませんでした（リセット直後など）");
                if (rankingTextSlots.Length > 0 && rankingTextSlots[0] != null)
                {
                    rankingTextSlots[0].text = "No Rankings Yet";
                }
            }
        });
    }
    void ShowTitleButtons()
    {
        if (nameInputPanel != null) nameInputPanel.SetActive(false);
        if (titleButtonsGroup != null) titleButtonsGroup.SetActive(true);
    }
}