using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class TitleManager : MonoBehaviour
{
    public GameObject[] instructionPages;

    private int currentPageIndex = 0;

    [Header("Warning UI")]
    public GameObject warningPanel; 
    public TMPro.TMP_Text warningText; 

    void Start()
    {
        foreach (var page in instructionPages)
        {
            if (page != null) page.SetActive(false);
        }
    }

    // 最初のページを表示する関数
    public void ShowInstructions()
    {
        // 最初のページを表示
        currentPageIndex = 0;
        if (instructionPages.Length > 0 && instructionPages[currentPageIndex] != null)
        {
            instructionPages[currentPageIndex].SetActive(true);
        }
    }

    public void HideAllInstructions()
    {
        foreach (var page in instructionPages)
        {
            if (page != null) page.SetActive(false);
        }
    }

    // 次のページを表示する関数
    public void GoToNextPage()
    {
        // 現在のページを非表示にする
        if (instructionPages[currentPageIndex] != null)
        {
            instructionPages[currentPageIndex].SetActive(false);
        }

        // 次のページへ
        if (currentPageIndex < instructionPages.Length - 1)
        {
            currentPageIndex++;
        }

        // 新しいページを表示する
        if (instructionPages[currentPageIndex] != null)
        {
            instructionPages[currentPageIndex].SetActive(true);
        }
    }

    // 前のページを表示する関数
    public void GoToPrevPage()
    {
        // 現在のページを非表示にする
        if (instructionPages[currentPageIndex] != null)
        {
            instructionPages[currentPageIndex].SetActive(false);
        }

        // 前のページへ
        if (currentPageIndex > 0)
        {
            currentPageIndex--;
        }

        // 新しいページを表示する
        if (instructionPages[currentPageIndex] != null)
        {
            instructionPages[currentPageIndex].SetActive(true);
        }
    }

    public void CloseWarningPanel()
    {
        if (warningPanel != null) warningPanel.SetActive(false);
    }

    public void On1v1ButtonClick()
    {
        PlayerPrefs.SetString("GameMode", "1v1");
        SceneManager.LoadScene("GameScene");
    }

    public void On2v2ButtonClick()
    {
        if (Gamepad.all.Count < 2)
        {
            if (warningPanel != null)
            {
                warningPanel.SetActive(true);
              
            }
            return;
        }

        PlayerPrefs.SetString("GameMode", "2v2");
        SceneManager.LoadScene("GameScene");
    }
    public void OnQuitButtonClick()
    {
        // Unityエディタで再生している場合
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        // ビルドされたゲームの場合
#else
            Application.Quit();
#endif
    }
}