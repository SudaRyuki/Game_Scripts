using UnityEngine;
using UnityEngine.UI;

public class CooldownGauge : MonoBehaviour
{
    public Vector3 offset = new Vector3(0, 1.5f, 0);

    private Slider slider;
    private Transform targetToFollow;
    private float cooldownTimer;
    private float cooldownDuration;

    void Awake()
    {
        slider = GetComponent<Slider>();
    }

    void Update()
    {
        if (targetToFollow != null)
        {
            //プレイヤーの頭上に表示する
            transform.position = Camera.main.WorldToScreenPoint(targetToFollow.position + offset);

            if (cooldownTimer > 0)
            {
                cooldownTimer -= Time.deltaTime;
                slider.value = cooldownTimer / cooldownDuration;
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }

    public void StartCooldown(Transform target, float duration)
    {
        targetToFollow = target;
        cooldownDuration = duration;
        cooldownTimer = duration;
        slider.value = 1;
        gameObject.SetActive(true);
    }
}