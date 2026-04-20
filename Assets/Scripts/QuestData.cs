using System.Collections.Generic;
using UnityEngine;

// 퀘스트 목표 판정 방식을 구분하는 enum형
public enum QuestGoalType
{
    MoneyAboveForConsecutiveTurns, // N턴 연속 자금 X 이상 유지
    AffinityAboveOnce, // 특정 민심 X 이상 1회 달성
    AffinitySumAndMoneyOnce, // 두 민심 합계 X + 자금 Y 이상 동시 달성
    MultiAffinityForConsecutiveTurns, // 여러 민심 각각 X 이상 N턴 연속 유지
    SingleAffinityAndMoneyForConsecutiveTurns, // 민심 X + 자금 Y 이상 N턴 연속 유지
    PolicyCountWithinTurns, // 퀘스트 유효 기간 내 특정 정책 N회 사용
    SingleAffinityForConsecutiveTurns, // 단일 민심 X 이상 N턴 연속 유지
    AcceptProposalWithinTurns, // N턴 이내 안건 수락 버튼 클릭
}

// 퀘스트 목표 조건 데이터를 담는 클래스
[System.Serializable]
public class QuestGoalData
{
    public QuestGoalType goalType; // 목표 판정 방식
    public float targetValue; // 주요 목표 수치 (자금량, 민심 값 등)
    public float targetValue2; // 보조 목표 수치 (AND 조건의 두 번째 값)
    public int requiredTurns; // 조건 유지 턴 수 또는 제한 턴 수
    public int requiredCount; // 필요 행동 횟수 (정책 사용 횟수 등)

    // PolicyCountWithinTurns 전용: 대상 정책 타입 (1=청년, 2=노년, 3=기업)
    public int policyType;

    // AffinityAboveOnce / SingleAffinity 계열 전용: 대상 민심 타입 (0=청년, 1=노년, 2=기업)
    public int affinityType;

    // AffinitySumAndMoneyOnce / MultiAffinityForConsecutiveTurns 전용:
    // 합산 또는 개별 확인할 민심 타입 배열 (예: [0, 2] = 청년 + 기업)
    public int[] affinityTypes;
}

// 퀘스트 보상 또는 리스크 수치를 담는 클래스
[System.Serializable]
public class QuestEffectData
{
    public float moneyChange; // 자금 변화량 (+/-)
    public float youthAffinityChange; // 청년 민심 변화량
    public float seniorAffinityChange; // 노년 민심 변화량
    public float corpAffinityChange; // 기업 민심 변화량

    // 지역 발전도 레벨 변화량 (+1 = 레벨업 1단계, 0이면 변화 없음)
    public int devUnivLevelChange; // 신도시
    public int devSilverLevelChange; // 농촌
    public int devIndustryLevelChange; // 지방
    public int devHouseLevelChange; // 수도권

    // 전체 지역 발전도 일괄 변화량 (그린벨트 해제 리스크 등)
    public float allDevChange;

    // 자금 획득률 감소 디버프 (거점 국립대 지원 실패 리스크 등)
    public int fundingDebuffTurns; // 디버프 지속 턴 수 (0이면 미적용)
    public float fundingDebuffRate; // 감소 비율 (0.1 = 10% 감소)

    // AI 힌트 활성화 보상 (빅데이터 센터 구축 보상)
    public bool activateAIHint; // AI 힌트 활성화 여부
    public int aiHintTurns; // AI 힌트 지속 턴 수
}

// 하나의 퀘스트 전체 정보를 담는 클래스
[System.Serializable]
public class QuestDefinition
{
    public int questId; // 퀘스트 고유 ID (0부터 시작하는 인덱스)
    public string questTitle; // 퀘스트 제목
    public string questDesc; // 퀘스트 배경 설명 텍스트
    public string questGoalText; // 목표 조건 표시용 텍스트 (UI)
    public string questRewardText; // 보상 표시용 텍스트 (UI)
    public string questRiskText; // 리스크 표시용 텍스트 (UI)
    public QuestGoalData goal; // 목표 조건 데이터
    public QuestEffectData reward; // 성공 시 지급할 보상 데이터
    public QuestEffectData risk; // 성공/실패 관계없이 부여될 리스크 데이터
}

