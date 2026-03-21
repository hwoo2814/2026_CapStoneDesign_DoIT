using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public static bool isTutorialMode = false;
    public Slider MoneyBar; // 돈 게이지 슬라이드

    public int CURRENT_TURN = 1;
    public int MAX_TURN = 35; // 최대 턴수
    public int START_MONEY = 100, MAX_MONEY = 100; // 초기자금, 자금 최대치
    public int MIN_AFFINITY = -1; // 민심 데이터 최솟값
    public int MAX_AFFINITY = 3;  // 민심 데이터 최댓값
    public int START_AFFINITY = 0; // 모든 계층 초기 민심
    
    public float FAIL_RND_MIN = -0.5f;   // 실패 시 민심 감소 최소 범위
    public float FAIL_RND_MAX = -1.5f;   // 실패 시 민심 감소 최대 범위

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // 인게임 씬(GameScene)에서만 턴 계산하도록 함
        if (SceneManager.GetActiveScene().name == "GameScene")
        {
            CURRENT_TURN = 1;
            ScoreManager.Instance.InitData(); // 초기 데이터 세팅
            StartTurn();
        }

        // 튜토리얼인지 게임 바로 시작인지 체크
        int doTutorial = PlayerPrefs.GetInt("PlayTutorial", 1); // 1 = 튜토리얼 켜기, 0 = 바로 시작, 기본값은 1
        if (doTutorial == 1 && TutorialManager.Instance != null)
        {
            TutorialManager.Instance.StartTutorial(); // 튜토리얼 시작함ㄴ
        }
        else if (TutorialManager.Instance != null)
        {
            // '바로 시작'을 선택했다면 튜토리얼 패널을 끔
            TutorialManager.Instance.tutorialPanel.SetActive(false);
            TutorialManager.Instance.isTutorial = false;
        }
    }

    // 턴 시작 시 호출
    public void StartTurn()
    {
        if (CURRENT_TURN > MAX_TURN) // 모든 턴이 끝나면
        {
            ScoreManager.Instance.GameEnding(); // 게임 끝내고 최종 결과 출격
            return;
        }

        if (CURRENT_TURN > 1)
        {
            SuddenEventManager.Instance.CheckAndTriggerEvent(); // 돌발 이벤트 체크 및 발생 함수
        }

        UpdateMoneyUI(); // 돈 게이지 업데이트
        UIManager.Instance.UpdateTurnText(); // 현재 턴 업데이트
    }

    // 플레이어가 카드를 선택하여 행동을 마쳤을 때 호출
    public void OnPlayerActionCompleted()
    {
        UpdateMoneyUI(); //UI에 현재 돈 업데이트
        ScoreManager.Instance.CalculateTurnScore(); // 턴점수 계산
        SuddenEventManager.Instance.CheckAndTriggerEvent(); //돌발 이벤트 체크

        CURRENT_TURN++;
        StartTurn();
    }

    // 돈 게이지 슬라이더 업데이트
    public void UpdateMoneyUI()
    {
        if (MoneyBar != null)
        {
            MoneyBar.value = (float)ScoreManager.Instance.money / MAX_MONEY;
        }
    }

    // 여기서부터 씬 컨트롤을 하는 버튼 함수들임
    // '튜토리얼하기' 버튼을 눌렀을 때
    public void PlayWithTutorial()
    {
        PlayerPrefs.SetInt("PlayTutorial", 1); // 메모장에 1(한다) 적어두기
        SceneManager.LoadScene("GameScene");
    }

    // '바로 시작' 버튼을 눌렀을 때
    public void GameStart()
    {
        PlayerPrefs.SetInt("PlayTutorial", 0); // 메모장에 0(안한다) 적어두기
        SceneManager.LoadScene("GameScene");
    }

    // 튜토리얼 스타트
    public void TutorialStart()
    {
        isTutorialMode = true;
        SceneManager.LoadScene("GameScene");
    }

    // 메인메뉴로 돌아가기
    public void ReturnMainMenu()
    {
        SceneManager.LoadScene("MainScene");
    }

    // 옵션
    public void Option()
    {
        SceneManager.LoadScene("OptionScene");
    }

    // 게임 끝내기
    public void ExitButton()
    {
        Application.Quit();
    }
}