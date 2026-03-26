using UnityEngine;
using UnityEngine.UI;

public class SpecialGauge : MonoBehaviour
{
    // Playerの頭上に表示
    public Vector3 offset = new Vector3(0, 2.0f, 0);

    private Slider slider;
    private Transform targetToFollow;

    void Awake()
    {
        slider = GetComponent<Slider>();
    }

    void Update()
    {
        if (targetToFollow != null)
        {
            // キャラクターを追いかける
            transform.position = Camera.main.WorldToScreenPoint(targetToFollow.position + offset);
        }
        else
        {
            // キャラクターがいなくなったら自分も消える
            Destroy(gameObject);
        }
    }

    public void Initialize(Transform target)
    {
        targetToFollow = target;
        slider.value = 0f; // 最初はポイントゼロ
    }

    // 攻撃を当ててポイントが増えるたびに呼ばれる
    public void UpdateGauge(int currentPoints, int maxPoints)
    {
        if (slider != null)
        {
            slider.value = (float)currentPoints / maxPoints;
        }
    }
}