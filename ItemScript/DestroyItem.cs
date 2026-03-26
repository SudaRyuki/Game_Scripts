using UnityEngine;
using UnityEngine.SceneManagement;

public class AutoDestroyByTime : MonoBehaviour
{
    [Tooltip("このオブジェクトが自動的に消滅するまでの時間")]
    public float lifetime = 5.0f; // オブジェクトが残る時間

    void Start()
    {
        Destroy(gameObject, lifetime);
    }
    

}