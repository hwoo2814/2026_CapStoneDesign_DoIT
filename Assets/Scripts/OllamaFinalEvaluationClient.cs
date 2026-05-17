using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class OllamaFinalEvaluationClient : MonoBehaviour
{
    private const string DefaultOllamaBaseUrl = "http://210.115.229.20:1100";
    private const string OllamaChatEndpoint = "/api/chat";
    private const int MaxEvaluationCharacterCount = 199;

    public static OllamaFinalEvaluationClient Instance { get; private set; }

    [Header("Ollama Settings")]
    [SerializeField] private string ollamaUrl = DefaultOllamaBaseUrl;
    [SerializeField] private string modelName = "gemma4:e4b";

    [Header("Generation")]
    [SerializeField] private bool autoGenerateWhenEndingPanelOpens = true;
    [SerializeField] private int paragraphCount = 3;

    [Header("UI Output")]
    [SerializeField] private TMP_Text evaluationOutputText;
    [SerializeField] private Text legacyEvaluationOutputText;
    [SerializeField] private Button generateButton;

    [Header("Prompt")]
    [TextArea(5, 12)]
    [SerializeField] private string systemPrompt =
        "당신은 도시 운영 게임의 최종 평가 AI다.\n" +
        "입력된 최종 게임 데이터만 사용해 한국어 평가문을 작성하라.\n" +
        "없는 사실을 만들지 말고, 점수와 지역 발전도와 정책 선택 횟수의 균형을 근거로 평가하라.\n" +
        "지금 말해주는 단어들은 모두 대체어로 말해라. 대학교를 신도시로, 실버타운을 농촌으로, 산업지구를 지방으로, 주거구역을 수도권이라는 단어들로 바꿔말해라. 절대 잊지 말 것\n"  +
        "플레이어를 시장님이라고 부르며, 장점과 아쉬운 점을 모두 간결하게 말하라.";

    private int fundingPolicyCount;
    private int youthPolicyCount;
    private int seniorPolicyCount;
    private int corpPolicyCount;
    private bool endingObserved;
    private bool requestInProgress;

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
        public string model;
        public OllamaMessage message;
        public string error;
        public bool done;
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Update()
    {
        if (!autoGenerateWhenEndingPanelOpens || endingObserved || UIManager.Instance == null)
        {
            return;
        }

        if (UIManager.Instance.endingPanel != null && UIManager.Instance.endingPanel.activeInHierarchy)
        {
            endingObserved = true;
            GenerateFinalEvaluation();
        }
    }

    // 기존 Confirm 버튼 연결을 유지하기 위한 호환용 함수
    // 실제 정책 선택 횟수 기록은 GameManager.OnClickConfirmPolicy()에서 처리한다.
    public void RecordAndConfirmPolicy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnClickConfirmPolicy();
        }
    }

    public void RecordPolicySelection(int policyType)
    {
        switch (policyType)
        {
            case 0:
                fundingPolicyCount++;
                break;
            case 1:
                youthPolicyCount++;
                break;
            case 2:
                seniorPolicyCount++;
                break;
            case 3:
                corpPolicyCount++;
                break;
        }
    }

    public void GenerateFinalEvaluation()
    {
        if (requestInProgress)
        {
            return;
        }

        if (ScoreManager.Instance == null)
        {
            SetOutputText("최종 평가를 만들 수 없습니다. ScoreManager가 없습니다.");
            return;
        }

        StartCoroutine(SendEvaluationRequest(BuildFinalEvaluationData()));
    }

    private FinalEvaluationData BuildFinalEvaluationData()
    {
        ScoreManager score = ScoreManager.Instance;

        return new FinalEvaluationData
        {
            finalScore = score.totalScore,
            totalUnivScore = score.totalUnivScore,
            totalSilverScore = score.totalSilverScore,
            totalIndustryScore = score.totalIndustryScore,
            totalHouseScore = score.totalHouseScore,
            devUniv = score.devUniv,
            devSilver = score.devSilver,
            devIndustry = score.devIndustry,
            devHouse = score.devHouse,
            youthAffinity = score.youthAffinity,
            seniorAffinity = score.seniorAffinity,
            corpAffinity = score.corpAffinity,
            fundingPolicyCount = fundingPolicyCount,
            youthPolicyCount = youthPolicyCount,
            seniorPolicyCount = seniorPolicyCount,
            corpPolicyCount = corpPolicyCount,
            mostSelectedPolicies = GetPolicyExtremes(true),
            leastSelectedPolicies = GetPolicyExtremes(false),
            paragraphCount = Mathf.Max(1, paragraphCount)
        };
    }

    private IEnumerator SendEvaluationRequest(FinalEvaluationData data)
    {
        requestInProgress = true;
        SetButtonInteractable(false);
        SetOutputText("AI 최종 평가 생성 중...");

        OllamaChatRequest requestBody = new OllamaChatRequest
        {
            model = modelName,
            stream = false,
            messages = new[]
            {
                new OllamaMessage { role = "system", content = systemPrompt },
                new OllamaMessage { role = "user", content = BuildUserPrompt(data) }
            }
        };

        string json = JsonUtility.ToJson(requestBody);
        byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest request = new UnityWebRequest(BuildChatRequestUrl(), UnityWebRequest.kHttpVerbPOST))
        {
            request.uploadHandler = new UploadHandlerRaw(jsonBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            UnityWebRequestAsyncOperation operation;
            try
            {
                operation = request.SendWebRequest();
            }
            catch (InvalidOperationException ex)
            {
                requestInProgress = false;
                SetButtonInteractable(true);
                SetOutputText("요청 시작 실패\n" + ex.Message);
                Debug.LogException(ex);
                yield break;
            }

            yield return operation;

            requestInProgress = false;
            SetButtonInteractable(true);

            if (request.result != UnityWebRequest.Result.Success)
            {
                SetOutputText("Ollama 요청 실패\n" + request.error);
                yield break;
            }

            OllamaChatResponse response = JsonUtility.FromJson<OllamaChatResponse>(request.downloadHandler.text);
            if (response == null)
            {
                SetOutputText("응답 파싱에 실패했습니다.");
                yield break;
            }

            if (!string.IsNullOrWhiteSpace(response.error))
            {
                SetOutputText("Ollama 오류\n" + response.error);
                yield break;
            }

            if (response.message == null || string.IsNullOrWhiteSpace(response.message.content))
            {
                SetOutputText("생성된 최종 평가가 없습니다.");
                yield break;
            }

            SetOutputText(TrimEvaluationText(response.message.content));
        }
    }

    private string BuildUserPrompt(FinalEvaluationData data)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("다음 최종 게임 데이터를 바탕으로 평가문만 작성하라.");
        builder.AppendLine($"정확히 {data.paragraphCount}문단으로 작성하라.");
        builder.AppendLine($"전체 평가문은 공백 포함 {MaxEvaluationCharacterCount}자 이하로 작성하라.");
        builder.AppendLine("글자 수 제한을 넘길 것 같으면 문단 내용을 줄이고, 모든 문장은 반드시 마침표로 끝내라.");
        builder.AppendLine("제목, 표, 불릿 없이 본문만 작성하라.");
        builder.AppendLine("최종점수, 각 지역 발전도, 가장 많이 선택한 정책, 가장 적게 선택한 정책을 반드시 언급하라.");
        builder.AppendLine();
        builder.AppendLine("[최종 점수]");
        builder.AppendLine($"최종점수: {Mathf.RoundToInt(data.finalScore)}");
        builder.AppendLine($"대학가 누적 점수: {Mathf.RoundToInt(data.totalUnivScore)}");
        builder.AppendLine($"실버타운 누적 점수: {Mathf.RoundToInt(data.totalSilverScore)}");
        builder.AppendLine($"산업단지 누적 점수: {Mathf.RoundToInt(data.totalIndustryScore)}");
        builder.AppendLine($"주거단지 누적 점수: {Mathf.RoundToInt(data.totalHouseScore)}");
        builder.AppendLine();
        builder.AppendLine("[지역 발전도]");
        builder.AppendLine($"대학가 발전도: {data.devUniv:F0} ({GetDevLevelText(data.devUniv)})");
        builder.AppendLine($"실버타운 발전도: {data.devSilver:F0} ({GetDevLevelText(data.devSilver)})");
        builder.AppendLine($"산업단지 발전도: {data.devIndustry:F0} ({GetDevLevelText(data.devIndustry)})");
        builder.AppendLine($"주거단지 발전도: {data.devHouse:F0} ({GetDevLevelText(data.devHouse)})");
        builder.AppendLine();
        builder.AppendLine("[최종 민심]");
        builder.AppendLine($"청년 민심: {data.youthAffinity:F1}");
        builder.AppendLine($"노년 민심: {data.seniorAffinity:F1}");
        builder.AppendLine($"기업 민심: {data.corpAffinity:F1}");
        builder.AppendLine();
        builder.AppendLine("[정책 선택 횟수]");
        builder.AppendLine($"자금 확보: {data.fundingPolicyCount}회");
        builder.AppendLine($"청년 정책: {data.youthPolicyCount}회");
        builder.AppendLine($"노년 정책: {data.seniorPolicyCount}회");
        builder.AppendLine($"기업 정책: {data.corpPolicyCount}회");
        builder.AppendLine($"가장 많이 선택한 정책: {data.mostSelectedPolicies}");
        builder.AppendLine($"가장 적게 선택한 정책: {data.leastSelectedPolicies}");
        return builder.ToString();
    }

    private string TrimEvaluationText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        string trimmed = value.Trim();
        if (trimmed.Length <= MaxEvaluationCharacterCount)
        {
            return trimmed;
        }

        string limited = trimmed.Substring(0, MaxEvaluationCharacterCount).TrimEnd();
        int sentenceEndIndex = limited.LastIndexOfAny(new[] { '.', '!', '?', '。', '\n' });
        if (sentenceEndIndex >= 0)
        {
            return limited.Substring(0, sentenceEndIndex + 1).TrimEnd();
        }

        return limited;
    }

    private string BuildChatRequestUrl()
    {
        string trimmedUrl = string.IsNullOrWhiteSpace(ollamaUrl)
            ? DefaultOllamaBaseUrl
            : ollamaUrl.Trim();

        trimmedUrl = trimmedUrl.TrimEnd('/');

        if (trimmedUrl.EndsWith(OllamaChatEndpoint, StringComparison.OrdinalIgnoreCase))
        {
            return trimmedUrl;
        }

        return trimmedUrl + OllamaChatEndpoint;
    }

    private string GetPolicyExtremes(bool highest)
    {
        int target = highest
            ? Mathf.Max(fundingPolicyCount, youthPolicyCount, seniorPolicyCount, corpPolicyCount)
            : Mathf.Min(fundingPolicyCount, youthPolicyCount, seniorPolicyCount, corpPolicyCount);

        StringBuilder builder = new StringBuilder();
        AppendPolicyIfCountMatches(builder, "자금 확보", fundingPolicyCount, target);
        AppendPolicyIfCountMatches(builder, "청년 정책", youthPolicyCount, target);
        AppendPolicyIfCountMatches(builder, "노년 정책", seniorPolicyCount, target);
        AppendPolicyIfCountMatches(builder, "기업 정책", corpPolicyCount, target);

        return builder.Length > 0 ? $"{builder} ({target}회)" : "없음";
    }

    private void AppendPolicyIfCountMatches(StringBuilder builder, string policyName, int count, int target)
    {
        if (count != target)
        {
            return;
        }

        if (builder.Length > 0)
        {
            builder.Append(", ");
        }

        builder.Append(policyName);
    }

    private string GetDevLevelText(float dev)
    {
        if (dev >= 50f) return "LV 3";
        if (dev >= 20f) return "LV 2";
        return "LV 1";
    }

    private void SetOutputText(string value)
    {
        if (evaluationOutputText != null)
        {
            evaluationOutputText.text = value;
        }

        if (legacyEvaluationOutputText != null)
        {
            legacyEvaluationOutputText.text = value;
        }
    }

    private void SetButtonInteractable(bool canInteract)
    {
        if (generateButton != null)
        {
            generateButton.interactable = canInteract;
        }
    }

    private struct FinalEvaluationData
    {
        public float finalScore;
        public float totalUnivScore;
        public float totalSilverScore;
        public float totalIndustryScore;
        public float totalHouseScore;
        public float devUniv;
        public float devSilver;
        public float devIndustry;
        public float devHouse;
        public float youthAffinity;
        public float seniorAffinity;
        public float corpAffinity;
        public int fundingPolicyCount;
        public int youthPolicyCount;
        public int seniorPolicyCount;
        public int corpPolicyCount;
        public string mostSelectedPolicies;
        public string leastSelectedPolicies;
        public int paragraphCount;
    }
}
