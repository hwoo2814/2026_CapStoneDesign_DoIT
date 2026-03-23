using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public Text turnText; // 현재 턴 텍스트 표시

    public Text totalScoreText; // 현재 누적된 총점 텍스트 표시

    public GameObject eventPanel; // 돌발 이벤트가 뜰 때 화면에 나타나는 GameObject
    public Text eventTitleText; // 돌발 이벤트 창의 제목 텍스트
    public Image eventImage; // 돌발 이벤트 이미지(앵커 옆에)

    public GameObject hoverTooltip; // 마우스를 지역에 올렸을 때 마우스 옆에 튀어나오는 작은 창

    public Text hoverInfoText; // 오버 창에 들어갈 텍스트

    public GameObject endingPanel; // 턴이 모두 끝난 후 화면을 덮으며 나타날 최종 결과 창

    public Text endingResultText; // 결과 티어 텍스트
    public Text endingDescText; // 결과 스크롤 뷰 안의 결과에 대한 자세한 설명 텍스트

    public Image univImage; // 대학가 이미지
    public Image silverImage; // 실버타운 이미지
    public Image industryImage; // 산업단지 이미지
    public Image houseImage; // 주거단지 이미지
    public Text policyLogText; // Log Text 변수

    // 각 지역별 1~3레벨 전용 이미지 변수들
    // 대학가 발전도 이미지
    public Sprite univLv1Image; public Sprite univLv2Image; public Sprite univLv3Image; 
    
    // 실버타운 발전도 이미지
    public Sprite silverLv1Image; public Sprite silverLv2Image; public Sprite silverLv3Image;
    
    // 산업단지 발전도 이미지
    public Sprite indLv1Image; public Sprite indLv2Image; public Sprite indLv3Image;
    
    // 주거단지 발전도 이미지
    public Sprite houseLv1Image; public Sprite houseLv2Image; public Sprite houseLv3Image;

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

    //각 구역 업데이트 시, 각 지역의 레벨 이미지 묶음을 넘겨주는 함수
    public void UpdateRegionImages()
    {
        if (univImage != null) 
            univImage.sprite = GetLevelSprite(ScoreManager.Instance.devUniv, univLv1Image, univLv2Image, univLv3Image);
            
        if (silverImage != null) 
            silverImage.sprite = GetLevelSprite(ScoreManager.Instance.devSilver, silverLv1Image, silverLv2Image, silverLv3Image);
            
        if (industryImage != null) 
            industryImage.sprite = GetLevelSprite(ScoreManager.Instance.devIndustry, indLv1Image, indLv2Image, indLv3Image);
            
        if (houseImage != null) 
            houseImage.sprite = GetLevelSprite(ScoreManager.Instance.devHouse, houseLv1Image, houseLv2Image, houseLv3Image);
    }

    //넘겨받은 해당 지역의 레벨 이미지 중에서 발전도에 맞는 것을 골라서 반환
    private Sprite GetLevelSprite(float dev, Sprite lv1, Sprite lv2, Sprite lv3)
    {
        if (dev >= 50f) return lv3;
        else if (dev >= 20f) return lv2;
        else return lv1;
    }

    // 돌발 이벤트 보여주는 함수
    public void ShowEventPopup(string title, Sprite eventSprite)
    {
        eventTitleText.text = title;
        
        if (eventImage != null)
        {
            eventImage.sprite = eventSprite;
        }
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

    // 로그 메시지를 화면에 띄우는 함수
    public void AddPolicyLog(string logMsg)
    {
        if (policyLogText != null)
        {
            // 새로 들어온 메시지는 줄바꿈(\n)
            policyLogText.text += logMsg + "\n";
        }
    }
}