// 모든 퀘스트 정의를 보관하는 정적 데이터베이스 클래스
public static class QuestDatabase
{
    public static readonly List<QuestDefinition> AllQuests = new List<QuestDefinition>
    {
        // ── 퀘스트 0: GTX 광역철도 착공 ──
        new QuestDefinition
        {
            questId = 0,
            questTitle = "GTX 광역철도 착공",
            questDesc = "출퇴근 시간에 쏟아지는 민원을 더 이상 방치할 수 없습니다. 수도권의 지도를 바꿀 결단이 필요합니다.",
            questGoalText = "3턴 연속 자금 50 이상 유지하기",
            questRewardText = "수도권 LV+1, 청년 민심 +5",
            questRiskText = "자금 -40",
            goal = new QuestGoalData
            {
                goalType = QuestGoalType.MoneyAboveForConsecutiveTurns,
                targetValue = 50f,
                requiredTurns = 3
            },
            reward = new QuestEffectData { devHouseLevelChange = 1, youthAffinityChange = 5f },
            risk  = new QuestEffectData { moneyChange = -40f }
        },

        // ── 퀘스트 1: 거점 국립대 지원 ──
        new QuestDefinition
        {
            questId = 1,
            questTitle = "거점 국립대 지원",
            questDesc = "지역의 미래인 인재들이 수도권으로 유출되고 있습니다.\n대학 인프라를 혁신해 그들을 붙잡아야 합니다.",
            questGoalText = "청년 민심 7 이상 달성",
            questRewardText = "지방 LV+1, 청년/노년 민심 +5",
            questRiskText = "3턴 동안 자금 획득률 10% 감소, 기업 민심 -3",
            goal = new QuestGoalData
            {
                goalType = QuestGoalType.AffinityAboveOnce,
                affinityType = 0,
                targetValue = 7f
            },
            reward = new QuestEffectData { devIndustryLevelChange = 1, youthAffinityChange = 5f, seniorAffinityChange = 5f },
            risk  = new QuestEffectData { fundingDebuffTurns = 3, fundingDebuffRate = 0.1f, corpAffinityChange = -3f }
        },

        // ── 퀘스트 2: 대형 복합 쇼핑몰 ──
        new QuestDefinition
        {
            questId = 2,
            questTitle = "대형 복합 쇼핑몰",
            questDesc = "신도시의 성장이 정체되어 있습니다.\n랜드마크 쇼핑몰 유치로 기업의 투자와 젊은 소비층을 동시에 잡으십시오.",
            questGoalText = "청년 + 기업 민심 합계 10 이상 AND 자금 50 이상 보유",
            questRewardText = "신도시 LV+1, 청년 민심 +4, 기업 민심 +5",
            questRiskText = "자금 -40",
            goal = new QuestGoalData
            {
                goalType = QuestGoalType.AffinitySumAndMoneyOnce,
                affinityTypes = new int[] { 0, 2 },
                targetValue  = 10f,
                targetValue2 = 50f
            },
            reward = new QuestEffectData { devUnivLevelChange = 1, youthAffinityChange = 4f, corpAffinityChange = 5f },
            risk  = new QuestEffectData { moneyChange = -40f }
        },

        // ── 퀘스트 3: 광역 물류 센터 ──
        new QuestDefinition
        {
            questId = 3,
            questTitle = "광역 물류 센터",
            questDesc = "농촌의 유휴 부지를 첨단 물류 거점으로 전환해야 합니다. 기업들은 준비가 되었습니다.\n주민만 설득하십시오.",
            questGoalText = "기업, 노년 민심 각각 3 이상 2턴 연속 유지",
            questRewardText = "농촌 LV+1, 청년 민심 +3, 기업 민심 +3",
            questRiskText = "노년 민심 -3",
            goal = new QuestGoalData
            {
                goalType = QuestGoalType.MultiAffinityForConsecutiveTurns,
                affinityTypes = new int[] { 2, 1 },
                targetValue  = 3f,
                requiredTurns = 2
            },
            reward = new QuestEffectData { devSilverLevelChange = 1, youthAffinityChange = 3f, corpAffinityChange = 3f },
            risk  = new QuestEffectData { seniorAffinityChange = -3f }
        },

        // ── 퀘스트 4: 청년 안심 주택 공급 ──
        new QuestDefinition
        {
            questId = 4,
            questTitle = "청년 안심 주택 공급",
            questDesc = "높은 집값에 청년들이 절망하고 있습니다.\n역세권 부지에 저렴한 주택을 대량 공급해 희망을 줘야 합니다.",
            questGoalText = "2턴 연속 청년 민심 5 이상 AND 자금 40 이상 유지",
            questRewardText = "청년 민심 +8",
            questRiskText = "자금 -30, 노년 민심 -2, 기업 민심 -2",
            goal = new QuestGoalData
            {
                goalType = QuestGoalType.SingleAffinityAndMoneyForConsecutiveTurns,
                affinityType = 0,
                targetValue  = 5f,
                targetValue2 = 40f,
                requiredTurns = 2
            },
            reward = new QuestEffectData { youthAffinityChange = 8f },
            risk  = new QuestEffectData { moneyChange = -30f, seniorAffinityChange = -2f, corpAffinityChange = -2f }
        },

        // ── 퀘스트 5: 어르신 무상 교통 ──
        new QuestDefinition
        {
            questId = 5,
            questTitle = "어르신 무상 교통",
            questDesc = "나이가 들었다고 이동권까지 제한받아선 안 됩니다.\n버스 무상 이용은 노년층의 가장 간절한 숙원입니다.",
            questGoalText = "퀘스트 기간 내 노년 정책 3회 선택",
            questRewardText = "노년 민심 +10",
            questRiskText = "자금 -25, 청년 민심 -3",
            goal = new QuestGoalData
            {
                goalType = QuestGoalType.PolicyCountWithinTurns,
                policyType    = 2,
                requiredCount = 3
            },
            reward = new QuestEffectData { seniorAffinityChange = 10f },
            risk  = new QuestEffectData { moneyChange = -25f, youthAffinityChange = -3f }
        },

        // ── 퀘스트 6: 산업단지 규제 완화 ──
        new QuestDefinition
        {
            questId = 6,
            questTitle = "산업단지 규제 완화",
            questDesc = "과도한 환경 규제가 기업의 발목을 잡고 있습니다.\n규제를 풀어야 투자가 살아나고 세수가 확보됩니다.",
            questGoalText = "2턴 연속 기업 민심 5 이상 유지",
            questRewardText = "기업 민심 +7",
            questRiskText = "청년 민심 -3, 노년 민심 -3",
            goal = new QuestGoalData
            {
                goalType = QuestGoalType.SingleAffinityForConsecutiveTurns,
                affinityType = 2,
                targetValue  = 5f,
                requiredTurns = 2
            },
            reward = new QuestEffectData { corpAffinityChange = 7f },
            risk  = new QuestEffectData { youthAffinityChange = -3f, seniorAffinityChange = -3f }
        },

        // ── 퀘스트 7: 청년 자산 형성 지원 ──
        new QuestDefinition
        {
            questId = 7,
            questTitle = "청년 자산 형성 지원",
            questDesc = "열심히 일해도 목돈 마련이 힘든 시대입니다.\n시 예산으로 청년들의 저축에 힘을 보태주십시오.",
            questGoalText = "퀘스트 기간 내 청년 정책 3회 선택",
            questRewardText = "청년 민심 +8",
            questRiskText = "자금 -40, 노년 민심 -2",
            goal = new QuestGoalData
            {
                goalType = QuestGoalType.PolicyCountWithinTurns,
                policyType    = 1,
                requiredCount = 3
            },
            reward = new QuestEffectData { youthAffinityChange = 8f },
            risk  = new QuestEffectData { moneyChange = -40f, seniorAffinityChange = -2f }
        },

        // ── 퀘스트 8: 현대식 노인 복지관 ──
        new QuestDefinition
        {
            questId = 8,
            questTitle = "현대식 노인 복지관",
            questDesc = "갈 곳 없는 노인들이 늘고 있습니다.\n최신 시설을 갖춘 복지 거점을 확충해 노후의 질을 높여주십시오.",
            questGoalText = "2턴 연속 노년 민심 5 이상 유지",
            questRewardText = "노년 민심 +10",
            questRiskText = "자금 -35, 청년 민심 -3",
            goal = new QuestGoalData
            {
                goalType = QuestGoalType.SingleAffinityForConsecutiveTurns,
                affinityType = 1,
                targetValue  = 5f,
                requiredTurns = 2
            },
            reward = new QuestEffectData { seniorAffinityChange = 10f },
            risk  = new QuestEffectData { moneyChange = -35f, youthAffinityChange = -3f }
        },

        // ── 퀘스트 9: 쓰레기 소각장 건립 ──
        new QuestDefinition
        {
            questId = 9,
            questTitle = "쓰레기 소각장 건립",
            questDesc = "처리 시설 포화로 도시가 쓰레기에 묻힐 위기입니다.\n강력한 행정력으로 소각장 입지를 확정해야 합니다.",
            questGoalText = "퀘스트 기간 내 기업 정책 3회 선택",
            questRewardText = "기업 민심 +5, 자금 +50",
            questRiskText = "청년/노년 민심 각 -5",
            goal = new QuestGoalData
            {
                goalType = QuestGoalType.PolicyCountWithinTurns,
                policyType    = 3,
                requiredCount = 3
            },
            reward = new QuestEffectData { corpAffinityChange = 5f, moneyChange = 50f },
            risk  = new QuestEffectData { youthAffinityChange = -5f, seniorAffinityChange = -5f }
        },

        // ── 퀘스트 10: 지방 공기업 민영화 ──
        new QuestDefinition
        {
            questId = 10,
            questTitle = "지방 공기업 민영화",
            questDesc = "공기업의 방만한 경영이 시 재정을 좀먹고 있습니다.\n지분 매각을 통해 즉시 가용 예산을 확보해야 합니다.",
            questGoalText = "3턴 이내 안건 수락",
            questRewardText = "자금 +80",
            questRiskText = "청년/노년 민심 각 -2",
            goal = new QuestGoalData
            {
                goalType = QuestGoalType.AcceptProposalWithinTurns,
                requiredTurns = 3
            },
            reward = new QuestEffectData { moneyChange = 80f },
            risk  = new QuestEffectData { youthAffinityChange = -2f, seniorAffinityChange = -2f }
        },

        // ── 퀘스트 11: 그린벨트 대규모 해제 ──
        new QuestDefinition
        {
            questId = 11,
            questTitle = "그린벨트 대규모 해제",
            questDesc = "금고가 비었습니다. 보존 가치가 낮은 그린벨트를 해제하고 택지로 분양해 막대한 재원을 마련하십시오.",
            questGoalText = "3턴 이내 안건 수락",
            questRewardText = "자금 +80, 기업 민심 +2",
            questRiskText = "전체 발전도 -10",
            goal = new QuestGoalData
            {
                goalType = QuestGoalType.AcceptProposalWithinTurns,
                requiredTurns = 3
            },
            reward = new QuestEffectData { moneyChange = 80f, corpAffinityChange = 2f },
            risk  = new QuestEffectData { allDevChange = -10f }
        },

        // ── 퀘스트 12: 빅데이터 센터 구축 ──
        new QuestDefinition
        {
            questId = 12,
            questTitle = "빅데이터 센터 구축",
            questDesc = "데이터 기반의 과학적 행정을 실현해야 합니다.\n시 전역의 정보를 통합 관리할 서버실이 필요합니다.",
            questGoalText = "3턴 연속 자금 50 이상 유지",
            questRewardText = "5턴간 AI 힌트 활성화",
            questRiskText = "자금 -50",
            goal = new QuestGoalData
            {
                goalType = QuestGoalType.MoneyAboveForConsecutiveTurns,
                targetValue  = 50f,
                requiredTurns = 3
            },
            reward = new QuestEffectData { activateAIHint = true, aiHintTurns = 5 },
            risk  = new QuestEffectData { moneyChange = -50f }
        },
    };
}
