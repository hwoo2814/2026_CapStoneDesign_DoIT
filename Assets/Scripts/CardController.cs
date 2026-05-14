using UnityEngine;

public class CardController : MonoBehaviour
{
    // 플레이어가 버튼을 눌렀을 때 아직 확정되지 않은 정책 타입을 저장
    // (0 = 자금확보, 1 = 청년, 2 = 노년, 3 = 기업 / -1 = 없음)
    public int pendingPolicyType = -1;
    public float pendingPolicyCost = 0f; // 정책 비용 (자금 차감 값)


    // 각 정책 타입에 대응하는 이름 배열, [0]=자금확보 [1]=청년 [2]=노년 [3]=기업
    private readonly string[] policyNames =
    {
        "자금 확보",
        "청년 정책",
        "노년 정책",
        "기업 정책"
    };

    // 각 정책 타입에 대응하는 설명 배열, [0]=자금확보 [1]=청년 [2]=노년 [3]=기업
    // UIManager에 explainPolicyPanel의 ExplainPolicyText에 표시될 텍스트
    private readonly string[] policyDescs =
    {
        "자금을 확보합니다.",
        "청년층을 위한 정책을 시행합니다.\n설명...",
        "노년층을 위한 정책을 시행합니다.\n설명...",
        "기업을 위한 정책을 시행합니다.\n설명..."
    };

    //청년/노년/기업 정책 함수는 pendingPolicyType과 pendingPolicyCost에 값을 저장한 뒤
    //explainPolicyPanel을 띄워 플레이어의 최종 확인을 기다림
    private readonly string[][] randomPolicyTexts =
    {
        null,
        new[]
        {
            "청년 문화패스 지급",
            "청년 월세 상담센터 운영",
            "심야 대중교통 확대",
            "청년 창업 멘토링 데이",
            "공유오피스 이용권 지원",
            "청년 커뮤니티 공간 조성",
            "청년 취업 특강 개최",
            "청년 면접 정장 대여",
            "공공 와이파이 확대",
            "청년 운동시설 야간 개방",
            "청년 생활정보 플랫폼 운영",
            "청년 동아리 지원금",
            "지역 축제 청년 부스 지원",
            "청년 교통비 포인트",
            "청년 마음상담 프로그램",
            "공공 스터디룸 개방",
            "청년 자원봉사 포인트",
            "청년 취미 클래스 지원",
            "청년 정책 공모전",
            "지역 청년 인터뷰 홍보"
        },
        new[]
        {
            "어르신 건강검진 주간",
            "경로당 냉난방비 지원",
            "동네 순환버스 증편",
            "디지털 기기 교육교실",
            "어르신 스마트폰 상담소",
            "공원 벤치 추가 설치",
            "그늘막 설치 확대",
            "어르신 일자리 안내센터",
            "무료 혈압 측정 부스",
            "경로당 프로그램 다양화",
            "노년층 문화교실 운영",
            "전통시장 장보기 도우미",
            "어르신 보행로 정비",
            "횡단보도 신호시간 연장",
            "동네 병원 안내 지도 제작",
            "경로 우대 쿠폰 확대",
            "어르신 말벗 봉사단",
            "노인 복지 민원창구 운영",
            "공원 체조 프로그램",
            "어르신 이동식 건강상담소 운영"
        },
        new[]
        {
            "중소기업 행정서류 간소화",
            "지역 상권 홍보 캠페인",
            "소상공인 배달비 지원",
            "전통시장 주말 이벤트",
            "기업 채용설명회 개최",
            "플리마켓 허가 완화",
            "간판 정비 지원사업",
            "소상공인 회계 상담",
            "지역 상품 온라인몰 입점 지원",
            "창업 절차 안내센터 운영",
            "공장 주변 도로 정비",
            "산업단지 셔틀버스 운영",
            "기업 민원 빠른 처리 주간",
            "지역 브랜드 인증마크 도입",
            "소상공인 카드수수료 지원",
            "창업 박람회 개최",
            "낡은 상가 외벽 개선 지원",
            "야간 영업구역 조명 개선",
            "지역 물류비 일부 지원",
            "기업 세무 상담의 날"
        }
    };

    private readonly int[][] randomPolicyTextOrders = new int[4][];
    private readonly int[] nextPolicyTextIndexes = new int[4];

