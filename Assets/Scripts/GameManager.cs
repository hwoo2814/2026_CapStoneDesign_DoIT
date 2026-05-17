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
    public static bool isTutorialMode = false;

    public Image MoneyBar; // 돈 게이지 슬라이드

    //오디오 스피커 변수
    public AudioSource bgmAudioSource1, bgmAudioSource2, bgmAudioSource3, bgmAudioSource4; // 배경음악 4개
    private Coroutine bgmRandomPlayCoroutine; // 중복으로 배경음악 코루틴이 실행되는 것을 막기 위해 사용
    private AudioSource currentBgmAudioSource; // 현재 어떤 배경음악이 재생 중인지 확인하기 위해 사용
    public AudioSource endingAudioSource; // 엔딩 배경음악
    public AudioSource questSuccessAudioSource; // 퀘스트 성공 음악
    public AudioSource questFailedAudioSource; // 퀘스트 실패 음악
    public AudioSource clickAudioSource; // 모든 버튼, 정책 클릭시 나는 사운드

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
            StartRandomBGM(); // 배경음악 재생
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

        // 현재 튜토리얼 진행 중인지 확인하는 변수
        bool isTutorialRunning = TutorialManager.Instance != null && TutorialManager.Instance.isTutorial;

        // 튜토리얼 중이 아닐 때만 돌발 이벤트를 체크함
        if (CURRENT_TURN > 1 && !isTutorialRunning)
        {
            if (SuddenEventManager.Instance != null)
            {
                SuddenEventManager.Instance.CheckAndTriggerEvent(); // 돌발 이벤트 체크 및 발생 함수
            }
        }

        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnTurnStart(CURRENT_TURN);
        }

        bool isQuestGenerationTurn = System.Array.IndexOf(new int[] { 7, 14, 21, 28 }, CURRENT_TURN) >= 0;
        if (isQuestGenerationTurn && !(TutorialManager.Instance != null && TutorialManager.Instance.isTutorial))
        {
            UIManager.Instance.ShowLogPanelForced(0.5f);
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
        clickAudioSource.PlayOneShot(clickAudioSource.clip);

        UIManager.Instance.HideExplainPolicyPanel();

        // 정책횟수 카운트
        if (OllamaFinalEvaluationClient.Instance != null)
        {
            OllamaFinalEvaluationClient.Instance.RecordPolicySelection(cardController.pendingPolicyType);
        }

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
        clickAudioSource.PlayOneShot(clickAudioSource.clip);

        cardController.pendingPolicyType = -1;
        cardController.pendingPolicyCost = 0f;
        UIManager.Instance.HideExplainPolicyPanel();
    }
    
    // 뉴스 브렌치 켜기 끄기 버튼
    public void NewsBranchButton()
    {
        if (UIManager.Instance.newsBranchPanel.activeSelf == true) 
        {
            clickAudioSource.PlayOneShot(clickAudioSource.clip);
            UIManager.Instance.newsBranchPanel.SetActive(false);
        }
        else 
        {
            clickAudioSource.PlayOneShot(clickAudioSource.clip);
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
            clickAudioSource.PlayOneShot(clickAudioSource.clip);
            UIManager.Instance.questPanel.SetActive(false);
        }
        else 
        {
            clickAudioSource.PlayOneShot(clickAudioSource.clip);
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
            clickAudioSource.PlayOneShot(clickAudioSource.clip);
            UIManager.Instance.newsPanel.SetActive(false);
        }
        else 
        {
            clickAudioSource.PlayOneShot(clickAudioSource.clip);
            UIManager.Instance.newsBranchPanel.SetActive(false);
            UIManager.Instance.newsPanel.SetActive(true);

            // 뉴스 버튼 클릭시 가져온 퀘스트 알람 이미지를 원래대롣 되돌림
            if (UIManager.Instance.newsButtonImage != null && UIManager.Instance.newsDefaultSprite != null)
            {
                bool hasActiveQuest = QuestManager.Instance != null && QuestManager.Instance.HasActiveQuest();
                UIManager.Instance.newsButtonImage.sprite = hasActiveQuest && UIManager.Instance.newsQuestAlertSprite != null
                    ? UIManager.Instance.newsQuestAlertSprite
                    : UIManager.Instance.newsDefaultSprite;
            }
        }
    }

    // 퀘스트 수락 버튼
    public void AcceptButten()
    {
        clickAudioSource.PlayOneShot(clickAudioSource.clip);

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
        clickAudioSource.PlayOneShot(clickAudioSource.clip);

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
        clickAudioSource.PlayOneShot(clickAudioSource.clip);
        UIManager.Instance.eventPanel.SetActive(false);
    }

    // 옵션 창 켜기 끄기
    public void OptionButton()
    {
        if (UIManager.Instance.optionPanel.activeSelf == true) 
        {
            clickAudioSource.PlayOneShot(clickAudioSource.clip);
            UIManager.Instance.optionPanel.SetActive(false);
        }
        else 
        {
            clickAudioSource.PlayOneShot(clickAudioSource.clip);
            UIManager.Instance.optionPanel.SetActive(true);
        }
    }

    // HUD의 로그 버튼 클릭 시 호출되는 함수
    // 로그 패널을 열기만 하며, 튜토리얼 중이라면 TutorialManager에 알림
    public void LogButton()
    {
        clickAudioSource.PlayOneShot(clickAudioSource.clip);
        UIManager.Instance.OpenLogPanel();

        if (TutorialManager.Instance != null && TutorialManager.Instance.isTutorial)
            TutorialManager.Instance.OnLogBtnClicked();
    }

    // 로그 패널 닫기 버튼 클릭 시 호출되는 함수
    public void LogCloseButton()
    {
        clickAudioSource.PlayOneShot(clickAudioSource.clip);
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
        PlayerPrefs.SetFloat("BGMVolume", volume); // 현재 재생 중인 BGM이 있다면 즉시 볼륨을 반영 
        PlayerPrefs.Save();
            if (currentBgmAudioSource != null) 
            { 
                currentBgmAudioSource.volume = volume; 
            } 
            // 4개의 BGM AudioSource 모두 같은 볼륨으로 맞춤 // 다음에 랜덤으로 재생될 BGM에도 옵션 볼륨이 적용되도록 하기 위함 
            if (bgmAudioSource1 != null) bgmAudioSource1.volume = volume; 
            if (bgmAudioSource2 != null) bgmAudioSource2.volume = volume; 
            if (bgmAudioSource3 != null) bgmAudioSource3.volume = volume; 
            if (bgmAudioSource4 != null) bgmAudioSource4.volume = volume; 
    }

    // 랜덤 BGM 재생을 시작하는 함수
    // 이미 코루틴이 실행 중이면 중복 실행 방지
    public void StartRandomBGM()
    {
        if (bgmRandomPlayCoroutine != null) return;
        bgmRandomPlayCoroutine = StartCoroutine(RandomBGMCoroutine());
    }

    // 랜덤 BGM 재생을 정지하는 함수
    // 실행 중인 코루틴 중지
    public void StopRandomBGM()
    {
        if (bgmRandomPlayCoroutine != null)
        {
            StopCoroutine(bgmRandomPlayCoroutine);
            bgmRandomPlayCoroutine = null;
        }

        StopAllBGM();
    }

    // 랜덤 BGM을 반복 재생하는 코루틴
    // AudioSource 중 하나를 랜덤 선택
    // 기존 BGM 정지 후 새 BGM 재생
    // 저장된 볼륨 적용
    // 현재 음악이 끝날 때까지 대기 후 반복
    private IEnumerator RandomBGMCoroutine()
    {
        while (true)
        {
            AudioSource selectedBgm = GetRandomBGMAudioSource();

            if (selectedBgm == null)
            {
                yield return null;
                continue;
            }

            StopAllBGM();

            currentBgmAudioSource = selectedBgm;

            float savedVolume = PlayerPrefs.GetFloat("BGMVolume", currentBgmAudioSource.volume);
            currentBgmAudioSource.volume = savedVolume;

            currentBgmAudioSource.Play();

            while (currentBgmAudioSource != null && currentBgmAudioSource.isPlaying)
            {
                yield return null;
            }
        }
    }

    // 랜덤으로 BGM AudioSource를 선택하는 함수
    // null이 아닌 AudioSource만 리스트에 추가
    // 리스트에서 무작위 선택
    // 사용 가능한 AudioSource가 없으면 null 반환
    private AudioSource GetRandomBGMAudioSource()
    {
        List<AudioSource> bgmList = new List<AudioSource>();

        if (bgmAudioSource1 != null) bgmList.Add(bgmAudioSource1);
        if (bgmAudioSource2 != null) bgmList.Add(bgmAudioSource2);
        if (bgmAudioSource3 != null) bgmList.Add(bgmAudioSource3);
        if (bgmAudioSource4 != null) bgmList.Add(bgmAudioSource4);

        if (bgmList.Count == 0) return null;

        int randomIndex = UnityEngine.Random.Range(0, bgmList.Count);
        return bgmList[randomIndex];
    }

    // 엔딩 패널이 활성화될 때 호출되는 함수 
    // 기존 랜덤 BGM 코루틴과 현재 재생 중인 BGM을 모두 정지한 뒤 엔딩 음악을 재생함
    public void PlayEndingAudio() 
    { 
        StopRandomBGM(); 
        if (endingAudioSource == null) return; 

        float savedVolume = PlayerPrefs.GetFloat("BGMVolume", endingAudioSource.volume); 	
        endingAudioSource.volume = savedVolume; 
        
        if (!endingAudioSource.isPlaying) 
        { 
            endingAudioSource.Play(); 
        } 
    }

    // 모든 BGM을 정지하는 함수
    // 여러 AudioSource가 동시에 재생되는 것을 방지
    private void StopAllBGM()
    {
        if (bgmAudioSource1 != null) bgmAudioSource1.Stop();
        if (bgmAudioSource2 != null) bgmAudioSource2.Stop();
        if (bgmAudioSource3 != null) bgmAudioSource3.Stop();
        if (bgmAudioSource4 != null) bgmAudioSource4.Stop();
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

        // 현재 게임 BGM 코루틴과 재생 중인 일반 BGM 정지
        StopRandomBGM();

        // 엔딩 음악이 재생 중일 수도 있으므로 별도로 정지
        if (endingAudioSource != null)
        {
            endingAudioSource.Stop();
        }
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
