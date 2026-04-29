using UnityEngine;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    public bool isTutorial = false; // 튜토리얼 상태
    private int currentStep = 0; // 튜토리얼 대사 한줄씩 카운팅

    public GameObject tutorialPanel; // 화면 전체를 덮는 투명 패널 
    public Image characterImage;  // 비서 캐릭터 이미지
    public Text dialogText; // 대사 텍스트 
    public GameObject dialogBox; // 대사창 배경 

    // 캐릭터 이미지와 대사창의 위치 정보를 담을 배열입니다.
    // Dialogues 배열과 크기가 같아야 합니다.
    // characterPositions, dialogBoxPositions, dialogues, highlightTargets 
    // 위 4개의 배열을 크기가 같아야 에러가 나지 않습니다.
    public Vector2[] characterPositions; // 각 스텝별 캐릭터 위치
    public Vector2[] dialogBoxPositions; // 각 스텝별 대사창 위치

    [TextArea(1, 5)] // 한 대사에 최소 줄수 1 ~ 최대 줄수 5줄까지 입력할수 있음. 자유롭게 수정하여 사용.
    public string[] dialogues;

    // 대사가 넘어갈 때마다 밝게 강조할 UI 오브젝트를 넣을 배열과 현재 강조 중인 대상을 기억할 변수
    public GameObject[] highlightTargets; 
    private GameObject currentHighlightTarget = null;

    public Button youthPolicyBtn; // 청년 정책 (이것만 누르게 함)  
    public Button seniorPolicyBtn; // 노년 정책 (튜토리얼 중 잠금)
    public Button corpPolicyBtn; // 기업 정책 (튜토리얼 중 잠금)   
    public Button fundingBtn; // 자금 확보 (튜토리얼 중 잠금)
    public Button yesBtn; // ExplainPolicyPanel의 "예" 버튼
    public Button noBtn; // ExplainPolicyPanel의 "아니요" 버튼
    public Button newsButton; // NewsBranchPanel을 여는 뉴스 버튼
    public Button emailBtn; // NewsBranchPanel 안의 이메일(퀘스트) 버튼
    public GameObject logBtn; // 로그 열기/닫기 버튼 오브젝트

    public int buttonClickStep = 5;
    public int yesBtnClickStep = 6; // 청년 정책 버튼을 누르라고 지시하는 대사의 순번
                                    // 6번째 대사에서 클릭을 기다리게함 (임시)
    public int newsButtonClickStep = 10; // 뉴스 버튼을 누르라고 지시하는 대사의 순번
                                         // 10번째 대사에서 클릭을 기다리게함 (임시)
    public int emailBtnClickStep = 12; // 이메일 버튼을 누르라고 지시하는 대사의 순번
                                       // 12번째 대사에서 클릭을 기다리게함 (임시)
    public int logBtnClickStep = 14; // 로그 버튼 클릭을 유도하는 대사의 순번 (임시값, 인스펙터에서 조정)
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
        tutorialPanel.GetComponent<Image>().raycastTarget = true; // 화면 클릭 감지 켜기 

        // 시작 시 모든 정책 버튼 잠금 (미리 누르는 것 방지)
        youthPolicyBtn.interactable = false;
        seniorPolicyBtn.interactable = false;
        corpPolicyBtn.interactable = false;
        fundingBtn.interactable = false;

        // 시작 시 "예"/"아니요" 버튼 잠금 
        if (yesBtn != null)    yesBtn.interactable    = false;
        if (noBtn != null) noBtn.interactable = false;

        // 시작 시 뉴스/이메일 버튼 잠금
        if (newsButton != null) newsButton.interactable = false;
        if (emailBtn != null) emailBtn.interactable = false;

        ShowNextDialogue();
    }
    
    // 화면(튜토리얼 패널)을 클릭할 때마다 실행됨
    public void OnClickScreen()
    {
        if (!isTutorial) return;

        // 버튼 클릭을 지시하는 단계에서는 화면을 눌러도 대사가 넘어가지 않음
        if (currentStep == buttonClickStep) return;
        if (currentStep == yesBtnClickStep)    return;
        if (currentStep == newsButtonClickStep) return;
        if (currentStep == emailBtnClickStep)  return;

        currentStep++;
        
        // 대사가 아직 남아있다면 다음 대사 출력
        if (currentStep < dialogues.Length)
        {
            ShowNextDialogue();
        }
        else // 모든 대사를 다 보고 클릭했다면 튜토리얼 종료!
        {
            EndTutorial(); 
        }
    }

    // 설명 출력하는 함수
    private void ShowNextDialogue()
    {
        dialogText.text = dialogues[currentStep];

        UpdateUIPositions();
        ResetHighlight();

        // 이번 대사 순번에 맞게 강조해야 할 타겟 UI가 있다면 어두운 배경 앞으로 끌어옴
        if (highlightTargets != null && highlightTargets.Length > currentStep && highlightTargets[currentStep] != null)
        {
            currentHighlightTarget = highlightTargets[currentStep];
            SetHighlight(currentHighlightTarget);
        }

        if (currentStep == buttonClickStep)
        {
            // 청년 버튼만 켜서 청년 정책 누르는게 함, 튜토리얼 패널 클릭 비활성화
            tutorialPanel.GetComponent<Image>().raycastTarget = false;
            youthPolicyBtn.interactable = true;
        }
        else if (currentStep == yesBtnClickStep)
        {
            tutorialPanel.GetComponent<Image>().raycastTarget = false;
            // "예" 버튼만 활성화하고 "아니요"는 잠가 무조건 확정하도록 유도
            if (yesBtn != null)    yesBtn.interactable    = true;
            if (noBtn != null) noBtn.interactable = false;
        }
        // 뉴스 버튼 클릭 유도 단계
        else if (currentStep == newsButtonClickStep)
        {
            PrepareQuestTutorialSection(); // emailBtn 강제 활성화 (퀘스트 없어도 데모 가능하도록)
            tutorialPanel.GetComponent<Image>().raycastTarget = false;
            if (newsButton != null) newsButton.interactable = true;
        }
        // 이메일 버튼 클릭 유도 단계
        else if (currentStep == emailBtnClickStep)
        {
            tutorialPanel.GetComponent<Image>().raycastTarget = false;
            if (emailBtn != null) emailBtn.interactable = true;
        }
        else
        {
            tutorialPanel.GetComponent<Image>().raycastTarget = true;
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
        if (currentHighlightTarget != null)
        {
            Canvas canvas = currentHighlightTarget.GetComponent<Canvas>();
            if (canvas != null)
            {
                // 강제로 높였던 렌더링 순서 설정을 해제
                canvas.overrideSorting = false;
                canvas.sortingOrder = 0;
            }
            currentHighlightTarget = null; // 타겟 초기화
        }
    }

    // 카드 컨트롤러에서 청년 버튼을 눌렀을 때 호출되어 다음 대사로 진행하는 함수
    public void OnYouthPolicyClicked()
    {
        if (!isTutorial) return;

        // 다시 청년 버튼을 끄고 튜토리얼 패널 클릭 활성화
        youthPolicyBtn.interactable = false;
        tutorialPanel.GetComponent<Image>().raycastTarget = true;

        // 다음 대사로 넘어가기
        currentStep++;
        ShowNextDialogue();
    }

    // "예" 버튼이 눌려 정책이 실행된 직후 다음 대사로 진행시키는 함수
    //ExplainPolicyPanel의 "예" 버튼을 누른 직후 GameManager에서 호출.
    //"아니요" 버튼 잠금 해제 후 다음 대사로 넘어간다.
    public void OnYesBtnClicked()
    {
        if (!isTutorial) return;

        // "아니요" 버튼 잠금 해제 (이후 본 게임에서 정상 사용 가능하도록)
        if (noBtn != null) noBtn.interactable = true;
        if (yesBtn != null)    yesBtn.interactable    = false;

        tutorialPanel.GetComponent<Image>().raycastTarget = true;

        currentStep++;
        if (currentStep < dialogues.Length)
            ShowNextDialogue();
        else
            EndTutorial();
    }

  
    // GameManager.NewsBranchButton() 에서 튜토리얼 중 호출
    // NewsBranchPanel이 열린 직후 다음 대사(이메일 버튼 안내)로 진행
    // 뉴스 버튼을 눌러 NewsBranchPanel이 열린 직후 GameManager에서 호출.
    // 뉴스 버튼을 다시 잠그고 다음 대사로 넘어간다.
    public void OnNewsButtonClicked()
    {
        if (!isTutorial) return;

        if (newsButton != null) newsButton.interactable = false;
        tutorialPanel.GetComponent<Image>().raycastTarget = true;

        currentStep++;
        if (currentStep < dialogues.Length)
            ShowNextDialogue();
        else
            EndTutorial();
    }


    // GameManager.EmaillButton() 에서 튜토리얼 중 호출
    // QuestPanel이 열린 직후 다음 대사(퀘스트 패널 설명)로 진행
    // 이메일 버튼을 눌러 QuestPanel이 열린 직후 GameManager에서 호출.
    // 이메일 버튼을 다시 잠그고 다음 대사로 넘어간다.
    public void OnEmailBtnClicked()
    {
        if (!isTutorial) return;

        if (emailBtn != null) emailBtn.interactable = false;
        tutorialPanel.GetComponent<Image>().raycastTarget = true;

        currentStep++;
        if (currentStep < dialogues.Length)
            ShowNextDialogue();
        else
            EndTutorial();
    }

    // 튜토리얼 중 로그 버튼이 눌렸을 때 GameManager.LogButton()에서 호출
    // 로그 버튼을 다시 잠그고 로그 패널 설명 대사로 진행
    public void OnLogBtnClicked()
    {
        if (!isTutorial) return;

        // 로그 버튼 다시 잠금 (중복 클릭 방지)
        if (logBtn != null) logBtn.GetComponent<Button>().interactable = false;
        tutorialPanel.GetComponent<Image>().raycastTarget = true;

        currentStep++;
        if (currentStep < dialogues.Length)
            ShowNextDialogue();
        else
            EndTutorial();
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

        // 튜토리얼 때 나온 로그 텍스트를 지움
        if (UIManager.Instance.policyLogText != null)
        {
            UIManager.Instance.policyLogText.text = ""; 
        }
        
        // 잠가뒀던 모든 정책버튼 다시 켜주기
        youthPolicyBtn.interactable = true;
        seniorPolicyBtn.interactable = true;
        corpPolicyBtn.interactable = true;
        fundingBtn.interactable = true;

        // 튜토리얼 종료 시 "예/아니요" 버튼 잠금 해제
        if (yesBtn != null) yesBtn.interactable = true;
        if (noBtn != null) noBtn.interactable = true;

        // 튜토리얼 종료 시 뉴스/이메일 버튼 잠금 해제
        if (newsButton != null) newsButton.interactable = true;
        if (emailBtn != null) emailBtn.interactable = true;

        // 퀘스트 데모로 열렸을 수 있는 패널들 초기화
        if (UIManager.Instance.newsBranchPanel != null)
            UIManager.Instance.newsBranchPanel.SetActive(false);
        if (UIManager.Instance.questPanel != null)
            UIManager.Instance.questPanel.SetActive(false);
        if (UIManager.Instance.newsPanel != null)
            UIManager.Instance.newsPanel.SetActive(false);

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