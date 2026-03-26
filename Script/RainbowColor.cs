using UnityEngine;
using UnityEngine.UI;

public class RainbowColor : MonoBehaviour
{
    private Image image; [Header("色の変化スピード")]
    public float colorChangeSpeed = 1.0f;

    private float hue = 0f; // 色相（0〜1の間で変化）

    void Awake()
    {
        image = GetComponent<Image>();
    }

    void Update()
    {
        if (image == null) return;

        hue += Time.deltaTime * colorChangeSpeed;

        if (hue > 1f)
        {
            hue = 0f;
        }
        //ゲージが虹色になる設定
        image.color = Color.HSVToRGB(hue, 1f, 1f);
    }
}