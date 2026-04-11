using UnityEngine;
using System.Collections.Generic;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    public float money; //돈 게이지
    public float youthAffinity, seniorAffinity, corpAffinity; //각 민심 데이터
    public float devUniv, devSilver, devIndustry, devHouse; //각 지역 발전도

    // 지역 비활성화 기준 레벨 (임시 값 : 1 = LV1)
    public int DEACTIVATE_LEVEL_DATA = 1;

    public bool isUnivDeactivated = false;
    public bool isSilverDeactivated = false;
    public bool isIndustryDeactivated = false;
    public bool isHouseDeactivated = false;

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

        isUnivDeactivated = false;
        isSilverDeactivated = false;
        isIndustryDeactivated = false;
        isHouseDeactivated = false;

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
    
    //민심 계산 함수(각각의 민심 Clamp함수로 계산, 계산 결과를 0과 10 사이로 제한)
    public void ModifyAffinity(float youth, float senior, float corp)
    {
        int min = GameManager.Instance.MIN_AFFINITY; // 0
        int max = GameManager.Instance.MAX_AFFINITY; // 10
        
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
        // 지역이 비활성화되었는지 확인하고 true면 0으로 반환
        float scoreUniv = isUnivDeactivated ? 0f : CalculateRegionScore(devUniv, 0.7f, 0.1f, 0.2f);
        float scoreSilver = isSilverDeactivated ? 0f : CalculateRegionScore(devSilver,0.1f, 0.8f, 0.1f);
        float scoreIndustry = isIndustryDeactivated ? 0f : CalculateRegionScore(devIndustry, 0.2f, 0.1f, 0.7f);
        float scoreHouse = isHouseDeactivated ? 0f : CalculateRegionScore(devHouse, 0.3f, 0.4f, 0.3f);

        // 턴마다 구역별 점수를 더함
        totalUnivScore += scoreUniv;
        totalSilverScore += scoreSilver;
        totalIndustryScore += scoreIndustry;
        totalHouseScore += scoreHouse;

        // 최종 점수는 턴당 점수의 총합
        float turnTotalScore = scoreUniv + scoreSilver + scoreIndustry + scoreHouse;
        totalScore += turnTotalScore;
    }

    //발전도 값을 기반으로 지역 레벨(1~3)을 반환하는 함수
    private int GetDevLevel(float dev)
    {
        if (dev >= 50f) return 3;
        if (dev >= 20f) return 2;
        return 1;
    }

    // 현재 LV1 상태인 지역 이름 목록을 반환
    // GameManager의 경고(15~19턴) 및 소멸 선별(20턴)에서 공통으로 사용
    public List<string> GetLV1Regions()
    {
        List<string> lv1Regions = new List<string>();

        if (!isUnivDeactivated && GetDevLevel(devUniv) <= DEACTIVATE_LEVEL_DATA)
            lv1Regions.Add("신도시");
        if (!isSilverDeactivated && GetDevLevel(devSilver) <= DEACTIVATE_LEVEL_DATA)
            lv1Regions.Add("농촌");
        if (!isIndustryDeactivated && GetDevLevel(devIndustry) <= DEACTIVATE_LEVEL_DATA)
            lv1Regions.Add("지방");
        if (!isHouseDeactivated && GetDevLevel(devHouse) <= DEACTIVATE_LEVEL_DATA)
            lv1Regions.Add("수도권");

        return lv1Regions;
    }

    // 20턴 종료 시점에 LV1 지역 중 랜덤 1개만 소멸시키는 함수
    // GameManager.OnPlayerActionCompleted()에서 CURRENT_TURN == 20일 때 호출
    public void CheckDeactivationAtTurn20()
    {
        List<string> lv1Regions = GetLV1Regions();

        // LV1 지역이 하나도 없으면 소멸 없이 종료
        if (lv1Regions.Count == 0) return;

        // 여러 개의 LV1 지역 중 랜덤으로 1개만 선택
        int idx = Random.Range(0, lv1Regions.Count);
        string targetRegion = lv1Regions[idx];

        DeactivateSingleRegion(targetRegion);
    }

    // 지역 이름을 받아 해당 지역 1개만 소멸 처리하는 함수
    // CheckDeactivationAtTurn20()에서 선별된 1개 지역에만 호출됨
    private void DeactivateSingleRegion(string regionName)
    {
        if (regionName == "신도시") isUnivDeactivated = true;
        else if (regionName == "농촌") isSilverDeactivated = true;
        else if (regionName == "지방") isIndustryDeactivated = true;
        else if (regionName == "수도권") isHouseDeactivated = true;

        UIManager.Instance.ShowDeactivationNotice(regionName);
        UIManager.Instance.UpdateRegionImages();
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