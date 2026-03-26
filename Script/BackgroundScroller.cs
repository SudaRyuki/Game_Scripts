using UnityEngine;

public class BackgroundScroller : MonoBehaviour
{
    [Header("雲が流れる速さ")]
    public float speed = 1.0f;

    [Header("リセットする位置（左端）")]
    public float deadZone = -20f;

    [Header("再配置する位置（右端）")]
    public float startPosition = 20f;

    void Update()
    {
        // 左へ移動させる
        transform.Translate(Vector3.left * speed * Time.deltaTime);

        // もし左端を越えたら、右端に戻す
        if (transform.position.x < deadZone)
        {
            Vector3 newPos = transform.position;
            newPos.x = startPosition;
            transform.position = newPos;
        }
    }
}