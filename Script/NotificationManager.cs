using UnityEngine;
using TMPro;
using System.Collections;

public class NotificationManager : MonoBehaviour
{
    public static NotificationManager instance;

    public TextMeshProUGUI notificationText;
    public float displayTime = 2.0f;
    public float fadeOutTime = 1.5f;

    private Coroutine notificationCoroutine;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 他のスクリプトからこの関数を呼び出す
    public void ShowNotification(string message)
    {
        if (notificationCoroutine != null)
        {
            StopCoroutine(notificationCoroutine);
        }
        notificationCoroutine = StartCoroutine(NotificationCoroutine(message));
    }

    private IEnumerator NotificationCoroutine(string message)
    {
        // 1.テキストを表示する
        notificationText.text = message;
        notificationText.color = new Color(notificationText.color.r, notificationText.color.g, notificationText.color.b, 1);
        notificationText.gameObject.SetActive(true);

        // 2.指定された時間だけ待つ
        yield return new WaitForSeconds(displayTime);

        // 3.ゆっくりとフェードアウトさせる
        float alpha = 1.0f;
        while (alpha > 0)
        {
            alpha -= Time.deltaTime / fadeOutTime;
            notificationText.color = new Color(notificationText.color.r, notificationText.color.g, notificationText.color.b, alpha);
            yield return null;
        }

        // 4. 完全に消えたら非表示にする
        notificationText.gameObject.SetActive(false);
    }
}