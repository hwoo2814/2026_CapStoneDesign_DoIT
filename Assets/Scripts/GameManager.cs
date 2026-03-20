using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
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
            SuddenEventManager.Instance.CheckAndTriggerEvent();
        }

        UpdateMoneyUI(); // 돈 게이지 업데이트
        UIManager.Instance.UpdateTurnText(); // 현재 턴 업데이트
    }

    // 플레이어가 카드를 선택해 행동을 마쳤을 때 호출
    public void OnPlayerActionCompleted()
    {
        UpdateMoneyUI();
        ScoreManager.Instance.CalculateTurnScore();
        SuddenEventManager.Instance.CheckAndTriggerEvent();

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

    // 여기서부터 씬 컨트롤 함수들임
    public void GameStart()
    {
        SceneManager.LoadScene("GameScene");
    }

    public void ReturnMainMenu()
    {
        SceneManager.LoadScene("MainScene");
    }

    public void Option()
    {
        SceneManager.LoadScene("OptionScene");
    }

    public void ExitButton()
    {
        Application.Quit();
    }
}