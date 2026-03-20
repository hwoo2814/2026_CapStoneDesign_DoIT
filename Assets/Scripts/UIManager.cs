using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public Text turnText; //현재 턴 텍스트 표시
    public Text totalScoreText; // 현재 누적된 총점 텍스트 표시

    public GameObject eventPanel; // 돌발 이벤트가 뜰 때 화면에 나타나는 GameObject
    public Text eventTitleText; // 돌발 이벤트 창의 제목 텍스트 

    public Image eventbackGround; // 배경(앵커, 타이틀 배경 등등...)
    public Image eventImage; // 각 이벤트 이미지(앵커 옆에)

    public GameObject hoverTooltip; // 마우스를 지역에 올렸을 때 마우스 옆에 튀어나오는 작은 창
    public Text hoverInfoText; // 작은 창에 들어갈 텍스트

    public GameObject endingPanel; // 턴이 모두 끝난 후 화면을 덮으며 나타날 최종 결과 창
    public Text endingResultText; // 결과 티어 텍스트
    public Text endingDescText; // 결과에 대한 자세한 설명 텍스트 

    public Image univImage; // 대학가 이미지
    public Image silverImage; // 실버타운 이미지
    public Image industryImage; // 산업단지 이미지
    public Image houseImage; // 주거단지 이미지

    // 발전도 레벨 별 스프라이트
    public Sprite lv1_Image; // Lv1(0 ~ 19)
    public Sprite lv2_Image; // Lv2(20 ~ 49)
    public Sprite lv3_Image; // Lv3(50 이상)

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // 현재 턴 표시 함수
    public void UpdateTurnText()
    {
        if (turnText != null)
        {
            turnText.text = $"턴수 {GameManager.Instance.CURRENT_TURN}/{GameManager.Instance.MAX_TURN}";
        }
        
        // 턴이 시작되거나 바뀔 때마다 지역레벨 체크하고 변경
        UpdateRegionImages();
    }

    // 현재 누적점수 표시
    public void UpdateTotalScoreUI(float score)
    {
        if (totalScoreText != null)
        {
            totalScoreText.text = $"점수: {Mathf.RoundToInt(score)}";
        }
    }

    // 레벨에 따라 지역 이미지를 변경하는 함수
    public void UpdateRegionImages()
    {
        if (univImage != null) univImage.sprite = GetLevelSprite(ScoreManager.Instance.devUniv);
        if (silverImage != null) silverImage.sprite = GetLevelSprite(ScoreManager.Instance.devSilver);
        if (industryImage != null) industryImage.sprite = GetLevelSprite(ScoreManager.Instance.devIndustry);
        if (houseImage != null) houseImage.sprite = GetLevelSprite(ScoreManager.Instance.devHouse);
    }

    // 발전도 수치에 따라 레벨에 따라 이미지를 결정하는 함수
    private Sprite GetLevelSprite(float dev)
    {
        if (dev >= 50f) return lv3_Image;
        else if (dev >= 20f) return lv2_Image;
        else return lv1_Image;
    }

    // 돌발 이벤트 보여주는 함수
    public void ShowEventPopup(string title/*, Sprite backGround, Sprite eventSprite*/)
    {
        eventTitleText.text = title;

        /*if (eventbackGround != null)
        {
            eventbackGround.sprite = backGround;
        }*/
        
        /*if (eventImage != null)
        {
            eventImage.sprite = eventSprite;
        }*/
        eventPanel.SetActive(true);
    } 

    //마우스 오버시 각 지역 정보를 HoverTooltip에 보여주는 함수
    public void OnRegionHoverEnter(int regionIndex) 
    {
        hoverTooltip.SetActive(true);
        float dev = 0f;
        string rName = "";

        if (regionIndex == 1) { rName = "대학가"; dev = ScoreManager.Instance.devUniv; }
        else if (regionIndex == 2) { rName = "실버타운"; dev = ScoreManager.Instance.devSilver; }
        else if (regionIndex == 3) { rName = "산업단지"; dev = ScoreManager.Instance.devIndustry; }
        else if (regionIndex == 4) { rName = "주거단지"; dev = ScoreManager.Instance.devHouse; }

        string lv = dev >= 50f ? "LV 3" : (dev >= 20f ? "LV 2" : "LV 1");
        hoverInfoText.text = $"{rName}\n발전 {lv}";
    }

    // 마우스 오버 끝나면 꺼지게 하는 함수
    public void OnRegionHoverExit()
    {
        hoverTooltip.SetActive(false);
    }

    // 게임 끝나면 최종 점수와 평가 보이게 하는 함수
    public void ShowEndingPanel(string grade, string title, float finalScore, string description)
    {
        endingResultText.text = $"최종 점수: {Mathf.RoundToInt(finalScore)}\n등급: {grade}\n칭호: {title}";
        endingDescText.text = description;
        endingPanel.SetActive(true);
    }
}