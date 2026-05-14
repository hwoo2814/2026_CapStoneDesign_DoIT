using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class AIHintAdvisor : MonoBehaviour
{
    private const string DefaultOllamaBaseUrl = "http://210.115.229.20:1100";
    private const string OllamaChatEndpoint = "/api/chat";

    [Header("Ollama Settings")]
    [SerializeField] private string ollamaUrl = DefaultOllamaBaseUrl;
    [SerializeField] private string modelName = "gemma4:e4b";
    [SerializeField] private bool useOllamaSummary = true;

    [Header("UI Output")]
    [SerializeField] private GameObject hintPanelOverride;
    [SerializeField] private Text hintTextOverride;
    [SerializeField] private string hiddenHintText = "";
    [SerializeField] private string loadingText = "AI 힌트 분석 중...";

    [Header("Visual Recommendation UI")]
    [SerializeField] private GameObject fundingRecommendationMarker;
    [SerializeField] private GameObject youthRecommendationMarker;
    [SerializeField] private GameObject seniorRecommendationMarker;
    [SerializeField] private GameObject corpRecommendationMarker;
    [SerializeField] private bool controlButtonHighlightColors;
    [SerializeField] private Image fundingHighlightImage;
    [SerializeField] private Image youthHighlightImage;
    [SerializeField] private Image seniorHighlightImage;
    [SerializeField] private Image corpHighlightImage;
    [SerializeField] private Color recommendedColor = new Color(1f, 0.86f, 0.22f, 1f);
    [SerializeField] private Color normalColor = Color.white;

    [Header("Refresh")]
    [SerializeField] private bool alwaysShowHintForTesting = false;
    [SerializeField] private float refreshIntervalSeconds = 0.5f;
    [SerializeField] private int ollamaTimeoutSeconds = 20;

    [Header("Recommendation Rules")]
    [SerializeField] private float lowMoneyFundingThreshold = 70f;
    [SerializeField] private float lowSuccessFundingThreshold = 0.7f;
    [SerializeField] private float criticalMoneyFundingThreshold = 40f;
    [SerializeField] private float minimumMoneyAfterPolicy = 20f;
    [SerializeField] private bool logRecommendationBreakdown;

    private int lastTurn = -1;
    private int lastRemainingTurns = -1;
    private string lastGameStateKey = "";
    private float nextRefreshTime;
    private bool requestInProgress;
    private int requestSerial;
    private HintRecommendation lastRecommendation;

    [Serializable]
    private class OllamaMessage
    {
        public string role;
        public string content;
    }

    [Serializable]
    private class OllamaChatRequest
    {
        public string model;
        public OllamaMessage[] messages;
        public bool stream;
    }

    [Serializable]
    private class OllamaChatResponse
    {
        public OllamaMessage message;
        public string error;
    }

    private struct GameSnapshot
    {
        public int turn;
        public int maxTurn;
        public float money;
        public float youthAffinity;
        public float seniorAffinity;
        public float corpAffinity;
        public float devUniv;
        public float devSilver;
        public float devIndustry;
        public float devHouse;
        public bool isUnivDeactivated;
        public bool isSilverDeactivated;
        public bool isIndustryDeactivated;
        public bool isHouseDeactivated;
        public bool isHintActive;
        public int hintRemainingTurns;
    }

    private struct HintRecommendation
    {
        public int policyType;
        public string policyName;
        public string reason;
        public float projectedGain;
        public float successChance;
        public float successTurnGain;
        public float failureTurnGain;
        public float expectedScoreGain;
        public float moneyPenalty;
        public float failureRiskPenalty;
        public float reservePenalty;
        public string fallbackText;
        public string stateKey;
    }

    private void OnEnable()
    {
        ForceRefresh();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextRefreshTime)
        {
            return;
        }

        nextRefreshTime = Time.unscaledTime + Mathf.Max(0.1f, refreshIntervalSeconds);
        RefreshIfNeeded();
    }

    public void ForceRefresh()
    {
        lastGameStateKey = "";
        RefreshIfNeeded();
    }

    private void RefreshIfNeeded()
    {
        if (!TryBuildSnapshot(out GameSnapshot snapshot))
        {
            SetHintVisible(false);
            return;
        }

        string stateKey = BuildStateKey(snapshot);
        if (stateKey == lastGameStateKey)
        {
            return;
        }

        lastGameStateKey = stateKey;
        lastTurn = snapshot.turn;
        lastRemainingTurns = snapshot.hintRemainingTurns;

        lastRecommendation = BuildRecommendation(snapshot);
        ApplyFallbackHint(snapshot, lastRecommendation);

        if (snapshot.isHintActive && useOllamaSummary && !requestInProgress)
        {
            StartCoroutine(RequestOllamaHint(snapshot, lastRecommendation, ++requestSerial));
        }
    }

    private bool TryBuildSnapshot(out GameSnapshot snapshot)
    {
        snapshot = default;
        if (GameManager.Instance == null || ScoreManager.Instance == null)
        {
            return false;
        }

        ScoreManager sm = ScoreManager.Instance;
        QuestManager qm = QuestManager.Instance;

        snapshot = new GameSnapshot
        {
            turn = GameManager.Instance.CURRENT_TURN,
            maxTurn = GameManager.Instance.MAX_TURN,
            money = sm.money,
            youthAffinity = sm.youthAffinity,
            seniorAffinity = sm.seniorAffinity,
            corpAffinity = sm.corpAffinity,
            devUniv = sm.devUniv,
            devSilver = sm.devSilver,
            devIndustry = sm.devIndustry,
            devHouse = sm.devHouse,
            isUnivDeactivated = sm.isUnivDeactivated,
            isSilverDeactivated = sm.isSilverDeactivated,
            isIndustryDeactivated = sm.isIndustryDeactivated,
            isHouseDeactivated = sm.isHouseDeactivated,
            isHintActive = qm != null && qm.IsAIHintActive(),
            hintRemainingTurns = qm != null ? qm.aiHintRemainingTurns : 0
        };

        return true;
    }

    private HintRecommendation BuildRecommendation(GameSnapshot snapshot)
    {
        HintRecommendation funding = EvaluateFunding(snapshot);
        HintRecommendation youth = EvaluatePolicy(snapshot, 1);
        HintRecommendation senior = EvaluatePolicy(snapshot, 2);
        HintRecommendation corp = EvaluatePolicy(snapshot, 3);

        HintRecommendation best = youth;
        best = SelectHigherProjectedGain(best, youth);
        best = SelectHigherProjectedGain(best, senior);
        best = SelectHigherProjectedGain(best, corp);

        if (ShouldPreferFunding(snapshot, best))
        {
            best = funding;
        }

        best.stateKey = BuildStateKey(snapshot);
        best.fallbackText = BuildFallbackText(snapshot, best);
        return best;
    }

    private HintRecommendation SelectHigherProjectedGain(HintRecommendation best, HintRecommendation current)
    {
        return current.projectedGain > best.projectedGain ? current : best;
    }

    private bool ShouldPreferFunding(GameSnapshot snapshot, HintRecommendation bestPolicy)
    {
        if (bestPolicy.policyType == 0)
        {
            return false;
        }

        float maxMoney = GameManager.Instance != null ? GameManager.Instance.MAX_MONEY : 100f;
        if (snapshot.money >= maxMoney)
        {
            return false;
        }

        if (snapshot.money <= criticalMoneyFundingThreshold)
        {
            return true;
        }

        float policyCost = Mathf.Abs(GetPolicyCost(bestPolicy.policyType));
        if (snapshot.money - policyCost < minimumMoneyAfterPolicy)
        {
            return true;
        }

        if (bestPolicy.successChance < lowSuccessFundingThreshold)
        {
            return true;
        }

        return snapshot.money < lowMoneyFundingThreshold;
    }

    private HintRecommendation EvaluateFunding(GameSnapshot snapshot)
    {
        float maxMoney = GameManager.Instance != null ? GameManager.Instance.MAX_MONEY : 100f;
        if (snapshot.money >= maxMoney)
        {
            return new HintRecommendation
            {
                policyType = 0,
                policyName = "자금 확보",
                projectedGain = -999999f,
                successChance = 1f,
                reason = "자금이 이미 최대치라 이번 턴 효율이 낮습니다."
            };
        }

        float fundingMultiplier = QuestManager.Instance != null ? QuestManager.Instance.GetFundingMultiplier() : 1f;
        float expectedMoneyGain = ((17.5f * 0.5f) + (27.5f * 0.3f) + ((maxMoney - snapshot.money) * 0.2f)) * fundingMultiplier;
        float moneyPressure = 1f - Mathf.Clamp01(snapshot.money / Mathf.Max(1f, maxMoney));
        float remainingTurns = Mathf.Max(1, snapshot.maxTurn - snapshot.turn + 1);
        float projectedGain = expectedMoneyGain * (2f + moneyPressure * 8f + remainingTurns * 0.15f);

        return new HintRecommendation
        {
            policyType = 0,
            policyName = "자금 확보",
            projectedGain = projectedGain,
            successChance = 1f,
            reason = $"현재 자금이 {snapshot.money:F0}이라 다음 정책 성공률을 높이는 가치가 큽니다."
        };
    }

    private HintRecommendation EvaluatePolicy(GameSnapshot snapshot, int policyType)
    {
        float successChance = EstimateSuccessChance(snapshot.money);
        GameSnapshot success = ApplyPolicySuccess(snapshot, policyType);
        GameSnapshot failure = ApplyPolicyFailure(snapshot, policyType);

        float beforeScore = CalculateTurnScore(snapshot);
        float successGain = CalculateTurnScore(success) - beforeScore;
        float failureGain = CalculateTurnScore(failure) - beforeScore;
        float remainingTurns = Mathf.Max(1, snapshot.maxTurn - snapshot.turn + 1);
        float expectedScoreGain = Mathf.Lerp(failureGain, successGain, successChance) * remainingTurns;
        float moneySpent = Mathf.Max(0f, snapshot.money - success.money);
        float lowMoneyRisk = 1f - Mathf.Clamp01(snapshot.money / Mathf.Max(1f, GameManager.Instance.MAX_MONEY));
        float remainingMoneyAfterPolicy = snapshot.money - moneySpent;
        float reservePenalty = remainingMoneyAfterPolicy < minimumMoneyAfterPolicy
            ? (minimumMoneyAfterPolicy - remainingMoneyAfterPolicy) * 5f
            : 0f;
        float failureRiskPenalty = (1f - successChance) * lowMoneyRisk * 120f;
        float moneyPenalty = moneySpent * (0.5f + lowMoneyRisk * 2f);
        float projectedGain = expectedScoreGain - moneyPenalty - failureRiskPenalty - reservePenalty;

        return new HintRecommendation
        {
            policyType = policyType,
            policyName = GetPolicyName(policyType),
            projectedGain = projectedGain,
            successChance = successChance,
            successTurnGain = successGain,
            failureTurnGain = failureGain,
            expectedScoreGain = expectedScoreGain,
            moneyPenalty = moneyPenalty,
            failureRiskPenalty = failureRiskPenalty,
            reservePenalty = reservePenalty,
            reason = BuildPolicyReason(snapshot, policyType, successGain)
        };
    }

    private void LogRecommendationBreakdown(
        GameSnapshot snapshot,
        HintRecommendation funding,
        HintRecommendation youth,
        HintRecommendation senior,
        HintRecommendation corp,
        HintRecommendation selected)
    {
        if (!logRecommendationBreakdown)
        {
            return;
        }

        Debug.Log(
            "[AIHintAdvisor] Recommendation breakdown\n" +
            $"Turn {snapshot.turn}/{snapshot.maxTurn}, Money {snapshot.money:F0}, " +
            $"Affinity Y/S/C {snapshot.youthAffinity:F1}/{snapshot.seniorAffinity:F1}/{snapshot.corpAffinity:F1}, " +
            $"Dev Univ/Silver/Industry/House {snapshot.devUniv:F0}/{snapshot.devSilver:F0}/{snapshot.devIndustry:F0}/{snapshot.devHouse:F0}\n" +
            FormatRecommendationLogLine(funding) + "\n" +
            FormatRecommendationLogLine(youth) + "\n" +
            FormatRecommendationLogLine(senior) + "\n" +
            FormatRecommendationLogLine(corp) + "\n" +
            $"Selected: {selected.policyName}",
            this);
    }

    private string FormatRecommendationLogLine(HintRecommendation recommendation)
    {
        return $"{recommendation.policyName}: projected {recommendation.projectedGain:F1}, " +
               $"successTurn +{recommendation.successTurnGain:F1}, failureTurn {recommendation.failureTurnGain:F1}, " +
               $"expected {recommendation.expectedScoreGain:F1}, moneyPenalty {recommendation.moneyPenalty:F1}, " +
               $"failurePenalty {recommendation.failureRiskPenalty:F1}, reservePenalty {recommendation.reservePenalty:F1}, " +
               $"successChance {recommendation.successChance * 100f:F0}%";
    }

    private GameSnapshot ApplyPolicySuccess(GameSnapshot snapshot, int policyType)
    {
        GameSnapshot result = snapshot;
        result.money = Mathf.Clamp(result.money + GetPolicyCost(policyType), 0f, GameManager.Instance.MAX_MONEY);

        switch (policyType)
        {
            case 1:
                result.youthAffinity = ClampAffinity(result.youthAffinity + 1.25f);
                result.seniorAffinity = ClampAffinity(result.seniorAffinity + 0.7f);
                result.corpAffinity = ClampAffinity(result.corpAffinity + 0.7f);
                result.devUniv = Mathf.Max(0f, result.devUniv + 5f);
                result.devHouse = Mathf.Max(0f, result.devHouse + 5f);
                break;
            case 2:
                result.youthAffinity = ClampAffinity(result.youthAffinity - 0.7f);
                result.seniorAffinity = ClampAffinity(result.seniorAffinity + 1.25f);
                result.corpAffinity = ClampAffinity(result.corpAffinity - 0.7f);
                result.devSilver = Mathf.Max(0f, result.devSilver + 5f);
                result.devHouse = Mathf.Max(0f, result.devHouse + 5f);
                break;
            case 3:
                result.youthAffinity = ClampAffinity(result.youthAffinity + 0.7f);
                result.seniorAffinity = ClampAffinity(result.seniorAffinity - 0.7f);
                result.corpAffinity = ClampAffinity(result.corpAffinity + 1.25f);
                result.devIndustry = Mathf.Max(0f, result.devIndustry + 5f);
                break;
        }

        return result;
    }

    private GameSnapshot ApplyPolicyFailure(GameSnapshot snapshot, int policyType)
    {
        GameSnapshot result = snapshot;
        result.money = Mathf.Clamp(result.money + GetPolicyCost(policyType), 0f, GameManager.Instance.MAX_MONEY);

        float expectedFailAffinity = (GameManager.Instance.FAIL_RND_MIN + GameManager.Instance.FAIL_RND_MAX) * 0.5f;
        result.youthAffinity = ClampAffinity(result.youthAffinity + expectedFailAffinity);
        result.seniorAffinity = ClampAffinity(result.seniorAffinity + expectedFailAffinity);
        result.corpAffinity = ClampAffinity(result.corpAffinity + expectedFailAffinity);
        return result;
    }

    private float CalculateTurnScore(GameSnapshot snapshot)
    {
        float score = 0f;

        if (!snapshot.isUnivDeactivated)
        {
            score += CalculateRegionScore(snapshot.devUniv, snapshot.youthAffinity, snapshot.seniorAffinity, snapshot.corpAffinity, 0.7f, 0.1f, 0.2f);
        }

        if (!snapshot.isSilverDeactivated)
        {
            score += CalculateRegionScore(snapshot.devSilver, snapshot.youthAffinity, snapshot.seniorAffinity, snapshot.corpAffinity, 0.1f, 0.8f, 0.1f);
        }

        if (!snapshot.isIndustryDeactivated)
        {
            score += CalculateRegionScore(snapshot.devIndustry, snapshot.youthAffinity, snapshot.seniorAffinity, snapshot.corpAffinity, 0.2f, 0.1f, 0.7f);
        }

        if (!snapshot.isHouseDeactivated)
        {
            score += CalculateRegionScore(snapshot.devHouse, snapshot.youthAffinity, snapshot.seniorAffinity, snapshot.corpAffinity, 0.3f, 0.4f, 0.3f);
        }

        return score;
    }

    private float CalculateRegionScore(float dev, float youth, float senior, float corp, float wY, float wS, float wC)
    {
        float baseScore = dev * ((youth * wY) + (senior * wS) + (corp * wC));
        float multiplier = dev >= 50f ? 2.5f : (dev >= 20f ? 1.5f : 1.0f);
        return baseScore * multiplier;
    }

    private IEnumerator RequestOllamaHint(GameSnapshot snapshot, HintRecommendation recommendation, int serial)
    {
        requestInProgress = true;
        SetHintText(loadingText);

        OllamaChatRequest requestBody = new OllamaChatRequest
        {
            model = modelName,
            stream = false,
            messages = new[]
            {
                new OllamaMessage
                {
                    role = "system",
                    content = "당신은 도시 운영 게임의 AI 힌트 시스템이다. 입력된 게임 데이터만 사용해 한국어로 짧게 정책 추천을 작성하라. 한 줄만 작성하고, 추천 정책 이름과 이유를 포함하라."
                },
                new OllamaMessage
                {
                    role = "user",
                    content = BuildOllamaPrompt(snapshot, recommendation)
                }
            }
        };

        string json = JsonUtility.ToJson(requestBody);
        byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest request = new UnityWebRequest(BuildChatRequestUrl(), UnityWebRequest.kHttpVerbPOST))
        {
            request.uploadHandler = new UploadHandlerRaw(jsonBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = Mathf.Max(1, ollamaTimeoutSeconds);
            request.SetRequestHeader("Content-Type", "application/json");

            UnityWebRequestAsyncOperation operation;
            try
            {
                operation = request.SendWebRequest();
            }
            catch (InvalidOperationException ex)
            {
                Debug.LogWarning("AIHintAdvisor request failed to start: " + ex.Message, this);
                requestInProgress = false;
                ApplyFallbackHint(snapshot, recommendation);
                yield break;
            }

            yield return operation;
            requestInProgress = false;

            if (serial != requestSerial || !IsStillCurrent(snapshot, recommendation))
            {
                yield break;
            }

            if (request.result != UnityWebRequest.Result.Success || string.IsNullOrWhiteSpace(request.downloadHandler.text))
            {
                ApplyFallbackHint(snapshot, recommendation);
                yield break;
            }

            OllamaChatResponse response = JsonUtility.FromJson<OllamaChatResponse>(request.downloadHandler.text);
            if (response == null || response.message == null || string.IsNullOrWhiteSpace(response.message.content) || !string.IsNullOrWhiteSpace(response.error))
            {
                ApplyFallbackHint(snapshot, recommendation);
                yield break;
            }

            SetHintVisible(snapshot.isHintActive);
            SetRecommendationVisual(snapshot.isHintActive ? recommendation.policyType : -1);
            if (snapshot.isHintActive)
            {
                SetHintText(response.message.content.Trim());
            }
        }
    }

    private void ApplyFallbackHint(GameSnapshot snapshot, HintRecommendation recommendation)
    {
        SetHintVisible(snapshot.isHintActive);
        SetRecommendationVisual(snapshot.isHintActive ? recommendation.policyType : -1);
        SetHintText(snapshot.isHintActive ? recommendation.fallbackText : hiddenHintText);
    }

    private string BuildFallbackText(GameSnapshot snapshot, HintRecommendation recommendation)
    {
        int chancePercent = Mathf.RoundToInt(recommendation.successChance * 100f);
        return $"AI 힌트: {recommendation.policyName} 추천 - {recommendation.reason} 예상 성공률 {chancePercent}%, 점수 기대값 +{Mathf.RoundToInt(recommendation.projectedGain)}";
    }

    private string BuildOllamaPrompt(GameSnapshot snapshot, HintRecommendation recommendation)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("아래 추천 계산 결과를 바탕으로 UI에 표시할 한 줄 힌트를 작성하라.");
        builder.AppendLine("형식: AI 힌트: [정책명] 추천 - [짧은 이유]");
        builder.AppendLine("없는 수치를 추측하지 마라.");
        builder.AppendLine();
        builder.AppendLine($"현재 턴: {snapshot.turn}/{snapshot.maxTurn}");
        builder.AppendLine($"자금: {snapshot.money:F0}");
        builder.AppendLine($"민심: 청년 {snapshot.youthAffinity:F1}, 노년 {snapshot.seniorAffinity:F1}, 기업 {snapshot.corpAffinity:F1}");
        builder.AppendLine($"발전도: 대학가 {snapshot.devUniv:F0}, 실버타운 {snapshot.devSilver:F0}, 산업단지 {snapshot.devIndustry:F0}, 주거단지 {snapshot.devHouse:F0}");
        builder.AppendLine($"추천 정책: {recommendation.policyName}");
        builder.AppendLine($"추천 근거: {recommendation.reason}");
        builder.AppendLine($"예상 성공률: {recommendation.successChance * 100f:F0}%");
        builder.AppendLine($"점수 기대값: {recommendation.projectedGain:F0}");
        return builder.ToString();
    }

    private bool IsStillCurrent(GameSnapshot snapshot, HintRecommendation recommendation)
    {
        if (!TryBuildSnapshot(out GameSnapshot current))
        {
            return false;
        }

        return BuildStateKey(current) == recommendation.stateKey;
    }

    private string BuildStateKey(GameSnapshot snapshot)
    {
        return string.Join("|",
            snapshot.turn,
            snapshot.maxTurn,
            Mathf.RoundToInt(snapshot.money * 10f),
            Mathf.RoundToInt(snapshot.youthAffinity * 10f),
            Mathf.RoundToInt(snapshot.seniorAffinity * 10f),
            Mathf.RoundToInt(snapshot.corpAffinity * 10f),
            Mathf.RoundToInt(snapshot.devUniv * 10f),
            Mathf.RoundToInt(snapshot.devSilver * 10f),
            Mathf.RoundToInt(snapshot.devIndustry * 10f),
            Mathf.RoundToInt(snapshot.devHouse * 10f),
            snapshot.isUnivDeactivated,
            snapshot.isSilverDeactivated,
            snapshot.isIndustryDeactivated,
            snapshot.isHouseDeactivated,
            snapshot.isHintActive,
            snapshot.hintRemainingTurns);
    }

    private string BuildChatRequestUrl()
    {
        string trimmedUrl = string.IsNullOrWhiteSpace(ollamaUrl) ? DefaultOllamaBaseUrl : ollamaUrl.Trim();
        trimmedUrl = trimmedUrl.TrimEnd('/');

        if (trimmedUrl.EndsWith(OllamaChatEndpoint, StringComparison.OrdinalIgnoreCase))
        {
            return trimmedUrl;
        }

        return trimmedUrl + OllamaChatEndpoint;
    }

    private void SetHintVisible(bool isVisible)
    {
        GameObject panel = hintPanelOverride != null
            ? hintPanelOverride
            : (UIManager.Instance != null ? UIManager.Instance.aiHintPanel : null);

        if (panel != null)
        {
            panel.SetActive(isVisible);
        }

        if (!isVisible)
        {
            SetRecommendationVisual(-1);
        }
    }

    private void SetHintText(string value)
    {
        Text hintText = hintTextOverride != null
            ? hintTextOverride
            : (UIManager.Instance != null ? UIManager.Instance.aiHintText : null);

        if (hintText != null)
        {
            hintText.text = value;
        }
    }

    private void SetRecommendationVisual(int policyType)
    {
        SetMarkerActive(fundingRecommendationMarker, policyType == 0);
        SetMarkerActive(youthRecommendationMarker, policyType == 1);
        SetMarkerActive(seniorRecommendationMarker, policyType == 2);
        SetMarkerActive(corpRecommendationMarker, policyType == 3);

        if (!controlButtonHighlightColors)
        {
            return;
        }

        SetHighlightColor(fundingHighlightImage, policyType == 0);
        SetHighlightColor(youthHighlightImage, policyType == 1);
        SetHighlightColor(seniorHighlightImage, policyType == 2);
        SetHighlightColor(corpHighlightImage, policyType == 3);
    }

    private void SetMarkerActive(GameObject marker, bool isActive)
    {
        if (marker != null)
        {
            marker.SetActive(isActive);
        }
    }

    private void SetHighlightColor(Image image, bool isRecommended)
    {
        if (image != null)
        {
            image.color = isRecommended ? recommendedColor : normalColor;
        }
    }

    private string BuildPolicyReason(GameSnapshot snapshot, int policyType, float successGain)
    {
        switch (policyType)
        {
            case 1:
                return successGain >= 0f
                    ? "대학가와 주거단지 발전도가 올라 청년 중심 점수 상승이 기대됩니다."
                    : "청년 민심을 보강해 이후 대학가 점수 기반을 만들 수 있습니다.";
            case 2:
                return successGain >= 0f
                    ? "실버타운과 주거단지 발전도가 올라 노년 민심 점수 반영이 큽니다."
                    : "노년 민심을 보강해 실버타운 점수 기반을 안정화할 수 있습니다.";
            case 3:
                return successGain >= 0f
                    ? "산업단지 발전도와 기업 민심 상승이 현재 점수식에 유리합니다."
                    : "기업 민심을 보강해 산업단지 점수 기반을 만들 수 있습니다.";
            default:
                return "다음 정책 성공률을 높이기 위한 자금 확보가 우선입니다.";
        }
    }

    private string GetPolicyName(int policyType)
    {
        switch (policyType)
        {
            case 1: return "청년 정책";
            case 2: return "노년 정책";
            case 3: return "기업 정책";
            default: return "자금 확보";
        }
    }

    private float GetPolicyCost(int policyType)
    {
        switch (policyType)
        {
            case 1: return -32.5f;
            case 2: return -15f;
            case 3: return -45f;
            default: return 0f;
        }
    }

    private float EstimateSuccessChance(float money)
    {
        if (TutorialManager.Instance != null && TutorialManager.Instance.isTutorial)
        {
            return 1f;
        }

        if (money >= 100f) return 1f;
        if (money <= 0f) return 0f;
        if (money >= 80f) return 0.895f;
        if (money >= 60f) return 0.695f;
        if (money >= 40f) return 0.495f;
        if (money >= 20f) return 0.295f;
        return 0.105f;
    }

    private float ClampAffinity(float value)
    {
        if (GameManager.Instance == null)
        {
            return Mathf.Clamp(value, 0f, 10f);
        }

        return Mathf.Clamp(value, GameManager.Instance.MIN_AFFINITY, GameManager.Instance.MAX_AFFINITY);
    }
}
