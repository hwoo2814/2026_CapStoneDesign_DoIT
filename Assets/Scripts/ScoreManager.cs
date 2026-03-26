using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    public float money; //돈 게이지
    public float youthAffinity, seniorAffinity, corpAffinity; //각 민심 데이터
    public float devUniv, devSilver, devIndustry, devHouse; //각 지역 발전도
    
    public float totalUnivScore, totalSilverScore, totalIndustryScore, totalHouseScore; //구역별 누적 점수
    public float totalScore; // 총 누적 점수

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    //초기 데이터값으로 셋팅
    public void InitData()
    {
        money = GameManager.Instance.START_MONEY;
        youthAffinity = GameManager.Instance.START_AFFINITY;
        seniorAffinity = GameManager.Instance.START_AFFINITY;
        corpAffinity = GameManager.Instance.START_AFFINITY;
        
        // 각 점수 0으로 초기화
        devUniv = 0f; 
        devSilver = 0f; 
        devIndustry = 0f; 
        devHouse = 0f;

        totalUnivScore = 0f;
        totalSilverScore = 0f;
        totalIndustryScore = 0f;
        totalHouseScore = 0f;

        totalScore = 0f;
    }

    //돈 계산 함수(amount가 들어와서 Clamp함수로 계산, 계산 결과를 0과 100 사이로 제한)
    public void ModifyMoney(float amount)
    {
        money = Mathf.Clamp(money + amount, 0f, GameManager.Instance.MAX_MONEY);
    }
    
    //민심 계산 함수(각각의 민심 Clamp함수로 계산, 계산 결과를 -1과 3 사이로 제한)
    public void ModifyAffinity(float youth, float senior, float corp)
    {
        int min = GameManager.Instance.MIN_AFFINITY; // -1
        int max = GameManager.Instance.MAX_AFFINITY; // 3
        
        youthAffinity = Mathf.Clamp(youthAffinity + youth, min, max);
        seniorAffinity = Mathf.Clamp(seniorAffinity + senior, min, max);
        corpAffinity = Mathf.Clamp(corpAffinity + corp, min, max);

        UIManager.Instance.UpdateAffinityUI();
    }

    //발전도 계산 함수
    public void ModifyDev(float univ, float silver, float industry, float house)
    {
        // 돌발 이벤트로 발전도가 음수가 될 수 있어 Mathf.Max(두 값 중 큰 값을 반환)을 사용해 음수 방지
        devUniv = Mathf.Max(0f, devUniv + univ);
        devSilver = Mathf.Max(0f, devSilver + silver);
        devIndustry = Mathf.Max(0f, devIndustry + industry);
        devHouse = Mathf.Max(0f, devHouse + house);
    }

    //해당 턴의 획득 점수를 계산하여 누적 총점에 더하는 함수
    public void CalculateTurnScore()
    {
        float scoreUniv = CalculateRegionScore(devUniv, 0.7f, 0.1f, 0.2f);
        float scoreSilver = CalculateRegionScore(devSilver, 0.1f, 0.8f, 0.1f);
        float scoreIndustry = CalculateRegionScore(devIndustry, 0.2f, 0.1f, 0.7f);
        float scoreHouse = CalculateRegionScore(devHouse, 0.3f, 0.4f, 0.3f);

        // 턴마다 구역별 점수를 더함
        totalUnivScore += scoreUniv;
        totalSilverScore += scoreSilver;
        totalIndustryScore += scoreIndustry;
        totalHouseScore += scoreHouse;

        // 최종 점수는 턴당 점수의 총합
        float turnTotalScore = scoreUniv + scoreSilver + scoreIndustry + scoreHouse;
        totalScore += turnTotalScore;
    }

    // 각 지역의 민심 데이터 점수 계산 함수
    public float CalculateRegionScore(float dev, float wY, float wS, float wC)
    {
        float baseScore = dev * ((youthAffinity * wY) + (seniorAffinity * wS) + (corpAffinity * wC));
        float multiplier = dev >= 50f ? 2.5f : (dev >= 20f ? 1.5f : 1.0f);
        // 지역 레벨에 따른 점수 배율
        // 발전도(dev)가 50 이상이면 x2.5 (LV 3)
        // 발전도(dev)가 20 이상이면 x1.5 (LV 2)
        // 그 외(0~19)는 x1.0 (LV 1)
        return baseScore * multiplier;
    }

    //게임이 끝나 최종 점수에 따른 결과를 EndingPanel로 출력하는 함수
    public void GameEnding()
    {
        string grade = "F";
        string title = "탄핵 위기";

        if (totalScore >= 12000f) 
        { 
            grade = "S"; 
            title = "전설적인 성군";
        }
        else if (totalScore >= 8000f) 
        { 
            grade = "A"; 
            title = "유능한 행정가";
        }
        else if (totalScore >= 5000f) 
        { 
            grade = "B"; 
            title = "안정적인 시장";
        }
        else if (totalScore >= 2000f) 
        { 
            grade = "C"; 
            title = "평범한 관료";
        }
        else if (totalScore >= 500f) 
        { 
            grade = "D"; 
            title = "위태로운 초보";
        }

        // UI로 보이게 함
        UIManager.Instance.ShowEndingPanel(grade, title, totalScore);
    }
}