using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    public GameObject startPopupPanel; // 튜토리얼인지 바로시작인지 물어보는 팝업창

    void Start()
    {
        // 씬이 시작될 때 팝업창은 화면에서 끔
        if (startPopupPanel != null)
            startPopupPanel.SetActive(false);
    }

    // 1. 메인 화면의 Game Start 버튼을 누르면 팝업창을 띄우는 함수
    public void OpenStartPopup()
    {
        startPopupPanel.SetActive(true);
    }

    // 2. 팝업창의 튜토리얼 버튼을 눌렀을 때 실행될 함수
    public void PlayWithTutorial()
    {
        // PlayTutorial에 1을 적고 게임 씬으로
        PlayerPrefs.SetInt("PlayTutorial", 1); 
        SceneManager.LoadScene("GameScene"); 
    }

    // 3. 팝업창의 바로 시작 버튼을 눌렀을 때 실행될 함수
    public void PlayWithoutTutorial()
    {
        // PlayTutorial에 0을 적고 게임 씬으로
        PlayerPrefs.SetInt("PlayTutorial", 0); 
        SceneManager.LoadScene("GameScene");
    }

    // 게임 끄기 버튼
    public void GameEndButton()
    {
        Application.Quit();
    }
}