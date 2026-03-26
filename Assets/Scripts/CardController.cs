using UnityEngine;

public class CardController : MonoBehaviour
{
    // 각 정책 카드를 선택했을 때 실행하는 함수
    public void OnClickYouthPolicy() // 청년 정책
    { 
        // 튜토리얼 중이라면 청년 정책을 누르면 다음 칭찬 대사로 넘어가도록 지시
        if (TutorialManager.Instance != null && TutorialManager.Instance.isTutorial)
        {
            TutorialManager.Instance.OnYouthPolicyClicked(); 
        }
        ProcessPolicy(Random.Range(-35f, -30f), 1); 
    }
    public void OnClickSeniorPolicy() // 노년 정책
    { 
        ProcessPolicy(Random.Range(-20f, -10f), 2); 
    }
    public void OnClickCorpPolicy() // 기업 정책
    { 
        ProcessPolicy(Random.Range(-50f, -40f), 3);
    }

    //자금 확보 카드를 선택했을 때 실행되는 함수
    public void OnClickFunding() 
    {
        float currentMoney = ScoreManager.Instance.money;
        float rand = Random.value;

        if (currentMoney >= GameManager.Instance.MAX_MONEY) return; // 100이면 선택 불가
        float getMoney = 0f; //자금이 얼마나 올랐는지 로그에 적기위한 변수

        if (rand <= 0.5f) getMoney = Random.Range(10f, 25f); // 50% 확률
        else if (rand <= 0.8f) getMoney = Random.Range(26f, 35f); // 30% 확률
        else getMoney = GameManager.Instance.MAX_MONEY - currentMoney; // 20% 확률

        ScoreManager.Instance.ModifyMoney(getMoney); // 돈 더하기
        
        // 자금 확보 로그 메시지를 만들어 UIManager로 전송
        int turn = GameManager.Instance.CURRENT_TURN;
        UIManager.Instance.AddPolicyLog($"{turn}번째 턴 : 자금 확보 성공 +{getMoney:F0}");

        GameManager.Instance.OnPlayerActionCompleted();
    }

    // 정책 버튼을 선택했을때 전달받은 각각의 cost와 policyType를 받아 실행하는 함수
    private void ProcessPolicy(float cost, int policyType)
    {
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
                
                logMessage += $"청년 +{dY:F1}, 노년 +{dS:F1}, 기업 +{dC:F1} / 대학가 +5, 주거단지 +5)";
            }
            else if (policyType == 2) // 노년정책 성공시
            {
                float dY = Random.Range(-0.9f, -0.5f); 
                float dS = Random.Range(1f, 1.5f); 
                float dC = Random.Range(-0.9f, -0.5f);

                ScoreManager.Instance.ModifyAffinity(dY, dS, dC);
                ScoreManager.Instance.ModifyDev(0, 5f, 0, 5f);

                logMessage += $"청년 {dY:F1}, 노년 +{dS:F1}, 기업 {dC:F1} / 실버타운 +5, 주거단지 +5)";
            }
            else if (policyType == 3) // 기업정책 성공시
            {
                float dY = Random.Range(0.5f, 0.9f); 
                float dS = Random.Range(-0.9f, -0.5f); 
                float dC = Random.Range(1f, 1.5f);

                ScoreManager.Instance.ModifyAffinity(dY, dS, dC);
                ScoreManager.Instance.ModifyDev(0, 0, 5f, 0);

                logMessage += $"청년 +{dY:F1}, 노년 {dS:F1}, 기업 +{dC:F1} / 산업단지 +5)";
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
            
            logMessage += $"실패 하였습니다. (청년 {dY:F1}, 노년 {dS:F1}, 기업 {dC:F1})";
        }

        UIManager.Instance.AddPolicyLog(logMessage);
        GameManager.Instance.OnPlayerActionCompleted();
    }

    // 현재 돈 게이지를 기준으로 정책의 성공 확률을 계산하는 함수
    private bool CheckSuccess(float money)
    {
        // 튜토리얼 중이라면 돈이나 확률에 상관없이 무조건 100% 성공
        if (TutorialManager.Instance != null && TutorialManager.Instance.isTutorial) return true;

        if (money >= 100f) return true;
        if (money <= 0f) return false;

        float chance = 0f;
        
        if (money >= 80f) chance = Random.Range(80f, 100f);
        else if (money >= 60f) chance = Random.Range(60f, 80f);
        else if (money >= 40f) chance = Random.Range(40f, 60f);
        else if (money >= 20f) chance = Random.Range(20f, 40f);
        else chance = Random.Range(1f, 21f);

        return Random.Range(1f, 100f) <= chance;
    }
}