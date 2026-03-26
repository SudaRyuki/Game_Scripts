using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerItemCollector1 : MonoBehaviour
{
    public string playerName = "Player2"; // Player or Player2

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Item"))
        {
            Destroy(collision.gameObject);

            // スコアを加算
            ScoreManager.instance.AddScore(playerName, 1);

            // 内部スコアを取得
            int currentScore = ScoreManager.instance.GetScore(playerName);

            if (currentScore >= 10)
            {
                Debug.Log($"{playerName} wins!");
                SceneManager.LoadScene("ResultScene");
            }
        }
    }
}
