using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Audio;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public static bool isTutorialMode = false;

    public Image MoneyBar; // 돈 게이지 슬라이드
    //오디오 스피커 변수
    //public AudioSource bgmAudioSource; // 배경음악 담당 스피커
    //public AudioSource sfxAudioSource; // 효과음 담당 스피커, 효과음이 추가되면 이름 변경

    public int CURRENT_TURN = 1;
    public int MAX_TURN = 35; // 최대 턴수
    public int START_MONEY = 100, MAX_MONEY = 100; // 초기자금, 자금 최대치
    public int MIN_AFFINITY = -1; // 민심 데이터 최솟값
    public int MAX_AFFINITY = 3;  // 민심 데이터 최댓값
    public int START_AFFINITY = 0; // 모든 계층 초기 민심
    public float FAIL_RND_MIN = -1.5f;   // 실패 시 민심 감소 최대 마이너스 값
    public float FAIL_RND_MAX = -0.5f;   // 실패 시 민심 감소 최소 마이너스 값

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // 인게임에서만 턴 계산하도록 함
        if (SceneManager.GetActiveScene().name == "GameScene")
        {
            CURRENT_TURN = 1;
            ScoreManager.Instance.InitData();

            // "PlayTutorial" 을 찾아서 읽기
            int doTutorial = PlayerPrefs.GetInt("PlayTutorial", 1);
            // "PlayTutorial"이 1이면 튜토리얼 실행
            if (doTutorial == 1 && TutorialManager.Instance != null)
            {
                TutorialManager.Instance.StartTutorial();
            }
            // "PlayTutorial"이 1이 아니면 바로 게임 실행
            else if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.tutorialPanel.SetActive(false);
                TutorialManager.Instance.isTutorial = false;
            }

        StartTurn(); // 튜토리얼 세팅 완료 후 턴 시작
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))// ESC 키를 눌렀는지 체크
        {
            OptionButton();
        }
    }

    // 턴 시작 시 호출
    public void StartTurn()
    {
        if (CURRENT_TURN > MAX_TURN) // 모든 턴이 끝나면
        {
            ScoreManager.Instance.GameEnding(); // 게임 끝내고 최종 결과 출력
            return;
        }

        if (CURRENT_TURN > 1)
        {
            SuddenEventManager.Instance.CheckAndTriggerEvent(); // 돌발 이벤트 체크 및 발생 함수
        }

        UpdateMoneyUI(); // 돈 게이지 업데이트
        UIManager.Instance.UpdateTurnText(); // 현재 턴 업데이트
        UIManager.Instance.UpdateFundingButtonState(); // 자금에 따른 버튼 상태 갱신 
    }

    // 플레이어가 카드를 선택하여 행동을 마쳤을 때 호출
    public void OnPlayerActionCompleted()
    {
        UpdateMoneyUI(); //UI에 현재 돈 업데이트
        ScoreManager.Instance.CalculateTurnScore(); // 턴점수 계산

        CURRENT_TURN++;
        StartTurn();
    }

    // 돈 게이지, 성공 확률 업데이트
    public void UpdateMoneyUI()
    {
        if (MoneyBar != null)
        {
            MoneyBar.fillAmount = (float)ScoreManager.Instance.money / MAX_MONEY;
        }
        UIManager.Instance.UpdateSuccessProbabilityUI(ScoreManager.Instance.money);
    }

    // 옵션 창의 해상도 조절 기능 함수
    public void SetResolution(int index)
    {
        if (index == 0) Screen.SetResolution(1920, 1080, true); // 전체화면
        else if (index == 1) Screen.SetResolution(1280, 720, false); // 창모드
    }

    // 옵션 창의 소리 조절 기능 함수
    public void SetBGMVolume(float volume)
    {
        PlayerPrefs.SetFloat("BGMVolume", volume);
        
        // 해당 소리를 연결하면 주석 해제
        // if (bgmAudioSource != null) 
        // {
        //     bgmAudioSource.volume = volume;
        // }
    }

    // 여기서부터 씬 컨트롤 하는 버튼 함수들임
    // 메인메뉴로 돌아가기
    public void ReturnMainMenu()
    {
        SceneManager.LoadScene("MainScene");
    }

    // 옵션 창 켜기 끄기
    public void OptionButton()
    {
        if (UIManager.Instance.optionPanel.activeSelf == true) 
        {
            UIManager.Instance.optionPanel.SetActive(false);
        }
        else 
        {
            UIManager.Instance.optionPanel.SetActive(true);
        }
    }
    
    // 뉴스 켜기 끄기 버튼
    public void NewsButton()
    {
        if (UIManager.Instance.newsPanel.activeSelf == true) 
        {
            UIManager.Instance.newsPanel.SetActive(false);
        }
        else 
        {
            UIManager.Instance.newsPanel.SetActive(true);
        }
    }

    // 돌발 이벤트창 끄기 버튼
    public void SuddenEventEndButten()
    {
        UIManager.Instance.eventPanel.SetActive(false);
    }

    // 튜토리얼이 없는 재시작
    public void RestartWithTutorial()
    {
        Time.timeScale = 1f;
        PlayerPrefs.SetInt("PlayTutorial", 1);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // 게임 끝내기
    public void GameEndButton()
    {
        Application.Quit();
    }
}