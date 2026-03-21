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

    public Button youthPolicyBtn; // 청년 정책 (이것만 누르게 함)  
    public Button seniorPolicyBtn; // 노년 정책 (튜토리얼 중 잠금)
    public Button corpPolicyBtn; // 기업 정책 (튜토리얼 중 잠금)   
    public Button fundingBtn; // 자금 확보 (튜토리얼 중 잠금)      

    public int buttonClickStep = 5; // 청년 정책 버튼을 누르라고 지시하는 대사의 순번
                                    // 5번째 대사에서 클릭을 기다리게함 (임시)

    [TextArea(1, 5)] // 한 대사에 최소 줄수 1 ~ 최대 줄수 5줄까지 입력할수 있음. 자유롭게 수정하여 사용.

    public string[] dialogues = new string[]
    {
        "대사...", // 0
        "대사...", // 1
        "대사...", // 2
        "대사...", // 3
        "대사...", // 4
        "대사...", // 5 (여기서 진행 멈추고 버튼 활성화!)
        "대사...", // 6 (버튼 클릭 후 나오는 대사)
        "대사..." // 7 (마지막 대사)
    };

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

        ShowNextDialogue();
    }
    
    // 화면(튜토리얼 패널)을 클릭할 때마다 실행됨
    public void OnClickScreen()
    {
        if (!isTutorial) return;

        // 버튼 클릭을 지시하는 단계에서는 화면을 눌러도 대사가 넘어가지 않음
        if (currentStep == buttonClickStep) return;

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

        if (currentStep == buttonClickStep)
        {
            // 청년 버튼만 켜서 청년 정책 누르는게 함, 튜토리얼 패널 클릭 비활성화
            tutorialPanel.GetComponent<Image>().raycastTarget = false;
            youthPolicyBtn.interactable = true;
        }
        else
        {
            tutorialPanel.GetComponent<Image>().raycastTarget = true;
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

    public void EndTutorial()
    {
        isTutorial = false;
        tutorialPanel.SetActive(false); // 튜토리얼 창 끄기
        
        // 잠가뒀던 모든 버튼 다시 켜주기 (본 게임 시작)
        youthPolicyBtn.interactable = true;
        seniorPolicyBtn.interactable = true;
        corpPolicyBtn.interactable = true;
        fundingBtn.interactable = true;

        //튜토리얼에서 변했던 데이터를 전부 초기화 함
        ScoreManager.Instance.InitData(); 
        GameManager.Instance.CURRENT_TURN = 1; 

        // 초기화된 데이터를 UI에 반영
        GameManager.Instance.UpdateMoneyUI();
        ScoreManager.Instance.CalculateTurnScore();
        UIManager.Instance.UpdateTurnText();
        UIManager.Instance.UpdateRegionImages();
    }
}