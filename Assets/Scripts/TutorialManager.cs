using UnityEngine;
using UnityEngine.UI;

[System.Serializable] public class HighlightTargetGroup 
{ 
    // 해당 대사에서 동시에 밝게 강조할 UI 오브젝트 배열 
    public GameObject[] targets; 
}

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    public bool isTutorial = false; // 튜토리얼 상태
    private int currentStep = 0; // 튜토리얼 대사 순선를 한번씩 카운팅

    public GameObject tutorialPanel; // 화면 전체를 덮는 투명 패널
    public Image characterImage;  // 비서 캐릭터 이미지
    public Text dialogText; // 대사 텍스트
    public GameObject dialogBox; // 대사창 배경

    private Image tutorialPanelImage; // 버튼 클릭 단계에서 tutorialPanel의 raycastTarget을 끄기 위해

    // 캐릭터 이미지와 대사창의 위치 정보를 담을 배열입니다.
    // Dialogues 배열과 크기가 같아야 합니다.
    // characterPositions, dialogBoxPositions, dialogues, highlightTargets
    // 이 4개의 배열을 크기가 같아야 에러가 나지 않습니다.
    public Vector2[] characterPositions; // 각 스텝별 캐릭터 위치
    public Vector2[] dialogBoxPositions; // 각 스텝별 대사창 위치

    [TextArea(1, 10)] // 한 대사에 최소 줄수 1 ~ 최대 줄수 5줄까지 입력할수 있음. 자유롭게 수정하여 사용.
    public string[] dialogues;

    public HighlightTargetGroup[] highlightTargets; // 대사가 넘어갈 때마다 밝게 강조할 UI 오브젝트를 넣을 배열
    private readonly System.Collections.Generic.List<GameObject> currentHighlightTargets = 
    new System.Collections.Generic.List<GameObject>(); // 현재 강조 중인 UI 오브젝트들을 기억하는 리스트

    public Button youthPolicyBtn; // 청년 정책 (이것만 누르게 함)
    public Button seniorPolicyBtn; // 노년 정책 (튜토리얼 중 잠금)
    public Button corpPolicyBtn; // 기업 정책 (튜토리얼 중 잠금)
    public Button fundingBtn; // 자금 확보 (튜토리얼 중 잠금)
    public Button yesBtn; // ExplainPolicyPanel의 "예" 버튼
    public Button noBtn; // ExplainPolicyPanel의 "아니요" 버튼
    public Button newsButton; // NewsBranchPanel을 여는 뉴스 버튼
    public Button emailBtn; // NewsBranchPanel 안의 이메일(퀘스트) 버튼
    public Button logBtn; // 로그 열기/닫기 버튼 오브젝트

    public int buttonClickStep = 8; // 청년 정책 버튼을 누르라고 지시하는 대사의 순번
    public int yesBtnClickStep = 9; // 정책의 "예" 버튼을 누르라고 지시하는 대사의 순번
    public int newsButtonClickStep = 12; // 뉴스 버튼을 누르라고 지시하는 대사의 순번
    public int emailBtnClickStep = 14; // 이메일 버튼을 누르라고 지시하는 대사의 순번
    public int logBtnClickStep = 16; // 로그 버튼 클릭을 유도하는 대사의 순번
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // 게임 매니저가 게임 시작 시 호출할 튜토리얼 시작 함수
    public void StartTutorial()
    {
        isTutorial = true;
        currentStep = 0;

        // 튜토리얼 패널 켜기
        tutorialPanel.SetActive(true);
        LockAllTutorialButtons();

        if (dialogues == null || dialogues.Length == 0)
        {
            EndTutorial();
            return;
        }

        ShowNextDialogue();
    }

    // 화면(튜토리얼 패널)을 클릭할 때마다 실행됨
    public void OnClickScreen()
    {
        GameManager.Instance.clickAudioSource.PlayOneShot(GameManager.Instance.clickAudioSource.clip);
        if (!isTutorial) return;

        if (IsWaitingForTutorialButton())
            return;

        AdvanceTutorialStep();
    }

    // 현재 단계가 특정 버튼 클릭을 기다리는 단계인지 확인하는 함수
    private bool IsWaitingForTutorialButton()
    {
        return currentStep == buttonClickStep ||
               currentStep == yesBtnClickStep ||
               currentStep == newsButtonClickStep ||
               currentStep == emailBtnClickStep ||
               currentStep == logBtnClickStep;
    }

    // 튜토리얼 단계를 하나 진행시키는 공통 함수
    private void AdvanceTutorialStep()
    {
        currentStep++;

        if (dialogues != null && currentStep < dialogues.Length)
        {
            ShowNextDialogue();
        }
        else
        {
            EndTutorial();
        }
    }

    // tutorialPanel의 raycastTarget을 제어하는 함수
    private void SetTutorialPanelRaycast(bool value)
    {
        if (tutorialPanel == null) return;

        if (tutorialPanelImage == null)
            tutorialPanelImage = tutorialPanel.GetComponent<Image>();

        if (tutorialPanelImage != null)
            tutorialPanelImage.raycastTarget = value;
    }

    // 버튼 interactable을 바꾸는 함수
    private void SetButtonInteractable(Button button, bool value)
    {
        if (button != null)
            button.interactable = value;
    }

    // 튜토리얼 관련 버튼을 모두 잠그는 함수
    // 매 단계마다 먼저 전부 잠그고, 필요한 버튼만 다시 활성화하는 구조
    private void LockAllTutorialButtons()
    {
        SetButtonInteractable(youthPolicyBtn, false);
        SetButtonInteractable(seniorPolicyBtn, false);
        SetButtonInteractable(corpPolicyBtn, false);
        SetButtonInteractable(fundingBtn, false);

        SetButtonInteractable(yesBtn, false);
        SetButtonInteractable(noBtn, false);

        SetButtonInteractable(newsButton, false);
        SetButtonInteractable(emailBtn, false);
        SetButtonInteractable(logBtn, false);
    }

    // 현재 튜토리얼 단계에 맞게 버튼 활성화와 tutorialPanel 클릭 차단 상태를 갱신하는 핵심 함수
    private void UpdateTutorialInteractionState()
    {
        if (!isTutorial) return;

        // 먼저 모든 버튼을 잠금
        LockAllTutorialButtons();

        // 현재 단계가 버튼 클릭을 기다리는 단계인지 확인
        bool isButtonStep = IsWaitingForTutorialButton();

        // 일반 대사 단계에서는 tutorialPanel이 클릭을 받아야 함
        // 버튼 클릭 단계에서는 tutorialPanel이 클릭을 막으면 안 되므로 raycastTarget을 false로 설정
        SetTutorialPanelRaycast(!isButtonStep);

        // 이메일 버튼은 GameManager.Start()에서 SetActive(false) 되므로 튜토리얼 중 필요한 구간에서 다시 켜야 함
        UpdateTutorialEmailButtonVisibility();

        // 청년 정책 버튼 클릭 유도 단계
        if (currentStep == buttonClickStep)
        {
            SetButtonInteractable(youthPolicyBtn, true);
            return;
        }

        // 정책 설명 패널의 "예" 버튼 클릭 유도 단계
        if (currentStep == yesBtnClickStep)
        {
            SetButtonInteractable(yesBtn, true);

            // "아니요"를 누르면 튜토리얼 흐름이 꼬일 수 있으므로 이 단계에서는 잠금
            SetButtonInteractable(noBtn, false);
            return;
        }

        // 뉴스 버튼 클릭 유도 단계
        if (currentStep == newsButtonClickStep)
        {
            SetButtonInteractable(newsButton, true);
            return;
        }

        // 이메일 버튼 클릭 유도 단계
        if (currentStep == emailBtnClickStep)
        {
            if (UIManager.Instance != null && UIManager.Instance.emailBtn != null)
                UIManager.Instance.emailBtn.SetActive(true);

            SetButtonInteractable(emailBtn, true);
            return;
        }

        // 로그 버튼 클릭 유도 단계
        if (currentStep == logBtnClickStep)
        {
            SetButtonInteractable(logBtn, true);
            return;
        }
    }

    // 튜토리얼 중 이메일 버튼 표시 여부를 제어하는 함수
    // 실제 퀘스트가 없어도 튜토리얼 설명을 위해 이메일 버튼을 임시로 보여줌
    private void UpdateTutorialEmailButtonVisibility()
    {
        if (UIManager.Instance == null) return;
        if (UIManager.Instance.emailBtn == null) return;

        // 뉴스 버튼을 누른 다음 단계부터 이메일 버튼을 누르는 단계까지 이메일 버튼 표시
        bool shouldShowEmailButton =
            currentStep >= newsButtonClickStep + 1 &&
            currentStep <= emailBtnClickStep;

        UIManager.Instance.emailBtn.SetActive(shouldShowEmailButton);
    }

    // 설명 출력하는 함수
    private void ShowNextDialogue()
    {
        CloseTutorialRelatedPanels();
        dialogText.text = dialogues[currentStep];

        UpdateUIPositions();
        ResetHighlight();
        UpdateTutorialInteractionState();

        // 이번 대사 순번에 맞게 강조해야 할 타겟 UI가 있다면 어두운 배경 앞으로 끌어옴
        if (highlightTargets != null &&highlightTargets.Length > currentStep && highlightTargets[currentStep] != null)
        {
            HighlightTargetGroup group = highlightTargets[currentStep];

            if (group.targets != null)
            {
                for (int i = 0; i < group.targets.Length; i++)
                {
                    GameObject target = group.targets[i];

                    if (target == null)
                        continue;

                    SetHighlight(target);

                    if (!currentHighlightTargets.Contains(target))
                    {
                        currentHighlightTargets.Add(target);
                    }
                }
            }
        }
    }

    // RectTransform의 anchoredPosition을 이용해 UI 위치를 옮기는 함수
    private void UpdateUIPositions()
    {
        if (characterPositions != null && characterPositions.Length > currentStep)
        {
            characterImage.rectTransform.anchoredPosition = characterPositions[currentStep];
        }

        if (dialogBoxPositions != null && dialogBoxPositions.Length > currentStep)
        {
            dialogBox.GetComponent<RectTransform>().anchoredPosition = dialogBoxPositions[currentStep];
        }
    }

    // 특정 UI가 tutorialPanel을 뚫고 맨 앞으로 보이게 해주는 함수
    private void SetHighlight(GameObject target)
    {
        // 타겟에 Canvas 컴포넌트가 없으면 임시로 붙임
        Canvas canvas = target.GetComponent<Canvas>();
        if (canvas == null) canvas = target.AddComponent<Canvas>();

        // 타겟에 GraphicRaycaster가 없으면 임시로 붙임 (버튼 클릭을 위해 필요)
        GraphicRaycaster raycaster = target.GetComponent<GraphicRaycaster>();
        if (raycaster == null) raycaster = target.AddComponent<GraphicRaycaster>();

        // 렌더링 순서를 tutorialPanel 보다 높게(100) 설정하여 맨 앞으로 튀어나오게 함
        canvas.overrideSorting = true;
        canvas.sortingOrder = 100;
    }

    // 맨 앞으로 튀어나왔던 UI를 다시 원래 자리로 돌려놓는 함수
    private void ResetHighlight() 
    { 
        for (int i = 0; i < currentHighlightTargets.Count; i++) 
        { 
            GameObject target = currentHighlightTargets[i]; 
            if (target == null) continue; ResetSingleHighlight(target); 
        } 
        currentHighlightTargets.Clear(); 
    }

    // 특정 UI 오브젝트 하나의 Highlight 상태를 해제하는 함수
    // SetHighlight()에서 변경한 Canvas.overrideSorting과 sortingOrder 값을 원래 상태로 되돌림
    private void ResetSingleHighlight(GameObject target)
    {
        if (target == null)
            return;

        Canvas canvas = target.GetComponent<Canvas>();

        if (canvas != null)
        {
            // 강제로 높였던 렌더링 순서 설정을 해제
            canvas.overrideSorting = false;
            canvas.sortingOrder = 0;
        }
    }

    // 카드 컨트롤러에서 청년 버튼을 눌렀을 때 호출되어 다음 대사로 진행하는 함수
    public void OnYouthPolicyClicked()
    {
        if (!isTutorial) return;

        SetButtonInteractable(youthPolicyBtn, false);

        AdvanceTutorialStep();
    }

    // "예" 버튼이 눌려 정책이 실행된 직후 다음 대사로 진행시키는 함수
    //ExplainPolicyPanel의 "예" 버튼을 누른 직후 GameManager에서 호출.
    //"아니요" 버튼 잠금 해제 후 다음 대사로 넘어간다.
    public void OnYesBtnClicked()
    {
        if (!isTutorial) return;

        SetButtonInteractable(yesBtn, false);
        SetButtonInteractable(noBtn, false);

        AdvanceTutorialStep();
    }


    // GameManager.NewsBranchButton() 에서 튜토리얼 중 호출
    // NewsBranchPanel이 열린 직후 다음 대사(이메일 버튼 안내)로 진행
    // 뉴스 버튼을 눌러 NewsBranchPanel이 열린 직후 GameManager에서 호출.
    // 뉴스 버튼을 다시 잠그고 다음 대사로 넘어간다.
    public void OnNewsButtonClicked()
    {
        if (!isTutorial) return;

        SetButtonInteractable(newsButton, false);

        AdvanceTutorialStep();
    }

    // GameManager.EmaillButton() 에서 튜토리얼 중 호출
    // QuestPanel이 열린 직후 다음 대사(퀘스트 패널 설명)로 진행
    // 이메일 버튼을 눌러 QuestPanel이 열린 직후 GameManager에서 호출.
    // 이메일 버튼을 다시 잠그고 다음 대사로 넘어간다.
    public void OnEmailBtnClicked()
    {
        if (!isTutorial) return;

        SetButtonInteractable(emailBtn, false);

        AdvanceTutorialStep();
    }

    // 튜토리얼 중 로그 버튼이 눌렸을 때 GameManager.LogButton()에서 호출
    // 로그 버튼을 다시 잠그고 로그 패널 설명 대사로 진행
    public void OnLogBtnClicked()
    {
        if (!isTutorial) return;

        SetButtonInteractable(logBtn, false);

        AdvanceTutorialStep();
    }

    // UI 패널을 끄는 함수
    private void CloseTutorialRelatedPanels() 
    {
        if (UIManager.Instance.explainPolicyPanel != null && UIManager.Instance.explainPolicyPanel.activeSelf != false && currentStep == 10) 
            UIManager.Instance.explainPolicyPanel.SetActive(false);
        
        if (UIManager.Instance.newsBranchPanel != null && UIManager.Instance.newsBranchPanel.activeSelf != false && currentStep == 15) 
            UIManager.Instance.newsBranchPanel.SetActive(false); 
        
        if (UIManager.Instance.questPanel != null && UIManager.Instance.questPanel.activeSelf != false && currentStep == 16) 
            UIManager.Instance.questPanel.SetActive(false); 
    }

    // 뉴스/퀘스트 튜토리얼 섹션 진입 전처리 함수
    // 실제 퀘스트가 없어도 이메일 버튼을 임시 활성화하여 데모 시연이 가능하게 함
    private void PrepareQuestTutorialSection()
    {
        if (UIManager.Instance != null && UIManager.Instance.emailBtn != null)
            UIManager.Instance.emailBtn.SetActive(true);
    }

    public void EndTutorial()
    {
        // 튜토리얼이 끝날 때, 켜져있는 포커싱 효과가 있다면 꺼줌
        ResetHighlight();

        isTutorial = false;
        tutorialPanel.SetActive(false); // 튜토리얼 창 끄기

        SetTutorialPanelRaycast(true);

        // 튜토리얼 때 나온 로그 텍스트를 지움
        if (UIManager.Instance.policyLogText != null)
        {
            UIManager.Instance.policyLogText.text = "";
        }

        // 잠가뒀던 모든 버튼 다시 켜주기
        SetButtonInteractable(youthPolicyBtn, true);
        SetButtonInteractable(seniorPolicyBtn, true);
        SetButtonInteractable(corpPolicyBtn, true);
        SetButtonInteractable(fundingBtn, true);

        SetButtonInteractable(yesBtn, true);
        SetButtonInteractable(noBtn, true);

        SetButtonInteractable(newsButton, true);
        SetButtonInteractable(emailBtn, true);
        SetButtonInteractable(logBtn, true);

        // 퀘스트 데모로 열렸을 수 있는 패널들 초기화
        if (UIManager.Instance.newsBranchPanel != null)
            UIManager.Instance.newsBranchPanel.SetActive(false);
        if (UIManager.Instance.questPanel != null)
            UIManager.Instance.questPanel.SetActive(false);
        if (UIManager.Instance.newsPanel != null)
            UIManager.Instance.newsPanel.SetActive(false);
        if (UIManager.Instance.logPanel != null)
            UIManager.Instance.logPanel.SetActive(false);
        if (UIManager.Instance.explainPolicyPanel != null)
            UIManager.Instance.explainPolicyPanel.SetActive(false);

        // 튜토리얼에서 변했던 데이터를 전부 초기화 함
        ScoreManager.Instance.InitData();
        GameManager.Instance.CURRENT_TURN = 1;

        // 초기화된 데이터를 UI에 반영
        UIManager.Instance.UpdateMoneyUI();
        ScoreManager.Instance.CalculateTurnScore();
        UIManager.Instance.UpdateTurnText();
        UIManager.Instance.UpdateRegionImages();
        UIManager.Instance.UpdateAffinityUI();
        if (UIManager.Instance.emailBtn != null) // 튜토리얼 종료 후 emailBtn은 퀘스트 없으므로 다시 비활성화
            UIManager.Instance.emailBtn.SetActive(false);
        UIManager.Instance.UpdateTotalScoreUI(ScoreManager.Instance.totalScore);
    }
}