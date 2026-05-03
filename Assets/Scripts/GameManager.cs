using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;
using UnityEngine.InputSystem;
using UnityEngine.Audio;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public CardController cardController; //explainPolicyPanel의 "예"/"아니요" 버튼 처리를 위해
                                          //CardController의 pending 데이터와 Execute 함수에 접근할 참조 변수.
    public QuestManager questManager;
    public static bool isTutorialMode = false;

    public Image MoneyBar; // 돈 게이지 슬라이드
    //오디오 스피커 변수
    //public AudioSource bgmAudioSource; // 배경음악 담당 스피커
    //public AudioSource sfxAudioSource; // 효과음 담당 스피커, 효과음이 추가되면 이름 변경

    public int CURRENT_TURN = 1;
    public int MAX_TURN = 35; // 최대 턴수
    public int START_MONEY = 100, MAX_MONEY = 100; // 초기자금, 자금 최대치
    public int MIN_AFFINITY = 0; // 민심 데이터 최솟값
    public int MAX_AFFINITY = 10;  // 민심 데이터 최댓값
    public int START_AFFINITY = 5; // 모든 계층 초기 민심
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
            if (UIManager.Instance.emailBtn != null)
            {
                UIManager.Instance.emailBtn.SetActive(false);
            }

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

        if (QuestManager.Instance != null)
            {
                    QuestManager.Instance.InitData();
            }

        StartTurn(); // 튜토리얼 세팅 완료 후 턴 시작
        }
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            OptionButton();
        }
    }

    // 새로운 턴 시작 시 호출
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

        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnTurnStart(CURRENT_TURN);
        }

        bool isQuestGenerationTurn = System.Array.IndexOf(new int[] { 7, 14, 21, 28 }, CURRENT_TURN) >= 0;
        if (isQuestGenerationTurn && !(TutorialManager.Instance != null && TutorialManager.Instance.isTutorial))
        {
            UIManager.Instance.ShowLogPanelForced(4f);
        }

        UIManager.Instance.UpdateMoneyUI(); // 돈 게이지 업데이트
        UIManager.Instance.UpdateTurnText(); // 현재 턴 업데이트
        UIManager.Instance.UpdateFundingButtonState(); // 자금에 따른 버튼 상태 갱신
        UIManager.Instance.UpdateAffinityUI(); // 민심 게이지 업데이트
        UIManager.Instance.UpdateRegionImages(); // 지역 이미지 업데이트
        UIManager.Instance.UpdateTotalScoreUI(ScoreManager.Instance.totalScore); // 계산된 총점수를 UI에 업데이트
    }

    // 플레이어가 카드를 선택하여 행동을 마쳤을 때 호출하여 턴 넘김
    public void OnPlayerActionCompleted()
    {
        if (CURRENT_TURN == 20)
        {
            ScoreManager.Instance.CheckDeactivationAtTurn20();
        }
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnTurnEnd();
        }

        ScoreManager.Instance.CalculateTurnScore();

        CURRENT_TURN++;
        StartTurn();
    }

    // 정책 패널의 "예" 버튼
    // 실행 순서
    // 1. 패널을 닫음
    // 2. pendingPolicyType이 0(자금확보)이면 ExecuteFunding() 호출,
    //    1~3(정책)이면 ExecutePendingPolicy() 호출하여 실제 정책 실행
    // 3. pending 데이터를 -1, 0으로 초기화하여 중복 실행 방지
    // 튜토리얼 중일 때는 CheckSuccess()가 isTutorial을 감지해 무조건 성공 반환
    public void OnClickConfirmPolicy()
    {
        UIManager.Instance.HideExplainPolicyPanel();

        if (cardController.pendingPolicyType == 0)
        {
            cardController.ExecuteFunding();
        }
        else
        {
            cardController.ProcessPolicy(cardController.pendingPolicyCost, cardController.pendingPolicyType);
        }

        cardController.pendingPolicyType = -1;
        cardController.pendingPolicyCost = 0f;

        // 튜토리얼 중이라면 "예" 버튼 클릭 시 TutorialManager에 알려 다음 대사로 진행
        if (TutorialManager.Instance != null && TutorialManager.Instance.isTutorial)
            TutorialManager.Instance.OnYesBtnClicked();
    }

    // 정책 패널의 "아니요" 버튼
    // 실행 순서
    // 1. pending 데이터를 -1, 0으로 초기화하여 선택을 취소
    // 2. 패널을 닫음
    // 정책이 실행되지 않으므로 턴이 넘어가지 않음
    public void OnClickCancelPolicy()
    {
        cardController.pendingPolicyType = -1;
        cardController.pendingPolicyCost = 0f;
        UIManager.Instance.HideExplainPolicyPanel();
    }

    // 뉴스 브렌치 켜기 끄기 버튼
    public void NewsBranchButton()
    {
        if (UIManager.Instance.newsBranchPanel.activeSelf == true)
        {
            UIManager.Instance.newsBranchPanel.SetActive(false);
        }
        else
        {
            UIManager.Instance.newsBranchPanel.SetActive(true);

            // 튜토리얼 중 뉴스 버튼으로 패널을 열었을 때 다음 대사로 진행
            if (TutorialManager.Instance != null && TutorialManager.Instance.isTutorial)
                TutorialManager.Instance.OnNewsButtonClicked();

        }
    }

    // 퀘스트(이메일) 페널 켜기
    public void EmaillButton()
    {
        if (UIManager.Instance.questPanel.activeSelf == true)
        {
            UIManager.Instance.questPanel.SetActive(false);
        }
        else
        {
            UIManager.Instance.newsBranchPanel.SetActive(false);
            UIManager.Instance.questPanel.SetActive(true);

            // 튜토리얼 중 이메일 버튼으로 퀘스트 패널을 열었을 때 다음 대사로 진행
            if (TutorialManager.Instance != null && TutorialManager.Instance.isTutorial)
                TutorialManager.Instance.OnEmailBtnClicked();
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
            UIManager.Instance.newsBranchPanel.SetActive(false);
            UIManager.Instance.newsPanel.SetActive(true);
        }
    }

    // 퀘스트 수락 버튼
    public void AcceptButten()
    {
        UIManager.Instance.ResultProposal.text = "본 제안서를 채택하겠습니다.";

        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestAccepted();
        }

        UIManager.Instance.acceptBtn.SetActive(false);
        UIManager.Instance.refuseBtn.SetActive(false);

        if (UIManager.Instance.newsButtonImage != null && UIManager.Instance.newsDefaultSprite != null)
        {
            UIManager.Instance.newsButtonImage.sprite = UIManager.Instance.newsDefaultSprite;
        }
    }

    // 퀘스트 거절 버튼
    public void RefuseButten()
    {
        UIManager.Instance.ResultProposal.text = "제시하신 제안서는 채택이 어려울거 같습니다.";

        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestRefused();
        }

        UIManager.Instance.acceptBtn.SetActive(false);
        UIManager.Instance.refuseBtn.SetActive(false);

        if (UIManager.Instance.newsButtonImage != null && UIManager.Instance.newsDefaultSprite != null)
        {
            UIManager.Instance.newsButtonImage.sprite = UIManager.Instance.newsDefaultSprite;
        }
    }

    // 돌발 이벤트창 끄기 버튼
    public void SuddenEventEndButten()
    {
        UIManager.Instance.eventPanel.SetActive(false);
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

    // HUD의 로그 버튼 클릭 시 호출되는 함수
    // 로그 패널을 열기만 하며, 튜토리얼 중이라면 TutorialManager에 알림
    public void LogButton()
    {
        UIManager.Instance.OpenLogPanel();

        if (TutorialManager.Instance != null && TutorialManager.Instance.isTutorial)
            TutorialManager.Instance.OnLogBtnClicked();
    }

    // 로그 패널 닫기 버튼 클릭 시 호출되는 함수
    public void LogCloseButton()
    {
        UIManager.Instance.CloseLogPanel();
    }


    // 옵션 창의 해상도 조절 기능 함수
    public void SetResolution(int index)
    {
        if (index == 0) Screen.SetResolution(1920, 1080, true); // 전체화면
        else if (index == 1) Screen.SetResolution(1280, 720, false); // 창모드
    }

    // 옵션 창의 배경소리 조절 기능 함수
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

    // 튜토리얼이 없는 재시작
    public void RestartWithoutTutorial()
    {
        Time.timeScale = 1f;
        PlayerPrefs.SetInt("PlayTutorial", 0);
        PlayerPrefs.Save();
        SceneManager.LoadScene("GameScene");
    }

    // 게임 끝내기
    public void GameEndButton()
    {
        Application.Quit();
    }
}