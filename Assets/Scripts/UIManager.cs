using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public GameObject optionPanel; // 옵션창 패널
    public GameObject emailBtn; // 이메일 버튼
    public Image newsButtonImage; // 뉴스 버튼 이미지 (퀘스트 알림 시 교체할 토대)
    public Sprite newsDefaultSprite; // 뉴스 버튼 기본 이미지
    public Sprite newsQuestAlertSprite; // 퀘스트 생성 시 표시할 뉴스 알림 이미지
    public GameObject newsBranchPanel; // 뉴스 브렌치 패널
    public GameObject questPanel; // 이메일(퀘스트) 패널
    public GameObject newsPanel; // 뉴스창 패널
    // AI뉴스
    public Text newsWarningText; // 뉴스 경고 문구 텍스트
    public Text ResultProposal; // 수락/거절 문구 텍스트
    public GameObject acceptBtn; // 안건 수락 버튼 오브젝트
    public GameObject refuseBtn; // 안건 거절 버튼 오브젝트
    public Button fundingBtn; // 자금이 100일때 버튼 선택을 막기위해서 가져옴
    public Image MoneyBar; // 돈 게이지 슬라이드
    public GameObject aiHintPanel; // AI 힌트를 표시하는 패널
    public Text aiHintText; // AI 힌트 잔여 턴 수 텍스트
    public Text questTitleText; // 퀘스트 제목 텍스트
    public Text questDescText; // 퀘스트 배경 설명 텍스트
    public Text summaryText; // 퀘스트 요약 텍스트
    public Text resultProposalText; // 퀘스트 수락/거절 텍스트
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

    // 지역을 강조하는 외각선들
    public Outline univOutline; // 신도시 지역 Outline
    public Outline silverOutline; // 농촌 지역 Outline
    public Outline industryOutline; // 지방 지역 Outline
    public Outline houseOutline; // 수도권 지역 Outline
    private Outline currentRegionOutline; // 현재 마우스가 올라가 있는 지역의 Outline을 임시로 저장하는 변수

    public GameObject endingPanel; // 턴이 모두 끝난 후 화면을 덮으며 나타날 최종 결과 창
    public Text regionUnivScoreText; // 대학가 점수
    public Text regionSilverScoreText; // 실버타운 점수
    public Text regionIndustryScoreText; // 산업단지 점수
    public Text regionHouseScoreText; // 주거단지 점수

    public Text gradeText; // 최종 결과 등급 텍스트
    public Text titleText; // 등급에 따른 칭호 텍스트

    public GameObject univImage; // 대학가 이미지
    public GameObject silverImage; // 실버타운 이미지
    public GameObject industryImage; // 산업단지 이미지
    public GameObject houseImage; // 주거단지 이미지

    public Text policyLogText; // Log Text 변수
    public GameObject logPanel; // 로그 전용 패널
    public GameObject logCloseBtn; // 로그 패널 내부의 닫기 버튼

    // 각 지역별 1~3레벨 전용 이미지 변수들
    // 대학가 발전도 이미지
    public GameObject univLv1Image;
    public GameObject univLv2Image;
    public GameObject univLv3Image;

    // 실버타운 발전도 이미지
    public GameObject silverLv1Image;
    public GameObject silverLv2Image;
    public GameObject silverLv3Image;

    // 산업단지 발전도 이미지
    public GameObject indLv1Image;
    public GameObject indLv2Image;
    public GameObject indLv3Image;

    // 주거단지 발전도 이미지
    public GameObject houseLv1Image;
    public GameObject houseLv2Image;
    public GameObject houseLv3Image;

    // 자물쇠 프리팹 (하나만 사용)
    public GameObject lockOverlayPrefab;

    // 각 지역에 생성된 자물쇠 인스턴스
    private GameObject univLock;
    private GameObject silverLock;
    private GameObject indLock;
    private GameObject houseLock;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        InitLockOverlays();
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
            totalScoreText.text = $"{Mathf.RoundToInt(score)}";
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

    // 각 구역 업데이트 시, 발전도 레벨에 맞는 3D 프리팹으로 지역 오브젝트 전체를 교체하는 함수
    public void UpdateRegionImages()
    {
        if (univImage != null)
        {
            GameObject nextPrefab = GetLevelPrefab(
                ScoreManager.Instance.devUniv,
                univLv1Image,
                univLv2Image,
                univLv3Image
            );

            ReplaceRegionObject(ref univImage, nextPrefab);
        }

        if (silverImage != null)
        {
            GameObject nextPrefab = GetLevelPrefab(
                ScoreManager.Instance.devSilver,
                silverLv1Image,
                silverLv2Image,
                silverLv3Image
            );

            ReplaceRegionObject(ref silverImage, nextPrefab);
        }

        if (industryImage != null)
        {
            GameObject nextPrefab = GetLevelPrefab(
                ScoreManager.Instance.devIndustry,
                indLv1Image,
                indLv2Image,
                indLv3Image
            );

            ReplaceRegionObject(ref industryImage, nextPrefab);
        }

        if (houseImage != null)
        {
            GameObject nextPrefab = GetLevelPrefab(
                ScoreManager.Instance.devHouse,
                houseLv1Image,
                houseLv2Image,
                houseLv3Image
            );

            ReplaceRegionObject(ref houseImage, nextPrefab);
        }

        UpdateLockOverlays();
    }

    // 현재 지역 오브젝트를 발전도 레벨에 맞는 3D 프리팹으로 교체하는 함수
    // currentObj : 현재 씬에 배치되어 있는 지역 오브젝트 참조, newPrefab : 새로 생성할 레벨별 3D 프리팹
    private void ReplaceRegionObject(ref GameObject currentObj, GameObject newPrefab)
    {
        // 교체할 프리팹이 없거나 현재 오브젝트가 없으면 실행하지 않음
        if (currentObj == null || newPrefab == null) return;

        // 이미 같은 프리팹으로 생성된 오브젝트라면 불필요한 교체를 막음
        // Instantiate된 오브젝트는 이름 뒤에 (Clone)이 붙기 때문에 이름 기준으로 비교
        string currentName = currentObj.name.Replace("(Clone)", "").Trim();
        string prefabName = newPrefab.name.Replace("(Clone)", "").Trim();

        if (currentName == prefabName) return;

        // 기존 오브젝트의 부모, 위치, 회전, 크기 정보를 저장
        Transform parent = currentObj.transform.parent;
        Vector3 position = currentObj.transform.position;
        Quaternion rotation = currentObj.transform.rotation;
        Vector3 scale = currentObj.transform.localScale;

        // 기존 지역 오브젝트 제거
        Destroy(currentObj);

        // 새 프리팹을 기존 오브젝트와 같은 위치에 생성
        GameObject createdObj = Instantiate(newPrefab, position, rotation, parent);

        // 기존 오브젝트의 스케일을 유지
        createdObj.transform.localScale = scale;

        // 현재 지역 오브젝트 참조를 새로 생성된 오브젝트로 갱신
        currentObj = createdObj;
    }

    // 발전도 값을 기준으로 현재 레벨에 맞는 3D 프리팹을 반환하는 함수
    private GameObject GetLevelPrefab(float dev, GameObject lv1, GameObject lv2, GameObject lv3)
    {
        if (dev >= 50f) return lv3;
        else if (dev >= 20f) return lv2;
        else return lv1;
    }

    // 각 지역 위에 자물쇠 오브젝트를 생성하는 함수
    private void InitLockOverlays()
    {
        if (lockOverlayPrefab == null) return;

        if (univImage != null)
            univLock = Instantiate(lockOverlayPrefab, univImage.transform);

        if (silverImage != null)
            silverLock = Instantiate(lockOverlayPrefab, silverImage.transform);

        if (industryImage != null)
            indLock = Instantiate(lockOverlayPrefab, industryImage.transform);

        if (houseImage != null)
            houseLock = Instantiate(lockOverlayPrefab, houseImage.transform);

        // 처음에는 전부 꺼둠
        if (univLock != null) univLock.SetActive(false);
        if (silverLock != null) silverLock.SetActive(false);
        if (indLock != null) indLock.SetActive(false);
        if (houseLock != null) houseLock.SetActive(false);
    }

    // 각 지역의 비활성화 상태에 따라 자물쇠 오버레이를 개별적으로 제어하는 함수
    private void UpdateLockOverlays()
    {
        if (univLock != null)
            univLock.SetActive(ScoreManager.Instance.isUnivDeactivated);

        if (silverLock != null)
            silverLock.SetActive(ScoreManager.Instance.isSilverDeactivated);

        if (indLock != null)
            indLock.SetActive(ScoreManager.Instance.isIndustryDeactivated);

        if (houseLock != null)
            houseLock.SetActive(ScoreManager.Instance.isHouseDeactivated);
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
        if (hoverTooltip != null)
            hoverTooltip.SetActive(true);
        float dev = 0f;
        string rName = "";
        bool isDeactivated = false;
        RectTransform tooltipRect = null;

        if (hoverTooltip != null)
            tooltipRect = hoverTooltip.GetComponent<RectTransform>();

        // 이전에 저장되어 있을 수 있는 Outline 정보를 초기화
        // 새로 마우스가 올라간 지역의 Outline을 다시 저장하기 위함
        currentRegionOutline = null;

        // 지역에 따라 이름, 발전도를 할당, Transform 변수 지정
        if (regionIndex == 1)
        {
            rName = "신도시";
            dev = ScoreManager.Instance.devUniv;
            isDeactivated = ScoreManager.Instance.isUnivDeactivated;
            currentRegionOutline = univOutline;
            if (tooltipRect != null && univHoverPos != null)
                tooltipRect.position = univHoverPos.position;
        }
        else if (regionIndex == 2)
        {
            rName = "농촌";
            dev = ScoreManager.Instance.devSilver;
            isDeactivated = ScoreManager.Instance.isSilverDeactivated;
            currentRegionOutline = silverOutline;
            if (tooltipRect != null && silverHoverPos != null)
                tooltipRect.position = silverHoverPos.position;
        }
        else if (regionIndex == 3)
        {
            rName = "지방";
            dev = ScoreManager.Instance.devIndustry;
            isDeactivated = ScoreManager.Instance.isIndustryDeactivated;
            currentRegionOutline = industryOutline;
            if (tooltipRect != null && industryHoverPos != null)
                tooltipRect.position = industryHoverPos.position;
        }
        else if (regionIndex == 4)
        {
            rName = "수도권";
            dev = ScoreManager.Instance.devHouse;
            isDeactivated = ScoreManager.Instance.isHouseDeactivated;
            currentRegionOutline = houseOutline;
            if (tooltipRect != null && houseHoverPos != null)
                tooltipRect.position = houseHoverPos.position;
        }

        if (hoverInfoText != null)
        {
            // 지역이 비활성화 상태라면 잠김 문구 표시
            if (isDeactivated)
            {
                hoverInfoText.text = $"{rName}\n[ 잠김 ]";
            }

            // 지역이 활성화 상태라면 발전도 레벨 표시
            else
            {
                string lv = dev >= 50f ? "LV 3" : (dev >= 20f ? "LV 2" : "LV 1");
                hoverInfoText.text = $"{rName}\n발전도 : {lv}";
            }
        }

        // 현재 지역의 Outline이 정상적으로 연결되어 있다면 Outline 켜기
        if (currentRegionOutline != null)
            currentRegionOutline.enabled = true;
    }

    // 마우스 오버 끝나면 꺼지게 하는 함수
    public void OnRegionHoverExit()
    {
        // 현재 마우스가 올라가 있던 지역의 Outline이 있다면 끔
        if (currentRegionOutline != null)
        {
            currentRegionOutline.enabled = false;

            // 더 이상 호버 중인 지역이 없으므로 저장된 Outline 참조를 비움
            currentRegionOutline = null;
        }
        // 호버 툴팁 오브젝트가 연결되어 있다면 화면에서 숨김
        if (hoverTooltip != null)
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

    // 게임 끝나면 최종 점수와 평가 보이게 하는 함수
    public void ShowEndingPanel(string grade, string title, float finalScore)
    {
        // 엔딩 패널 활성화  후 시간 정지
        if (GameManager.Instance != null) 
        { 
            GameManager.Instance.PlayEndingAudio(); 
        }
        endingPanel.SetActive(true);
        Time.timeScale = 0f;

        // 구역별 누적 점수 가져와 보여주기 (RoundToInt로 소수점 버리고 스트링으로 표현)
        regionUnivScoreText.text = "신도시 최종 점수 : " + Mathf.RoundToInt(ScoreManager.Instance.totalUnivScore).ToString();
        regionSilverScoreText.text = "농촌 최종 점수 : " + Mathf.RoundToInt(ScoreManager.Instance.totalSilverScore).ToString();
        regionIndustryScoreText.text = "지방 최종 점수 : " + Mathf.RoundToInt(ScoreManager.Instance.totalIndustryScore).ToString();
        regionHouseScoreText.text = "수도권 최종 점수 : " + Mathf.RoundToInt(ScoreManager.Instance.totalHouseScore).ToString();

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

    // 퀘스트 패널을 활성화하고 퀘스트 기본 정보를 표시하는 함수
    // QuestManager.GenerateNewQuest() 에서 새 퀘스트 등장 시 호출됨
    public void ShowQuestPanel(QuestDefinition quest)
    {
        if (questPanel == null) return;

        if (ResultProposal != null) ResultProposal.text = "";
        if (questTitleText != null) questTitleText.text = quest.questTitle;
        if (questDescText  != null) questDescText.text  = quest.questDesc;
        if (summaryText != null) summaryText.text = $"목표: {quest.questGoalText}\n보상: {quest.questRewardText}\n리스크: {quest.questRiskText}";

        // 뉴스 버튼 이미지를 퀘스트 알림 이미지로 교체
        if (newsButtonImage != null && newsQuestAlertSprite != null)
            newsButtonImage.sprite = newsQuestAlertSprite;

        if (acceptBtn != null) acceptBtn.SetActive(true);
        if (refuseBtn != null) refuseBtn.SetActive(true);

    }

    // 퀘스트 종료(성공/실패/거절) 시 텍스트 초기화, 버튼 비활성화, 뉴스 버튼 이미지 복구하는 함수
    // QuestManager 에서 호출됨
    public void ClearQuestPanel()
    {
        if (questTitleText != null) questTitleText.text = "";
        if (questDescText != null) questDescText.text = "";
        if (summaryText != null) summaryText.text = "";
        if (resultProposalText != null) resultProposalText.text = "";

        // 뉴스 버튼 이미지를 기본 이미지로 복구
        if (newsButtonImage != null && newsDefaultSprite != null)
            newsButtonImage.sprite = newsDefaultSprite;
    }

    // AI 힌트 패널 표시 상태를 갱신하는 함수
    // QuestManager.OnTurnStart() 에서 매 턴 호출됨
    // isActive : AI 힌트 활성화 여부 / remainingTurns : 잔여 활성화 턴 수
    public void UpdateAIHintUI(bool isActive, int remainingTurns)
    {
        if (aiHintPanel == null) return;
        aiHintPanel.SetActive(isActive);
        if (isActive && aiHintText != null)
            aiHintText.text = $"AI 힌트 활성화 ({remainingTurns}턴 남음)";
    }

    // 로그 메시지를 화면에 띄우는 함수
    public void AddPolicyLog(string logMsg)
    {
        if (policyLogText != null)
        {
            // 새로 들어온 메시지는 줄바꿈(\n)
            policyLogText.text += logMsg + "\n" + "\n";
        }
    }

    // 로그 버튼 클릭 시 로그 패널을 여는 함수
    // GameManager.LogButton()에서 호출됨
    public void OpenLogPanel()
    {
        if (logPanel == null) return;
        logPanel.SetActive(true);
    }

    // 로그 패널 닫기 버튼 클릭 시 호출되는 함수
    // 로그 패널 내부의 닫기 버튼 OnClick에 연결
    public void CloseLogPanel()
    {
        if (logPanel == null) return;
        logPanel.SetActive(false);
    }

    // 퀘스트 생성 턴(7, 14, 21, 28)에 로그 패널을 강제로 열고
    // 닫기 버튼을 delaySeconds초 동안 비활성화하는 함수
    // GameManager.StartTurn()에서 퀘스트 생성 턴에 호출됨
    public void ShowLogPanelForced(float delaySeconds = 4f)
    {
        if (logPanel == null) return;
        logPanel.SetActive(true);
        StartCoroutine(DisableLogCloseBtnTemporarily(delaySeconds));
    }

    // 로그 닫기 버튼을 일정 시간 동안 비활성화하는 코루틴
    // ShowLogPanelForced()에서만 호출됨
    private IEnumerator DisableLogCloseBtnTemporarily(float seconds)
    {
        if (logCloseBtn != null)
            logCloseBtn.GetComponent<Button>().interactable = false;

        yield return new WaitForSeconds(seconds);

        if (logCloseBtn != null)
            logCloseBtn.GetComponent<Button>().interactable = true;
    }
}
