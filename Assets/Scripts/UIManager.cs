using UnityEngine;
using UnityEngine.UI;
using System;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public GameObject optionPanel; // 옵션창 패널
    public GameObject newsBranchPanel;
    public GameObject EmailPanel;
    public GameObject newsPanel; // 뉴스창 패널
    public Text ResultProposal;
    public Button fundingBtn; // 자금이 100일때 버튼 선택을 막기위해서 가져옴
    public Image MoneyBar; // 돈 게이지 슬라이드

    public GameObject explainPolicyPanel; //정책 설명 패널
    public Text policyTitleText; // 정책 이름 텍스트
    public Text policyDescText; // 정책 설명 텍스트
    
    //민심 게이지 표현할 Image
    // 청년 민심
    public Image youthBorderImg; 
    public Image youthFillImg;
    // 노년 민심
    public Image seniorBorderImg;
    public Image seniorFillImg;
    // 기업 민심
    public Image corpBorderImg;
    public Image corpFillImg;

    public Text turnText; // 현재 턴 텍스트
    public Text totalScoreText; // 게임 창에 띄울 현재 누적된 총점 텍스트
    public Text successProbText; // 확률 텍스트 변수
    public Text endingTotalScoreText; // 엔딩창에 띄울 최종 점수 텍스트

    public GameObject eventPanel; // 돌발 이벤트가 뜰 때 화면에 나타나는 GameObject
    public Text eventTitleText; // 돌발 이벤트 창의 제목 텍스트
    public Image eventImage; // 돌발 이벤트 이미지(앵커 옆에)

    public GameObject hoverTooltip; // 마우스를 지역에 올렸을 때 마우스 옆에 튀어나오는 작은 창

    //호버 툴팁 위치 설정하는 Transform 변수
    public Transform univHoverPos;
    public Transform silverHoverPos;
    public Transform industryHoverPos;
    public Transform houseHoverPos;

    public Text hoverInfoText; // 오버 창에 들어갈 텍스트

    public GameObject endingPanel; // 턴이 모두 끝난 후 화면을 덮으며 나타날 최종 결과 창
    public Text regionUnivScoreText; // 대학가 점수
    public Text regionSilverScoreText; // 실버타운 점수
    public Text regionIndustryScoreText; // 산업단지 점수
    public Text regionHouseScoreText; // 주거단지 점수
    
    public Text gradeText; // 최종 결과 등급 텍스트
    public Text titleText; // 등급에 따른 칭호 텍스트
    
    public Image univImage; // 대학가 이미지
    public Image silverImage; // 실버타운 이미지
    public Image industryImage; // 산업단지 이미지
    public Image houseImage; // 주거단지 이미지
    public Text policyLogText; // Log Text 변수

    public Text newsWarningText; // 지역 소멸 경고 텍스트

    // 각 지역별 1~3레벨 전용 이미지 변수들
    // 대학가 발전도 이미지
    public Sprite univLv1Image; public Sprite univLv2Image; public Sprite univLv3Image; 
    
    // 실버타운 발전도 이미지
    public Sprite silverLv1Image; public Sprite silverLv2Image; public Sprite silverLv3Image;
    
    // 산업단지 발전도 이미지
    public Sprite indLv1Image; public Sprite indLv2Image; public Sprite indLv3Image;
    
    // 주거단지 발전도 이미지
    public Sprite houseLv1Image; public Sprite houseLv2Image; public Sprite houseLv3Image;

    // 각 지역 비활성화 이미지
    public Sprite univDeactivatedImage; // 대학가 비활성화 시 표시할 이미지
    public Sprite silverDeactivatedImage; // 실버타운 비활성화 시 표시할 이미지
    public Sprite indDeactivatedImage; // 산업단지 비활성화 시 표시할 이미지
    public Sprite houseDeactivatedImage; // 주거단지 비활성화 시 표시할 이미지

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
            turnText.text = $"{GameManager.Instance.CURRENT_TURN}/{GameManager.Instance.MAX_TURN}";
        }
    }

    // 돈 게이지, 성공 확률 업데이트
    public void UpdateMoneyUI()
    {
        if (MoneyBar != null)
        {
            MoneyBar.fillAmount = (float)ScoreManager.Instance.money / GameManager.Instance.MAX_MONEY;
        }
        UpdateSuccessProbabilityUI(ScoreManager.Instance.money);
    }

    // 현재 누적점수 표시
    public void UpdateTotalScoreUI(float score)
    {
        if (totalScoreText != null)
        {
            totalScoreText.text = $"점수: {Mathf.RoundToInt(score)}";
        }
    }

    // 정책 성공 확률을 표시하는 함수
    public void UpdateSuccessProbabilityUI(float money)
    {
        if (successProbText != null)
        {
            successProbText.text = GetSuccessProbabilityText(money);
        }
    }

    // 성공 확률 텍스트 표시 함수
    public string GetSuccessProbabilityText(float money)
    {
        if (money >= 100f) return "성공 확률 : 100%";
        if (money <= 0f) return "성공 확률 : 0%";

        if (money >= 80f) return "성공 확률 : 80% ~ 99%";
        else if (money >= 60f) return "성공 확률 : 60% ~ 79%";
        else if (money >= 40f) return "성공 확률 : 40% ~ 59%";
        else if (money >= 20f) return "성공 확률 : 20% ~ 39%";
        else return "성공 확률 : 1% ~ 20%";
    }

    // 민심 게이지 표현 함수
    public void UpdateAffinityUI()
    {
        // 각 계층별로 게이지 조절 함수를 호출함
        UpdateSingleAffinity(ScoreManager.Instance.youthAffinity, youthBorderImg, youthFillImg);
        UpdateSingleAffinity(ScoreManager.Instance.seniorAffinity, seniorBorderImg, seniorFillImg);
        UpdateSingleAffinity(ScoreManager.Instance.corpAffinity, corpBorderImg, corpFillImg);
    }

    // 하나의 민심 게이지를 계산하고 색상을 바꿔주는 함수
    private void UpdateSingleAffinity(float affinityValue, Image borderImg, Image fillImg)
    {
        if (borderImg == null || fillImg == null) return;

        // 테두리 색상 처리 (1 이하일 때만 빨간색, 나머지는 원래 색)
        if (affinityValue == 0f)
        {
            borderImg.color = Color.red;
        }
        else
        {
            borderImg.color = Color.white; // 원래 테두리 색인 흰색으로 설정
        }

        // 게이지 채우기 (0 ~ 10 사이의 값만 게이지로 표현)
        float displayValue = Mathf.Clamp(affinityValue, GameManager.Instance.MIN_AFFINITY, GameManager.Instance.MAX_AFFINITY); 
        
        // 게이지의 최대치가 10이므로 10으로 나누어 0~1 사이의 값으로 만듬
        fillImg.fillAmount = displayValue / GameManager.Instance.MAX_AFFINITY; 
    }

    // 정책 이름과 설명을 받아 explainPolicyPanel을 활성화하는 함수.
    public void ShowExplainPolicyPanel(string policyName, string policyDesc)
    {
        if (policyTitleText != null)
            policyTitleText.text = policyName;

        if (policyDescText != null)
            policyDescText.text = policyDesc;

        if (explainPolicyPanel != null)
            explainPolicyPanel.SetActive(true);
    }

    // explainPolicyPanel 비활성화 함수
    // "예" 또는 "아니요" 버튼 클릭 이후 GameManager에서 호출됨
    public void HideExplainPolicyPanel()
    {
        if (explainPolicyPanel != null)
            explainPolicyPanel.SetActive(false);
    }

    // 각 구역 업데이트 시, 각 지역의 레벨 이미지 묶음을 넘겨주는 함수
    // 지역 비활성시 비활성된 지역 이미지를 넘김
    public void UpdateRegionImages()
    {
        if (univImage != null) 
            univImage.sprite = ScoreManager.Instance.isUnivDeactivated 
                ? univDeactivatedImage 
                : GetLevelSprite(ScoreManager.Instance.devUniv, univLv1Image, univLv2Image, univLv3Image);
        if (silverImage != null) 
            silverImage.sprite = ScoreManager.Instance.isSilverDeactivated
                ? silverDeactivatedImage
                : GetLevelSprite(ScoreManager.Instance.devSilver, silverLv1Image, silverLv2Image, silverLv3Image);
            
        if (industryImage != null) 
            industryImage.sprite = ScoreManager.Instance.isIndustryDeactivated
                ? indDeactivatedImage
                : GetLevelSprite(ScoreManager.Instance.devIndustry, indLv1Image, indLv2Image, indLv3Image);
            
        if (houseImage != null) 
            houseImage.sprite = ScoreManager.Instance.isHouseDeactivated
                ? houseDeactivatedImage
                : GetLevelSprite(ScoreManager.Instance.devHouse, houseLv1Image, houseLv2Image, houseLv3Image);
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
        bool isDeactivated = false;

        RectTransform tooltipRect = hoverTooltip.GetComponent<RectTransform>();

        // 지역에 따라 이름, 발전도를 할당, Transform 변수 지정
        if (regionIndex == 1) 
        { 
            rName = "신도시"; 
            dev = ScoreManager.Instance.devUniv;
            isDeactivated = ScoreManager.Instance.isUnivDeactivated;
            if (univHoverPos != null) tooltipRect.position = univHoverPos.position;
        }
        else if (regionIndex == 2) 
        {
            rName = "농촌"; 
            dev = ScoreManager.Instance.devSilver;
            isDeactivated = ScoreManager.Instance.isSilverDeactivated;
            if (silverHoverPos != null) tooltipRect.position = silverHoverPos.position; 
        }
        else if (regionIndex == 3) 
        {
            rName = "지방"; 
            dev = ScoreManager.Instance.devIndustry;
            isDeactivated = ScoreManager.Instance.isIndustryDeactivated;
            if (industryHoverPos != null) tooltipRect.position = industryHoverPos.position;

        }
        else if (regionIndex == 4)
        { 
            rName = "수도권"; 
            dev = ScoreManager.Instance.devHouse;
            isDeactivated = ScoreManager.Instance.isHouseDeactivated;
            if (houseHoverPos != null) tooltipRect.position = houseHoverPos.position;
        }

        // 지역이 비활성화 되었다면 호버시 지역 비활성화 표시
        if (isDeactivated)
        {
            hoverInfoText.text = $"{rName}\n[ 비활성화 ]";
        }
        // 지역이 활성화 되어있다면 지역 레벨 표시
        else
        {
            string lv = dev >= 50f ? "LV 3" : (dev >= 20f ? "LV 2" : "LV 1");
            hoverInfoText.text = $"{rName}\n발전도 : {lv}";
        }
    }

    // 마우스 오버 끝나면 꺼지게 하는 함수
    public void OnRegionHoverExit()
    {
        hoverTooltip.SetActive(false);
    }

    // 자금 100이면 버튼 비활성화하는 함수
    public void UpdateFundingButtonState()
    {
        if (fundingBtn != null)
        {
            // 자금이 100이거나 크면
            bool isFull = ScoreManager.Instance.money >= GameManager.Instance.MAX_MONEY;
            fundingBtn.interactable = !isFull; // 버튼 비활성화
        }
    }

    // 뉴스 패널을 열고 지역 소멸 경고 메시지를 표시하는 함수
    // GameManager.StartTurn()에서 15~19턴 사이에 LV1 지역 존재 시 호출됨
    public void ShowRegionDeactivationWarning(string warningMessage)
    {
        if (newsWarningText != null)
            newsWarningText.text = warningMessage;

        if (newsPanel != null)
            newsPanel.SetActive(true);
    }

    // 게임 끝나면 최종 점수와 평가 보이게 하는 함수
    public void ShowEndingPanel(string grade, string title, float finalScore)
    {
        // 엔딩 패널 활성화  후 시간 정지
        endingPanel.SetActive(true);
        Time.timeScale = 0f;

        // 구역별 누적 점수 가져와 보여주기 (RoundToInt로 소수점 버리고 스트링으로 표현)
        regionUnivScoreText.text = "대학가 최종 점수 : " + Mathf.RoundToInt(ScoreManager.Instance.totalUnivScore).ToString();
        regionSilverScoreText.text = "실버타운 최종 점수 : " + Mathf.RoundToInt(ScoreManager.Instance.totalSilverScore).ToString();
        regionIndustryScoreText.text = "산업단지 최종 점수 : " + Mathf.RoundToInt(ScoreManager.Instance.totalIndustryScore).ToString();
        regionHouseScoreText.text = "주거단지 최종 점수 : " + Mathf.RoundToInt(ScoreManager.Instance.totalHouseScore).ToString();

        // 최종 점수 (RoundToInt로 소수점 버리고 스트링으로 표현), 등급, 칭호 입력
        endingTotalScoreText.text = "최종 점수 : " + Mathf.RoundToInt(finalScore).ToString();
        gradeText.text = grade;
        titleText.text = title;
    }

    // 지역 비활성화 발동 시 비활성화된 지역명을 받아 
    // 로그에 경고 메시지를 보내는 함수
    // ScoreManager.CheckSingleRegionDeactivation()에서 호출됨
    public void ShowDeactivationNotice(string regionName)
    {
        AddPolicyLog($"경고! : [{regionName}] 지역이 비활성화되었습니다!!!\n이제부터 해당 지역 점수가 제외됩니다.");
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