    public void OnClickYouthPolicy() // 청년 정책
    { 
        // 튜토리얼 중이라면 청년 정책을 누르면 다음 칭찬 대사로 넘어가도록 지시
        if (TutorialManager.Instance != null && TutorialManager.Instance.isTutorial)
        {
            TutorialManager.Instance.OnYouthPolicyClicked(); 
        }
        ShowPolicyConfirmPanel(1, Random.Range(-35f, -30f)); 
    }
    public void OnClickSeniorPolicy() // 노년 정책
    { 
        ShowPolicyConfirmPanel(2, Random.Range(-20f, -10f)); 
    }
    public void OnClickCorpPolicy() // 기업 정책
    { 
        ShowPolicyConfirmPanel(3, Random.Range(-50f, -40f));
    }

    // 자금 확보 버튼
    // ShowPolicyConfirmPanel()을 통해 패널을 먼저 표시하고
    // 자금을 얼마나 채울지는 "예" 확정 후 ExecuteFunding() 안에서 처리
    public void OnClickFunding() 
    {
        ShowPolicyConfirmPanel(0, 0f);
    }

    //자금 학보 선택시 얼마큼 확보하는지 실행하여 결정하는 함수
    public void ExecuteFunding()
    {
        float currentMoney = ScoreManager.Instance.money;
        float rand = Random.value;

        if (currentMoney >= 100f) return;
        float getMoney = 0f;

        if (rand <= 0.5f) getMoney = Random.Range(10f, 25f);
        else if (rand <= 0.8f) getMoney = Random.Range(20f, 35f);
        else getMoney = GameManager.Instance.MAX_MONEY - currentMoney;

        getMoney *= QuestManager.Instance != null ? QuestManager.Instance.GetFundingMultiplier() : 1f;

        ScoreManager.Instance.ModifyMoney(getMoney);

        int turn = GameManager.Instance.CURRENT_TURN;
        UIManager.Instance.AddPolicyLog($"{turn}번째 턴 : 자금 확보 성공. 자금 추가 +{getMoney:F0}");

        GameManager.Instance.OnPlayerActionCompleted();
    }

    // 정책 버튼 클릭 시 정책 타입과 차감될 자금을 인수로 받아 저장하고 
    // UIManager에 policyNames, policyDescs 배열에서 
    // 해당 텍스트를 꺼내 패널 표시를 요청
    // policyType : 0=자금확보 1=청년 2=노년 3=기업
    // cost : 정책 실행 시 차감될 자금 (음수)
    private void ShowPolicyConfirmPanel(int policyType, float cost)
    {
        pendingPolicyType = policyType;
        pendingPolicyCost = cost;
        UIManager.Instance.ShowExplainPolicyPanel(policyNames[policyType], GetPolicyDesc(policyType));
    }

    // 청년/노년/기업 정책 설명은 현재 순번의 텍스트만 보여주고, 실제 선택 확정 시 다음 순번으로 이동
    private string GetPolicyDesc(int policyType)
    {
        if (policyType <= 0 || policyType >= randomPolicyTexts.Length)
        {
            return policyDescs[policyType];
        }

        string[] texts = randomPolicyTexts[policyType];
        if (texts == null || texts.Length == 0)
        {
            return policyDescs[policyType];
        }

        EnsurePolicyTextOrder(policyType, texts.Length);

        int currentIndex = nextPolicyTextIndexes[policyType];
        int textIndex = randomPolicyTextOrders[policyType][currentIndex];
        return texts[textIndex];
    }

    private void AdvancePolicyDesc(int policyType)
    {
        if (policyType <= 0 || policyType >= randomPolicyTexts.Length)
        {
            return;
        }

        string[] texts = randomPolicyTexts[policyType];
        if (texts == null || texts.Length == 0)
        {
            return;
        }

        EnsurePolicyTextOrder(policyType, texts.Length);

        nextPolicyTextIndexes[policyType]++;
        if (nextPolicyTextIndexes[policyType] >= texts.Length)
        {
            nextPolicyTextIndexes[policyType] = 0;
        }
    }

    private void EnsurePolicyTextOrder(int policyType, int textCount)
    {
        if (randomPolicyTextOrders[policyType] != null)
        {
            return;
        }

        int[] order = new int[textCount];
        for (int i = 0; i < order.Length; i++)
        {
            order[i] = i;
        }

        for (int i = order.Length - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            int temp = order[i];
            order[i] = order[randomIndex];
            order[randomIndex] = temp;
        }

        randomPolicyTextOrders[policyType] = order;
    }

