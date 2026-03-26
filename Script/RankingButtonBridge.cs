using UnityEngine;

public class RankingButtonBridge : MonoBehaviour
{
    // ランキングボタンからこのメソッドを呼び出すように設定
    public void OnClickRanking()
    {
        if (LootLockerManager.instance != null)
        {
            LootLockerManager.instance.ShowLeaderboard();
        }
    }
}