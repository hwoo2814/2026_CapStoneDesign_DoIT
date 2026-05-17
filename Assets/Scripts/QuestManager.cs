using UnityEngine;
using System.Collections.Generic;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;

    // 현재 진행 중인 퀘스트 (null이면 활성 퀘스트 없음)
    private QuestDefinition activeQuest = null;

    // 퀘스트 자동 생성 기준 턴 목록 (7, 14, 21, 28)
    private readonly int[] questGenerationTurns = { 7, 14, 21, 28 };

    // 이미 사용된 퀘스트 ID 집합 (완료·실패한 퀘스트는 재등장하지 않음)
    private HashSet<int> usedQuestIds = new HashSet<int>();

    // 현재 퀘스트가 생성된 턴
    private int questStartTurn = 0;

    // 현재 퀘스트의 만료 턴 (생성 턴 + 7 = 다음 생성 턴)
    private int questExpiryTurn = 0;

    // 목표 진행 추적 변수
    // 연속 조건 달성 턴 카운터
    // MoneyAboveForConsecutiveTurns / SingleAffinity / MultiAffinity / SingleAffinityAndMoney 에서 사용
    private int consecutiveTurnsMet = 0;

    // 정책 사용 누적 횟수 카운터 (PolicyCountWithinTurns 에서 사용)
    private int policyUseCount = 0;

    // 안건 수락 여부 플래그 (AcceptProposalWithinTurns 에서 사용)
    private bool proposalAccepted = false;

    // 퀘스트 시작 이후 경과된 턴 수
    // AcceptProposalWithinTurns 의 제한 턴 초과 조기 실패 판정에 사용
    private int questElapsedTurns = 0;

    // 플레이어의 퀘스트 결정 상태 추적 (미결정 / 수락 / 거절)
    private enum QuestDecisionState { Pending, Accepted, Rejected }
    private QuestDecisionState decisionState = QuestDecisionState.Pending;

    // 지속 효과(디버프/버프) 추적 변수
    // 자금 획득률 감소 디버프 잔여 턴 수 (거점 국립대 지원 실패 리스크)
    public int fundingDebuffRemainingTurns = 0;

    // 자금 획득률 감소 비율 (0.1 = 10% 감소)
    public float fundingDebuffRate = 0f;

    // AI 힌트 활성화 잔여 턴 수 (빅데이터 센터 구축 성공 보상)
    public int aiHintRemainingTurns = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // 자금 획득 시 적용할 배율을 반환하는 함수
    // CardController.ExecuteFunding() 에서 실제 획득량에 곱함
    // 디버프 없음 → 1.0f (정상), 디버프 있음 → (1 - debuffRate)
    public float GetFundingMultiplier()
    {
        if (fundingDebuffRemainingTurns > 0)
            return 1f - fundingDebuffRate;
        return 1f;
    }

    // AI 힌트가 현재 활성 상태인지 반환
    // UIManager.UpdateAIHintUI() 에서 패널 표시 여부에 사용
    public bool IsAIHintActive()
    {
        return aiHintRemainingTurns > 0;
    }

    // 현재 활성 퀘스트가 있는지 여부를 반환
    public bool HasActiveQuest()
    {
        return activeQuest != null;
    }

    // 외부에서 현재 활성 퀘스트 데이터에 접근하기 위한 접근자
    public QuestDefinition GetActiveQuest()
    {
        return activeQuest;
    }

    // 턴 시작 시 GameManager.StartTurn() 에서 호출
    // 퀘스트 생성 주기 체크, 만료 퀘스트 실패 처리, 신규 퀘스트 배정을 수행
    // currentTurn : 방금 시작된 턴 번호
    public void OnTurnStart(int currentTurn)
    {
        // 튜토리얼 중이면 퀘스트 시스템 전체를 건너뜀
        if (TutorialManager.Instance != null && TutorialManager.Instance.isTutorial) return;

        // 지속 효과 잔여 턴 감소
        if (fundingDebuffRemainingTurns > 0) fundingDebuffRemainingTurns--;
        if (aiHintRemainingTurns > 0) aiHintRemainingTurns--;

        // 퀘스트 생성 턴(7, 14, 21, 28)인지 확인
        bool isGenerationTurn = System.Array.IndexOf(questGenerationTurns, currentTurn) >= 0;
        if (isGenerationTurn)
        {
            if (activeQuest != null && currentTurn >= questExpiryTurn)
            {
                if (decisionState == QuestDecisionState.Accepted)
                {
                    // 수락 후 목표 미달성 상태로 기한 도달 → 실패 처리 및 리스크 적용
                    FailCurrentQuest();
                }
                else
                {
                    // 미수락/거절 상태로 기한 도달 → 리스크 없이 종료
                    UIManager.Instance.AddPolicyLog($"[퀘스트 종료] : {activeQuest.questTitle}");
                    UIManager.Instance.ClearQuestPanel();
                    activeQuest = null;
                    ResetProgressCounters();
                }
            }
            if (activeQuest == null) GenerateNewQuest(currentTurn);
        }
        UIManager.Instance.UpdateAIHintUI(IsAIHintActive(), aiHintRemainingTurns);
    }

    // 턴 종료(플레이어 행동 완료) 후 GameManager.OnPlayerActionCompleted() 에서 호출
    // 현재 활성 퀘스트의 목표 달성 여부를 검사하고 성공, 실패를 처리
    public void OnTurnEnd()
    {
        if (activeQuest == null) return;

        questElapsedTurns++;

        // 목표 달성 여부 확인
        bool isComplete = CheckGoalCompletion(activeQuest.goal);
        if (isComplete)
        {
            CompleteCurrentQuest();
            return;
        }

        if (GameManager.Instance.CURRENT_TURN >= questExpiryTurn)
        {
            if (decisionState == QuestDecisionState.Accepted)
            {
                FailCurrentQuest();
            }
            else
            {
                UIManager.Instance.AddPolicyLog($"[퀘스트 종료] : {activeQuest.questTitle}");
                UIManager.Instance.ClearQuestPanel();
                activeQuest = null;
                ResetProgressCounters();
            }
        }
    }

    // 정책 사용 시 CardController.ProcessPolicy() 에서 호출
    // PolicyCountWithinTurns 타입 퀘스트의 정책 사용 횟수를 누적
    // policyType : 1=청년, 2=노년, 3=기업
    public void OnPolicyUsed(int policyType)
    {
        if (activeQuest == null) return;
        if (decisionState != QuestDecisionState.Accepted) return;
        if (activeQuest.goal.goalType != QuestGoalType.PolicyCountWithinTurns) return;
        if (activeQuest.goal.policyType != policyType) return;

        policyUseCount++;
    }

    // 수락 버튼 클릭 시 GameManager.AcceptButten() 에서 호출
    // AcceptProposalWithinTurns 타입 퀘스트에서 수락 완료 플래그를 세움
    public void OnQuestAccepted()
    {
        if (activeQuest == null) return;
        // 거절 후 재수락 시도 차단
        if (decisionState == QuestDecisionState.Rejected) return;

        // 기본적으로는 퀘스트를 수락 상태로 전환
        decisionState = QuestDecisionState.Accepted;

        // 10, 11번 퀘스트는 "3턴 이내 수락" 자체가 성공 조건
        // 따라서 이 타입은 턴 종료(OnTurnEnd)까지 기다리지 말고
        // 수락 버튼을 누르는 즉시 성공 처리
        if (activeQuest.goal.goalType == QuestGoalType.AcceptProposalWithinTurns)
        {
            // questElapsedTurns는 OnTurnEnd()가 호출될 때마다 1씩 증가
            // 턴 종료 때 증가하므로 0,1,2까지만 허용
            if (questElapsedTurns < activeQuest.goal.requiredTurns)
            {
                // 수락 성공 조건을 만족하면 true
                proposalAccepted = true;
                // 즉시 성공 처리
                CompleteCurrentQuest();
                // 이미 성공 처리까지 끝났으므로 함수 종료
                return;
            }
        }
    }

    // 수락 버튼 클릭 시 GameManager.AcceptButten() 에서 호출
    public void OnQuestRefused()
    {
        if (activeQuest == null) return;
        // 수락 후 거절 시도 차단
        if (decisionState == QuestDecisionState.Accepted) return;

        decisionState = QuestDecisionState.Rejected;
    }

    // 기존 함수명을 참조하는 곳이 있을 경우를 대비한 호환 wrapper
    public void OnProposalAccepted()
    {
        OnQuestAccepted();
    }

    // 게임 재시작 시 GameManager.Start() 에서 호출
    // 퀘스트 시스템 전체 초기화
    public void InitData()
    {
        activeQuest = null;
        usedQuestIds.Clear();
        questStartTurn = 0;
        questExpiryTurn = 0;
        fundingDebuffRemainingTurns = 0;
        fundingDebuffRate = 0f;
        aiHintRemainingTurns = 0;
        if (UIManager.Instance != null)
        {
            // 게임 시작시 퀘스트 부분 공백
            UIManager.Instance.ClearQuestPanel();
            UIManager.Instance.UpdateAIHintUI(false, 0);
        }
        ResetProgressCounters();
    }

    // 사용하지 않은 퀘스트 중 랜덤으로 1개를 선택해 활성화
    private void GenerateNewQuest(int currentTurn)
    {
        List<QuestDefinition> available = new List<QuestDefinition>();
        foreach (var q in QuestDatabase.AllQuests)
        {
            if (!usedQuestIds.Contains(q.questId))
                available.Add(q);
        }

        if (available.Count == 0)
        {
            UIManager.Instance.AddPolicyLog($"{currentTurn}번째 턴 : 사용 가능한 퀘스트가 없습니다.");
            return;
        }

        // 랜덤 선택 후 사용 목록에 등록 (재등장 방지)
        int idx = Random.Range(0, available.Count);
        activeQuest = available[idx];
        usedQuestIds.Add(activeQuest.questId);

        questStartTurn  = currentTurn;
        questExpiryTurn = currentTurn + 7;
        ResetProgressCounters();

        UIManager.Instance.AddPolicyLog($"[퀘스트 등장] : {activeQuest.questTitle}\n뉴스 버튼을 누른다음 업무 버튼을 누르면 내용을 볼 수 있습니다.");
        UIManager.Instance.ShowQuestPanel(activeQuest);
    }

    // 퀘스트 성공: 보상 + 리스크 모두 적용
    private void CompleteCurrentQuest()
    {
        GameManager.Instance.questSuccessAudioSource.PlayOneShot(GameManager.Instance.questSuccessAudioSource.clip);

        string title = activeQuest.questTitle;
        ApplyEffect(activeQuest.reward);
        ApplyEffect(activeQuest.risk);

        UIManager.Instance.AddPolicyLog($"[퀘스트 성공] : {title}\n보상 및 리스크 모두 적용");
        UIManager.Instance.ClearQuestPanel(); // 퀘스트 텍스트 초기화

        activeQuest = null;
        ResetProgressCounters();
    }

    // 퀘스트 실패: 리스크만 적용
    private void FailCurrentQuest()
    {
        GameManager.Instance.questFailedAudioSource.PlayOneShot(GameManager.Instance.questFailedAudioSource.clip);

        string title = activeQuest.questTitle;
        ApplyEffect(activeQuest.risk);

        UIManager.Instance.AddPolicyLog($"[퀘스트 실패] : {title}\n리스크만 적용");
        UIManager.Instance.ClearQuestPanel(); // 퀘스트 텍스트 초기화

        activeQuest = null;
        ResetProgressCounters();
    }

    // 퀘스트 목표 조건을 검사하여 달성 여부(bool)를 반환
    private bool CheckGoalCompletion(QuestGoalData goal)
    {
        ScoreManager sm = ScoreManager.Instance;

        // 10, 11번(AcceptProposalWithinTurns)을 제외한 퀘스트는
        // 수락 상태여야만 성공/실패 판정 진입
        if (goal.goalType != QuestGoalType.AcceptProposalWithinTurns && decisionState != QuestDecisionState.Accepted)
        {
            return false;
        }

        switch (goal.goalType)
        {
            case QuestGoalType.MoneyAboveForConsecutiveTurns:
                if (sm.money >= goal.targetValue) consecutiveTurnsMet++;
                else consecutiveTurnsMet = 0;
                return consecutiveTurnsMet >= goal.requiredTurns;

            case QuestGoalType.AffinityAboveOnce:
                return GetAffinityValue(goal.affinityType) >= goal.targetValue;

            case QuestGoalType.AffinitySumAndMoneyOnce:
            {
                float sum = 0f;
                if (goal.affinityTypes != null)
                    foreach (int t in goal.affinityTypes) sum += GetAffinityValue(t);
                return sum >= goal.targetValue && sm.money >= goal.targetValue2;
            }

            case QuestGoalType.MultiAffinityForConsecutiveTurns:
            {
                bool allMet = true;
                if (goal.affinityTypes != null)
                    foreach (int t in goal.affinityTypes)
                        if (GetAffinityValue(t) < goal.targetValue) { allMet = false; break; }
                if (allMet) consecutiveTurnsMet++;
                else consecutiveTurnsMet = 0;
                return consecutiveTurnsMet >= goal.requiredTurns;
            }

            case QuestGoalType.SingleAffinityAndMoneyForConsecutiveTurns:
                if (GetAffinityValue(goal.affinityType) >= goal.targetValue && sm.money >= goal.targetValue2)
                    consecutiveTurnsMet++;
                else
                    consecutiveTurnsMet = 0;
                return consecutiveTurnsMet >= goal.requiredTurns;

            case QuestGoalType.PolicyCountWithinTurns:
                return policyUseCount >= goal.requiredCount;

            case QuestGoalType.SingleAffinityForConsecutiveTurns:
                if (GetAffinityValue(goal.affinityType) >= goal.targetValue) consecutiveTurnsMet++;
                else consecutiveTurnsMet = 0;
                return consecutiveTurnsMet >= goal.requiredTurns;

            case QuestGoalType.AcceptProposalWithinTurns:
                // 수락 여부(proposalAccepted) + 3턴 이내 수락 여부(questElapsedTurns) 동시 검사
                // 3턴 초과 후 수락하면 proposalAccepted=true여도 성공하지 않음
                return proposalAccepted && questElapsedTurns <= goal.requiredTurns;
        }
        return false;
    }

    // QuestEffectData 를 받아 ScoreManager 및 QuestManager 수치에 반영
    private void ApplyEffect(QuestEffectData effect)
    {
        if (effect == null) return;
        ScoreManager sm = ScoreManager.Instance;

        // 자금 변화
        if (effect.moneyChange != 0f)
            sm.ModifyMoney(effect.moneyChange);

        // 민심 변화
        if (effect.youthAffinityChange != 0f || effect.seniorAffinityChange != 0f || effect.corpAffinityChange != 0f)
            sm.ModifyAffinity(effect.youthAffinityChange, effect.seniorAffinityChange, effect.corpAffinityChange);

        // 개별 지역 발전도 레벨 변화
        if (effect.devUnivLevelChange != 0) sm.IncreaseDevLevel(0, effect.devUnivLevelChange);
        if (effect.devSilverLevelChange != 0) sm.IncreaseDevLevel(1, effect.devSilverLevelChange);
        if (effect.devIndustryLevelChange != 0) sm.IncreaseDevLevel(2, effect.devIndustryLevelChange);
        if (effect.devHouseLevelChange != 0) sm.IncreaseDevLevel(3, effect.devHouseLevelChange);

        // 전체 지역 발전도 일괄 변화
        if (effect.allDevChange != 0f)
            sm.ModifyDev(effect.allDevChange, effect.allDevChange, effect.allDevChange, effect.allDevChange);

        // 자금 획득률 디버프 적용
        if (effect.fundingDebuffTurns > 0)
        {
            fundingDebuffRemainingTurns = effect.fundingDebuffTurns + 1;
            fundingDebuffRate = effect.fundingDebuffRate;
        }

        // AI 힌트 활성화
        if (effect.activateAIHint)
        {
            aiHintRemainingTurns = effect.aiHintTurns + 1;
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateAIHintUI(true, effect.aiHintTurns);
            }
        }
    }

    // 민심 타입 인덱스(0=청년, 1=노년, 2=기업)로 실제 민심 값을 반환
    private float GetAffinityValue(int affinityType)
    {
        switch (affinityType)
        {
            case 0: return ScoreManager.Instance.youthAffinity;
            case 1: return ScoreManager.Instance.seniorAffinity;
            case 2: return ScoreManager.Instance.corpAffinity;
        }
        return 0f;
    }

    // 목표 진행 관련 카운터를 모두 초기화
    private void ResetProgressCounters()
    {
        consecutiveTurnsMet = 0;
        policyUseCount = 0;
        proposalAccepted = false;
        questElapsedTurns = 0;
        decisionState = QuestDecisionState.Pending;
    }
}