    // 정책 버튼을 선택했을때 전달받은 각각의 cost와 policyType를 받아 실행하는 함수
    public void ProcessPolicy(float cost, int policyType)
    {
        AdvancePolicyDesc(policyType);

        float currentMoney = ScoreManager.Instance.money;
        bool isSuccess = CheckSuccess(currentMoney); // 성공, 실패 체크

        ScoreManager.Instance.ModifyMoney(cost); // 돈 차감
        int turn = GameManager.Instance.CURRENT_TURN; // 로그에 몇 번째 턴인지 적기 위한 변수
        string logMessage = $"{turn}번째 턴 : 정책이 "; // 로그 앞문장 미리쓰기

        if (isSuccess)
        {
            logMessage += "성공 하였습니다.\n(";

            if (policyType == 1) // 청년정책 성공시
            {
                // 변화량을 로그에 적기 위해 랜덤값을 dY, dS, dC에 저장
                float dY = Random.Range(1f, 1.5f); 
                float dS = Random.Range(0.5f, 0.9f); 
                float dC = Random.Range(0.5f, 0.9f);

                ScoreManager.Instance.ModifyAffinity(dY, dS, dC);
                ScoreManager.Instance.ModifyDev(5f, 0, 0, 5f);
                
                logMessage += $"청년 민심+{dY:F1}, 노년 민심+{dS:F1}, 기업 민심+{dC:F1} / 대학가 발전도+5, 주거단지 발전도+5)";
            }
            else if (policyType == 2) // 노년정책 성공시
            {
                float dY = Random.Range(-0.9f, -0.5f); 
                float dS = Random.Range(1f, 1.5f); 
                float dC = Random.Range(-0.9f, -0.5f);

                ScoreManager.Instance.ModifyAffinity(dY, dS, dC);
                ScoreManager.Instance.ModifyDev(0, 5f, 0, 5f);

                logMessage += $"청년 민심+{dY:F1}, 노년 민심+{dS:F1}, 기업 민심+{dC:F1} / 실버타운 발전도+5, 주거단지 발전도+5)";
            }
            else if (policyType == 3) // 기업정책 성공시
            {
                float dY = Random.Range(0.5f, 0.9f); 
                float dS = Random.Range(-0.9f, -0.5f); 
                float dC = Random.Range(1f, 1.5f);

                ScoreManager.Instance.ModifyAffinity(dY, dS, dC);
                ScoreManager.Instance.ModifyDev(0, 0, 5f, 0);

                logMessage += $"청년 민심+{dY:F1}, 노년 민심+{dS:F1}, 기업 민심+{dC:F1} / 산업단지 발전도+5)";
            }
        }
        else // 실패 시 -0.5 ~ -1.5 사이값으로 하락, 발전도 증가 없음
        {
            float fMin = GameManager.Instance.FAIL_RND_MIN; 
            float fMax = GameManager.Instance.FAIL_RND_MAX; 
            
            float dY = Random.Range(fMin, fMax); 
            float dS = Random.Range(fMin, fMax); 
            float dC = Random.Range(fMin, fMax);
            ScoreManager.Instance.ModifyAffinity(dY, dS, dC);
            
            logMessage += $"실패 하였습니다. (청년 민심{dY:F1}, 노년 민심{dS:F1}, 기업 민심{dC:F1})";
        }

        UIManager.Instance.AddPolicyLog(logMessage);
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnPolicyUsed(policyType);
        }
        GameManager.Instance.OnPlayerActionCompleted();
    }

    // 현재 돈 게이지를 기준으로 정책의 성공 확률을 계산하는 함수
    private bool CheckSuccess(float money)
    {
        // 튜토리얼 중이라면 돈이나 확률에 상관없이 무조건 100% 성공
        if (TutorialManager.Instance != null && TutorialManager.Instance.isTutorial) return true;

        if (money >= 100f) return true;
        if (money <= 0f) return false;

        float chance = 0f; // 자금의 양에 따라 확률을 저장하는 변수
        
        if (money >= 80f) chance = Random.Range(80f, 100f);
        else if (money >= 60f) chance = Random.Range(60f, 80f);
        else if (money >= 40f) chance = Random.Range(40f, 60f);
        else if (money >= 20f) chance = Random.Range(20f, 40f);
        else chance = Random.Range(1f, 21f);

        return Random.Range(1f, 100f) <= chance;
    }
}
