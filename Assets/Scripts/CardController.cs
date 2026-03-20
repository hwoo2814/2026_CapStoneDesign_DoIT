using UnityEngine;

public class CardController : MonoBehaviour
{
    // 각 정책 카드를 선택했을 때 실행하는 함수
    public void OnClickYouthPolicy() // 청년 정책
    { 
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
        if (currentMoney >= 100f) return; // 100이면 선택 불가

        float rand = Random.value;

        if (rand <= 0.5f) // 50% 확률
        {
            ScoreManager.Instance.ModifyMoney(Random.Range(10f, 25f));
        }
        else if (rand <= 0.8f) // 30% 확률
        {
            ScoreManager.Instance.ModifyMoney(Random.Range(20f, 35f));
        }
        else // 20% 확률
        {
            ScoreManager.Instance.money = GameManager.Instance.MAX_MONEY; // 100으로 SET
        }

        GameManager.Instance.OnPlayerActionCompleted();
    }

    // 정책 버튼을 선택했을때 전달받은 cost와 policyType를 받아 실행하는 함수
    private void ProcessPolicy(float cost, int policyType)
    {
        float currentMoney = ScoreManager.Instance.money;
        bool isSuccess = CheckSuccess(currentMoney); // 성공, 실패 체크

        ScoreManager.Instance.ModifyMoney(cost); // 돈 차감

        if (isSuccess)
        {
            if (policyType == 1) // 청년정책 성공시
            {
                ScoreManager.Instance.ModifyAffinity(Random.Range(1f, 1.5f), Random.Range(0.5f, 0.9f), Random.Range(0.5f, 0.9f));
                ScoreManager.Instance.ModifyDev(5f, 0, 0, 5f);
            }
            else if (policyType == 2) // 노년정책 성공시
            {
                ScoreManager.Instance.ModifyAffinity(Random.Range(-0.9f, -0.5f), Random.Range(1f, 1.5f), Random.Range(-0.9f, -0.5f));
                ScoreManager.Instance.ModifyDev(0, 5f, 0, 5f);
            }
            else if (policyType == 3) // 기업정책 성공시
            {
                ScoreManager.Instance.ModifyAffinity(Random.Range(0.5f, 0.9f), Random.Range(-0.9f, -0.5f), Random.Range(1f, 1.5f));
                ScoreManager.Instance.ModifyDev(0, 0, 5f, 0);
            }
        }
        else // 실패 시 -0.5 ~ -1.5 사이값으로 하락, 발전도 증가 없음
        {
            float fMin = GameManager.Instance.FAIL_RND_MIN; 
            float fMax = GameManager.Instance.FAIL_RND_MAX; 
            ScoreManager.Instance.ModifyAffinity(Random.Range(fMin, fMax), Random.Range(fMin, fMax), Random.Range(fMin, fMax));
        }

        GameManager.Instance.OnPlayerActionCompleted();
    }

    // 현재 돈 게이지를 기준으로 정책의 성공 확률을 계산하는 함수
    private bool CheckSuccess(float money)
    {
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