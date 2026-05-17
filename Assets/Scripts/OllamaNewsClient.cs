using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

public class OllamaNewsClient : MonoBehaviour
{
    private const string DefaultOllamaBaseUrl = "http://210.115.229.20:1100";
    private const string OllamaChatEndpoint = "/api/chat";
    private const string AcceptedProposalText = "\uBCF8 \uC81C\uC548\uC11C\uB97C \uCC44\uD0DD\uD558\uACA0\uC2B5\uB2C8\uB2E4.";
    private const string RefusedProposalText = "\uC81C\uC2DC\uD558\uC2E0 \uC81C\uC548\uC11C\uB294 \uCC44\uD0DD\uC774 \uC5B4\uB824\uC6B8\uAC70 \uAC19\uC2B5\uB2C8\uB2E4.";
    private const string PlaceholderText = "\uC5C6\uC74C";
    private const string QuestAcceptedText = "\uC218\uB77D";
    private const string QuestRefusedText = "\uAC70\uC808";
    private const string QuestInProgressText = "\uC9C4\uD589 \uC911";
    private const string QuestClosedText = "\uC885\uB8CC";
    private const string QuestSuccessText = "\uC131\uACF5";
    private const string QuestFailureText = "\uC2E4\uD328";
    private const string RegionNewTown = "\uC2E0\uB3C4\uC2DC";
    private const string RegionRural = "\uB18D\uCD0C";
    private const string RegionLocal = "\uC9C0\uBC29";
    private const string RegionCapital = "\uC218\uB3C4\uAD8C";
    private const string MultipleRegionsText = "\uBCF5\uC218 \uAD6C\uC5ED";

    public static OllamaNewsClient Instance { get; private set; }
    private static bool hasWarmedUpThisSession;

    [Header("Ollama Settings")]
    [SerializeField] private string ollamaUrl = DefaultOllamaBaseUrl;
    [SerializeField] private string modelName = "gemma4:e4b";
    [SerializeField] private bool useGameManagerData = true;

    [Header("Warmup")]
    [SerializeField] private bool warmupModelOnStart = true;
    [SerializeField] private float warmupDelaySeconds = 0.5f;

    [Header("News Rules")]
    [SerializeField] private bool autoGenerateOnGameEvents = true;
    [SerializeField] private float affinityThreshold = 7f;

    [Header("Prompt")]
    [TextArea(4, 10)]
    [SerializeField] private string systemPrompt =
        "\uB2F9\uC2E0\uC740 \uB3C4\uC2DC \uC6B4\uC601 \uAC8C\uC784 \uC18D AI \uB274\uC2A4 \uAE30\uC790\uB2E4.\n" +
        "\uC785\uB825\uB41C \uD604\uC7AC \uAC8C\uC784 \uB370\uC774\uD130\uB9CC \uC0AC\uC6A9\uD574 \uD55C\uAD6D\uC5B4 \uB274\uC2A4 \uBCF8\uBB38\uC744 \uC791\uC131\uD558\uB77C.\n" +
        "\uACFC\uC7A5, \uCD94\uCE21, \uC5C6\uB294 \uC0AC\uC2E4 \uCD94\uAC00\uB97C \uAE08\uC9C0\uD55C\uB2E4.\n" +
        "\uC815\uD655\uD788 \uC9C0\uC815\uB41C \uC904 \uC218\uB9CC \uC791\uC131\uD558\uACE0 \uC81C\uBAA9\uC740 \uC4F0\uC9C0 \uB9C8\uB77C.\n" +
        "\uD018\uC2A4\uD2B8\uC640 \uAD6C\uC5ED \uBCC0\uD654, \uBBFC\uC2EC\uC774 \uB192\uC740 \uACC4\uCE35\uC758 \uBC18\uC751\uC744 \uC911\uC2EC\uC73C\uB85C \uAC04\uACB0\uD558\uAC8C \uBCF4\uB3C4\uD558\uB77C.";

    [SerializeField] private int lineCount = 3;

    [Header("UI Output")]
    [SerializeField] private TMP_Text newsOutputText;
    [SerializeField] private Text legacyNewsOutputText;
    [SerializeField] private Button generateButton;
    [SerializeField] private TMP_FontAsset tmpKoreanFontOverride;
    [SerializeField] private bool applyRuntimeKoreanFontToTmp = true;
    [SerializeField] private bool allowOsFontFallbackForTmp;

    private readonly Queue<GameNewsData> pendingNewsQueue = new Queue<GameNewsData>();

    private bool isRequestInProgress;
    private bool observersInitialized;
    private bool hasAttemptedRuntimeFontSetup;
    private bool lastYouthAboveThreshold;
    private bool lastSeniorAboveThreshold;
    private bool lastCorpAboveThreshold;
    private int lastPolicyLogLength;
    private string lastProposalText = string.Empty;
    private string lastWarningText = string.Empty;
    private QuestDefinition lastObservedQuest;
    private RegionStateSnapshot lastRegionState;
    private TMP_FontAsset runtimeKoreanFontAsset;

    public enum NewsTriggerType
    {
        Manual,
        QuestAccepted,
        QuestRefused,
        QuestSucceeded,
        QuestFailed,
        AffinityThresholdReached,
        RegionLevelUp,
        RegionDeactivationWarning,
        RegionDeactivated
    }

    [Serializable]
    public class GameNewsData
    {
        public NewsTriggerType triggerType = NewsTriggerType.Manual;
        public int turn;
        public string questTitle = PlaceholderText;
        public string questContent = PlaceholderText;
        public string questDecision = PlaceholderText;
        public string questResult = PlaceholderText;
        public string regionName = PlaceholderText;
        public int regionLevel;
        public bool isRegionDeactivationWarning;
        public bool isRegionDeactivated;
        public int turnsUntilDeactivation;
        public string affectedRegions = PlaceholderText;
        public float youthAffinity;
        public float seniorAffinity;
        public float corpAffinity;
        public string notableAffinityGroups = PlaceholderText;
        public int lineCount = 3;
    }

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

    private struct RegionStateSnapshot
    {
        public int univLevel;
        public int silverLevel;
        public int industryLevel;
        public int houseLevel;
        public bool isUnivDeactivated;
        public bool isSilverDeactivated;
        public bool isIndustryDeactivated;
        public bool isHouseDeactivated;
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        TryApplyKoreanFontToTmp();
    }

    private void Start()
    {
        if (warmupModelOnStart && !hasWarmedUpThisSession)
        {
            StartCoroutine(WarmupModelSilently());
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (!autoGenerateOnGameEvents)
        {
            return;
        }

        ObserveGameEvents();
    }

    public void GenerateNews()
    {
        GameNewsData data = BuildManualNewsData();
        if (!PrepareNewsData(data, true))
        {
            return;
        }

        EnqueueNews(data);
    }

    public void GenerateNewsWithTestData()
    {
        GameNewsData testData = new GameNewsData
        {
            triggerType = NewsTriggerType.Manual,
            turn = 12,
            questTitle = "\uCCAD\uB144 \uC548\uC2EC \uC8FC\uD0DD \uACF5\uAE09",
            questContent = "\uC5ED\uC138\uAD8C \uBD80\uC9C0\uC5D0 \uC800\uB834\uD55C \uC8FC\uD0DD\uC744 \uACF5\uAE09\uD574 \uCCAD\uB144\uCE35\uC758 \uC8FC\uAC70 \uBD80\uB2F4\uC744 \uB0AE\uCD94\uB294 \uC548\uAC74\uC785\uB2C8\uB2E4.",
            questDecision = QuestAcceptedText,
            questResult = QuestSuccessText,
            regionName = RegionCapital,
            regionLevel = 2,
            affectedRegions = RegionCapital,
            youthAffinity = 8f,
            seniorAffinity = 5f,
            corpAffinity = 4f,
            notableAffinityGroups = "\uCCAD\uB144(8.0)",
            lineCount = Mathf.Max(1, lineCount)
        };

        EnqueueNews(testData);
    }

    private void ObserveGameEvents()
    {
        if (!TryInitializeObservers())
        {
            return;
        }

        ObserveQuestState();
        ObservePolicyLog();
        ObserveAffinityThreshold();
        ObserveRegionState();
        ObserveRegionWarning();
    }

    private bool TryInitializeObservers()
    {
        if (UIManager.Instance == null || ScoreManager.Instance == null)
        {
            return false;
        }

        if (observersInitialized)
        {
            return true;
        }

        lastProposalText = GetProposalText();
        lastWarningText = GetWarningText();
        lastPolicyLogLength = GetPolicyLogText().Length;
        lastObservedQuest = QuestManager.Instance != null ? QuestManager.Instance.GetActiveQuest() : null;
        lastYouthAboveThreshold = ScoreManager.Instance.youthAffinity >= affinityThreshold;
        lastSeniorAboveThreshold = ScoreManager.Instance.seniorAffinity >= affinityThreshold;
        lastCorpAboveThreshold = ScoreManager.Instance.corpAffinity >= affinityThreshold;
        lastRegionState = CaptureRegionState();
        observersInitialized = true;
        return true;
    }

    private void ObserveQuestState()
    {
        QuestDefinition activeQuest = QuestManager.Instance != null ? QuestManager.Instance.GetActiveQuest() : null;
        if (activeQuest != null)
        {
            lastObservedQuest = activeQuest;
        }

        string proposalText = GetProposalText();
        if (proposalText == lastProposalText)
        {
            return;
        }

        QuestDefinition questForNews = activeQuest ?? lastObservedQuest;
        if (questForNews != null)
        {
            if (proposalText == AcceptedProposalText)
            {
                TryQueueEventNews(new GameNewsData
                {
                    triggerType = NewsTriggerType.QuestAccepted,
                    turn = GetCurrentTurn(),
                    questTitle = questForNews.questTitle,
                    questContent = questForNews.questDesc,
                    questDecision = QuestAcceptedText,
                    questResult = QuestInProgressText,
                    lineCount = Mathf.Max(1, lineCount)
                });
            }
            else if (proposalText == RefusedProposalText)
            {
                TryQueueEventNews(new GameNewsData
                {
                    triggerType = NewsTriggerType.QuestRefused,
                    turn = GetCurrentTurn(),
                    questTitle = questForNews.questTitle,
                    questContent = questForNews.questDesc,
                    questDecision = QuestRefusedText,
                    questResult = QuestClosedText,
                    lineCount = Mathf.Max(1, lineCount)
                });
            }
        }

        lastProposalText = proposalText;
    }

    private void ObservePolicyLog()
    {
        string policyLogText = GetPolicyLogText();
        if (policyLogText.Length < lastPolicyLogLength)
        {
            lastPolicyLogLength = policyLogText.Length;
            return;
        }

        if (policyLogText.Length == lastPolicyLogLength)
        {
            return;
        }

        string appendedText = policyLogText.Substring(lastPolicyLogLength);
        lastPolicyLogLength = policyLogText.Length;

        TryQueueQuestResultFromLog(appendedText, "[\uD018\uC2A4\uD2B8 \uC131\uACF5] : ", NewsTriggerType.QuestSucceeded, QuestSuccessText);
        TryQueueQuestResultFromLog(appendedText, "[\uD018\uC2A4\uD2B8 \uC2E4\uD328] : ", NewsTriggerType.QuestFailed, QuestFailureText);
    }

    private void ObserveRegionState()
    {
        RegionStateSnapshot currentState = CaptureRegionState();

        QueueRegionLevelUpIfNeeded(RegionNewTown, lastRegionState.univLevel, currentState.univLevel);
        QueueRegionLevelUpIfNeeded(RegionRural, lastRegionState.silverLevel, currentState.silverLevel);
        QueueRegionLevelUpIfNeeded(RegionLocal, lastRegionState.industryLevel, currentState.industryLevel);
        QueueRegionLevelUpIfNeeded(RegionCapital, lastRegionState.houseLevel, currentState.houseLevel);

        QueueRegionDeactivatedIfNeeded(RegionNewTown, lastRegionState.isUnivDeactivated, currentState.isUnivDeactivated);
        QueueRegionDeactivatedIfNeeded(RegionRural, lastRegionState.isSilverDeactivated, currentState.isSilverDeactivated);
        QueueRegionDeactivatedIfNeeded(RegionLocal, lastRegionState.isIndustryDeactivated, currentState.isIndustryDeactivated);
        QueueRegionDeactivatedIfNeeded(RegionCapital, lastRegionState.isHouseDeactivated, currentState.isHouseDeactivated);

        lastRegionState = currentState;
    }

    private void ObserveAffinityThreshold()
    {
        if (ScoreManager.Instance == null)
        {
            return;
        }

        List<string> newlyReachedGroups = new List<string>();

        bool youthAboveThreshold = ScoreManager.Instance.youthAffinity >= affinityThreshold;
        bool seniorAboveThreshold = ScoreManager.Instance.seniorAffinity >= affinityThreshold;
        bool corpAboveThreshold = ScoreManager.Instance.corpAffinity >= affinityThreshold;

        if (!lastYouthAboveThreshold && youthAboveThreshold)
        {
            newlyReachedGroups.Add($"\uCCAD\uB144({ScoreManager.Instance.youthAffinity:F1})");
        }

        if (!lastSeniorAboveThreshold && seniorAboveThreshold)
        {
            newlyReachedGroups.Add($"\uB178\uB144({ScoreManager.Instance.seniorAffinity:F1})");
        }

        if (!lastCorpAboveThreshold && corpAboveThreshold)
        {
            newlyReachedGroups.Add($"\uAE30\uC5C5({ScoreManager.Instance.corpAffinity:F1})");
        }

        lastYouthAboveThreshold = youthAboveThreshold;
        lastSeniorAboveThreshold = seniorAboveThreshold;
        lastCorpAboveThreshold = corpAboveThreshold;

        if (newlyReachedGroups.Count == 0)
        {
            return;
        }

        TryQueueEventNews(new GameNewsData
        {
            triggerType = NewsTriggerType.AffinityThresholdReached,
            turn = GetCurrentTurn(),
            notableAffinityGroups = string.Join(", ", newlyReachedGroups),
            lineCount = Mathf.Max(1, lineCount)
        });
    }

    private void ObserveRegionWarning()
    {
        string warningText = GetWarningText();
        if (warningText == lastWarningText)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(warningText))
        {
            List<string> lv1Regions = ScoreManager.Instance.GetLV1Regions();
            int turnsLeft = Mathf.Max(0, 20 - GetCurrentTurn());

            TryQueueEventNews(new GameNewsData
            {
                triggerType = NewsTriggerType.RegionDeactivationWarning,
                turn = GetCurrentTurn(),
                regionName = lv1Regions.Count == 1 ? lv1Regions[0] : MultipleRegionsText,
                regionLevel = 1,
                isRegionDeactivationWarning = true,
                turnsUntilDeactivation = turnsLeft,
                affectedRegions = lv1Regions.Count > 0 ? string.Join(", ", lv1Regions) : PlaceholderText,
                lineCount = Mathf.Max(1, lineCount)
            });
        }

        lastWarningText = warningText;
    }

    private void TryQueueQuestResultFromLog(string appendedText, string marker, NewsTriggerType triggerType, string resultText)
    {
        int startIndex = appendedText.IndexOf(marker, StringComparison.Ordinal);
        if (startIndex < 0)
        {
            return;
        }

        startIndex += marker.Length;
        int endIndex = appendedText.IndexOf('\n', startIndex);
        string questTitle = endIndex >= 0
            ? appendedText.Substring(startIndex, endIndex - startIndex).Trim()
            : appendedText.Substring(startIndex).Trim();

        if (string.IsNullOrWhiteSpace(questTitle))
        {
            return;
        }

        QuestDefinition quest = FindQuestByTitle(questTitle) ?? lastObservedQuest;
        string questContent = quest != null && quest.questTitle == questTitle ? quest.questDesc : PlaceholderText;

        TryQueueEventNews(new GameNewsData
        {
            triggerType = triggerType,
            turn = GetCurrentTurn(),
            questTitle = questTitle,
            questContent = questContent,
            questDecision = QuestAcceptedText,
            questResult = resultText,
            lineCount = Mathf.Max(1, lineCount)
        });
    }

    private void QueueRegionLevelUpIfNeeded(string regionName, int previousLevel, int currentLevel)
    {
        if (currentLevel <= previousLevel)
        {
            return;
        }

        TryQueueEventNews(new GameNewsData
        {
            triggerType = NewsTriggerType.RegionLevelUp,
            turn = GetCurrentTurn(),
            regionName = regionName,
            regionLevel = currentLevel,
            affectedRegions = regionName,
            lineCount = Mathf.Max(1, lineCount)
        });
    }

    private void QueueRegionDeactivatedIfNeeded(string regionName, bool wasDeactivated, bool isDeactivated)
    {
        if (wasDeactivated || !isDeactivated)
        {
            return;
        }

        TryQueueEventNews(new GameNewsData
        {
            triggerType = NewsTriggerType.RegionDeactivated,
            turn = GetCurrentTurn(),
            regionName = regionName,
            isRegionDeactivated = true,
            affectedRegions = regionName,
            lineCount = Mathf.Max(1, lineCount)
        });
    }

    private void TryQueueEventNews(GameNewsData data)
    {
        if (!PrepareNewsData(data, false))
        {
            return;
        }

        EnqueueNews(data);
    }

    private bool PrepareNewsData(GameNewsData data, bool showFailureMessage)
    {
        if (useGameManagerData && GameManager.Instance != null)
        {
            data.turn = GameManager.Instance.CURRENT_TURN;
        }

        if (useGameManagerData && ScoreManager.Instance != null)
        {
            data.youthAffinity = ScoreManager.Instance.youthAffinity;
            data.seniorAffinity = ScoreManager.Instance.seniorAffinity;
            data.corpAffinity = ScoreManager.Instance.corpAffinity;
        }

        data.lineCount = Mathf.Max(1, data.lineCount <= 0 ? lineCount : data.lineCount);

        if (data.notableAffinityGroups == PlaceholderText || string.IsNullOrWhiteSpace(data.notableAffinityGroups))
        {
            List<string> notableGroups = GetNotableAffinityGroups(data);
            data.notableAffinityGroups = notableGroups.Count > 0
                ? string.Join(", ", notableGroups)
                : PlaceholderText;
        }

        return true;
    }

    private List<string> GetNotableAffinityGroups(GameNewsData data)
    {
        List<string> groups = new List<string>();

        if (data.youthAffinity >= affinityThreshold)
        {
            groups.Add($"\uCCAD\uB144({data.youthAffinity:F1})");
        }

        if (data.seniorAffinity >= affinityThreshold)
        {
            groups.Add($"\uB178\uB144({data.seniorAffinity:F1})");
        }

        if (data.corpAffinity >= affinityThreshold)
        {
            groups.Add($"\uAE30\uC5C5({data.corpAffinity:F1})");
        }

        return groups;
    }

    private void EnqueueNews(GameNewsData data)
    {
        pendingNewsQueue.Enqueue(data);
        if (!isRequestInProgress)
        {
            StartCoroutine(ProcessNewsQueue());
        }
    }

    private IEnumerator ProcessNewsQueue()
    {
        while (pendingNewsQueue.Count > 0)
        {
            yield return SendNewsRequest(pendingNewsQueue.Dequeue());
        }
    }

    private GameNewsData BuildManualNewsData()
    {
        QuestDefinition activeQuest = QuestManager.Instance != null ? QuestManager.Instance.GetActiveQuest() : null;
        List<string> lv1Regions = ScoreManager.Instance != null ? ScoreManager.Instance.GetLV1Regions() : new List<string>();

        return new GameNewsData
        {
            triggerType = NewsTriggerType.Manual,
            turn = GetCurrentTurn(),
            questTitle = activeQuest != null ? activeQuest.questTitle : "없음",
            questContent = activeQuest != null ? activeQuest.questDesc : "없음",
            questDecision = string.IsNullOrWhiteSpace(GetProposalText()) ? "없음" : GetProposalText(),
            questResult = "없음",
            regionName = lv1Regions.Count == 1 ? lv1Regions[0] : "없음",
            regionLevel = 0,
            isRegionDeactivationWarning = lv1Regions.Count > 0,
            turnsUntilDeactivation = lv1Regions.Count > 0 ? Mathf.Max(0, 20 - GetCurrentTurn()) : 0,
            affectedRegions = lv1Regions.Count > 0 ? string.Join(", ", lv1Regions) : "없음",
            lineCount = Mathf.Max(1, lineCount)
        };
    }

    private IEnumerator SendNewsRequest(GameNewsData data)
    {
        isRequestInProgress = true;
        SetButtonInteractable(false);
        SetOutputText("AI \uB274\uC2A4 \uC0DD\uC131 \uC911...");

        string userPrompt = BuildUserPrompt(data);
        OllamaChatRequest requestBody = new OllamaChatRequest
        {
            model = modelName,
            stream = false,
            messages = new[]
            {
                new OllamaMessage { role = "system", content = systemPrompt },
                new OllamaMessage { role = "user", content = userPrompt }
            }
        };

        string json = JsonUtility.ToJson(requestBody);
        byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
        string requestUrl = BuildChatRequestUrl();

        using (UnityWebRequest request = new UnityWebRequest(requestUrl, UnityWebRequest.kHttpVerbPOST))
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
                isRequestInProgress = false;
                SetButtonInteractable(true);

                if (requestUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                {
                    SetOutputText(
                        "HTTP \uC811\uC18D\uC774 \uCC28\uB2E8\uB418\uC5C8\uC2B5\uB2C8\uB2E4.\n" +
                        "Unity Player Settings > Other Settings > Insecure HTTP Option\uC744 " +
                        "'Always Allowed'\uB85C \uC124\uC815\uD574 \uC8FC\uC138\uC694.\n" +
                        requestUrl);
                }
                else
                {
                    SetOutputText("\uC694\uCCAD \uC2DC\uC791 \uC2E4\uD328\n" + ex.Message);
                }

                Debug.LogException(ex);
                yield break;
            }

            yield return operation;

            isRequestInProgress = false;
            SetButtonInteractable(true);

            if (request.result != UnityWebRequest.Result.Success)
            {
                SetOutputText("Ollama \uC694\uCCAD \uC2E4\uD328\n" + request.error);
                yield break;
            }

            string responseText = request.downloadHandler.text;
            if (string.IsNullOrWhiteSpace(responseText))
            {
                SetOutputText("\uC751\uB2F5\uC774 \uBE44\uC5B4 \uC788\uC2B5\uB2C8\uB2E4.");
                yield break;
            }

            OllamaChatResponse response = JsonUtility.FromJson<OllamaChatResponse>(responseText);
            if (response == null)
            {
                SetOutputText("\uC751\uB2F5 \uD30C\uC2F1\uC5D0 \uC2E4\uD328\uD588\uC2B5\uB2C8\uB2E4.");
                yield break;
            }

            if (!string.IsNullOrWhiteSpace(response.error))
            {
                SetOutputText("Ollama \uC624\uB958\n" + response.error);
                yield break;
            }

            if (response.message == null || string.IsNullOrWhiteSpace(response.message.content))
            {
                SetOutputText("\uC0DD\uC131\uB41C \uB274\uC2A4\uAC00 \uC5C6\uC2B5\uB2C8\uB2E4.");
                yield break;
            }

            ShowGeneratedNews(response.message.content.Trim());
        }
    }

    private IEnumerator WarmupModelSilently()
    {
        hasWarmedUpThisSession = true;

        if (warmupDelaySeconds > 0f)
        {
            yield return new WaitForSecondsRealtime(warmupDelaySeconds);
        }

        string requestUrl = BuildChatRequestUrl();
        OllamaChatRequest requestBody = new OllamaChatRequest
        {
            model = modelName,
            stream = false,
            messages = new[]
            {
                new OllamaMessage
                {
                    role = "system",
                    content = "\uB2F5\uBCC0\uC744 \uCD5C\uC18C\uD55C\uC73C\uB85C \uC0DD\uC131\uD558\uB77C."
                },
                new OllamaMessage
                {
                    role = "user",
                    content = "\uC900\uBE44 \uD655\uC778"
                }
            }
        };

        string json = JsonUtility.ToJson(requestBody);
        byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest request = new UnityWebRequest(requestUrl, UnityWebRequest.kHttpVerbPOST))
        {
            request.uploadHandler = new UploadHandlerRaw(jsonBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = 20;
            request.SetRequestHeader("Content-Type", "application/json");

            UnityWebRequestAsyncOperation operation;
            try
            {
                operation = request.SendWebRequest();
            }
            catch (InvalidOperationException)
            {
                yield break;
            }

            yield return operation;
        }
    }

    private string BuildUserPrompt(GameNewsData data)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("\uB2E4\uC74C \uD604\uC7AC \uAC8C\uC784 \uB370\uC774\uD130\uB97C \uBC14\uD0D5\uC73C\uB85C \uB274\uC2A4 \uBCF8\uBB38\uB9CC \uC791\uC131\uD558\uB77C.");
        builder.AppendLine($"\uC815\uD655\uD788 {Mathf.Max(1, data.lineCount)}\uC904\uB85C \uC791\uC131\uD558\uB77C.");
        builder.AppendLine("\uC81C\uBAA9 \uC5C6\uC774 \uBCF8\uBB38\uB9CC \uC791\uC131\uD558\uB77C.");
        builder.AppendLine("\uC785\uB825 \uB370\uC774\uD130\uC5D0 \uC5C6\uB294 \uC0AC\uC2E4\uC740 \uC4F0\uC9C0 \uB9C8\uB77C.");
        builder.AppendLine();
        builder.AppendLine($"\uB274\uC2A4 \uC0DD\uC131 \uC0AC\uC720: {GetTriggerDescription(data.triggerType)}");
        builder.AppendLine($"\uD604\uC7AC \uD134: {data.turn}");
        builder.AppendLine($"\uBBFC\uC2EC {affinityThreshold:F1} \uC774\uC0C1 \uACC4\uCE35: {data.notableAffinityGroups}");
        builder.AppendLine($"\uCCAD\uB144 \uBBFC\uC2EC: {data.youthAffinity:F1}");
        builder.AppendLine($"\uB178\uB144 \uBBFC\uC2EC: {data.seniorAffinity:F1}");
        builder.AppendLine($"\uAE30\uC5C5 \uBBFC\uC2EC: {data.corpAffinity:F1}");
        builder.AppendLine();
        builder.AppendLine("[\uC785\uB825 \uB370\uC774\uD130]");
        builder.AppendLine($"\uD018\uC2A4\uD2B8 \uC81C\uBAA9: {SafeValue(data.questTitle)}");
        builder.AppendLine($"\uD018\uC2A4\uD2B8 \uB0B4\uC6A9: {SafeValue(data.questContent)}");
        builder.AppendLine($"\uD018\uC2A4\uD2B8 \uC218\uB77D/\uAC70\uC808 \uC5EC\uBD80: {SafeValue(data.questDecision)}");
        builder.AppendLine($"\uD018\uC2A4\uD2B8 \uC131\uACF5/\uC2E4\uD328 \uC5EC\uBD80: {SafeValue(data.questResult)}");
        builder.AppendLine($"\uAD6C\uC5ED\uBA85: {SafeValue(data.regionName)}");
        builder.AppendLine($"\uAD6C\uC5ED LV: {FormatRegionLevel(data.regionLevel)}");
        builder.AppendLine(
            $"\uAD6C\uC5ED \uC18C\uBA78 \uC9C1\uC804: {(data.isRegionDeactivationWarning ? $"\uC608 ({data.turnsUntilDeactivation}\uD134 \uD6C4 \uC18C\uBA78 \uC608\uC815, \uB300\uC0C1: {SafeValue(data.affectedRegions)})" : "\uC544\uB2C8\uC624")}");
        builder.AppendLine(
            $"\uAD6C\uC5ED \uC18C\uBA78 \uC2DC: {(data.isRegionDeactivated ? $"\uC608 ({SafeValue(data.affectedRegions)})" : "\uC544\uB2C8\uC624")}");
        return builder.ToString();
    }

    private string GetTriggerDescription(NewsTriggerType triggerType)
    {
        switch (triggerType)
        {
            case NewsTriggerType.QuestAccepted:
                return "\uD018\uC2A4\uD2B8 \uC218\uB77D";
            case NewsTriggerType.QuestRefused:
                return "\uD018\uC2A4\uD2B8 \uAC70\uC808";
            case NewsTriggerType.QuestSucceeded:
                return "\uD018\uC2A4\uD2B8 \uC131\uACF5";
            case NewsTriggerType.QuestFailed:
                return "\uD018\uC2A4\uD2B8 \uC2E4\uD328";
            case NewsTriggerType.AffinityThresholdReached:
                return "\uD2B9\uC815 \uACC4\uCE35 \uBBFC\uC2EC 7 \uC774\uC0C1 \uB2EC\uC131";
            case NewsTriggerType.RegionLevelUp:
                return "\uAD6C\uC5ED \uB808\uBCA8 \uC0C1\uC2B9";
            case NewsTriggerType.RegionDeactivationWarning:
                return "\uAD6C\uC5ED \uC18C\uBA78 \uC9C1\uC804";
            case NewsTriggerType.RegionDeactivated:
                return "\uAD6C\uC5ED \uC18C\uBA78";
            default:
                return "\uC218\uB3D9 \uB274\uC2A4 \uC0DD\uC131";
        }
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

    private void ShowGeneratedNews(string value)
    {
        SetOutputText(value);

        if (UIManager.Instance != null &&
            UIManager.Instance.newsButtonImage != null &&
            UIManager.Instance.newsQuestAlertSprite != null)
        {
            UIManager.Instance.newsButtonImage.sprite = UIManager.Instance.newsQuestAlertSprite;
        }
    }

    private void SetOutputText(string value)
    {
        TryApplyKoreanFontToTmp();

        if (newsOutputText != null)
        {
            newsOutputText.text = value;
        }

        if (legacyNewsOutputText != null)
        {
            legacyNewsOutputText.text = value;
        }
    }

    private void SetButtonInteractable(bool canInteract)
    {
        if (generateButton != null)
        {
            generateButton.interactable = canInteract;
        }
    }

    private void TryApplyKoreanFontToTmp()
    {
        if (newsOutputText == null)
        {
            return;
        }

        if (tmpKoreanFontOverride != null)
        {
            newsOutputText.font = tmpKoreanFontOverride;
            return;
        }

        if (!applyRuntimeKoreanFontToTmp || !allowOsFontFallbackForTmp || hasAttemptedRuntimeFontSetup)
        {
            return;
        }

        hasAttemptedRuntimeFontSetup = true;

        if (runtimeKoreanFontAsset == null)
        {
            Font osFont = CreateKoreanOsFont();
            if (osFont == null)
            {
                return;
            }

            try
            {
                runtimeKoreanFontAsset = TMP_FontAsset.CreateFontAsset(
                    osFont,
                    90,
                    9,
                    GlyphRenderMode.SDFAA,
                    1024,
                    1024,
                    AtlasPopulationMode.Dynamic,
                    true);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("OllamaNewsClient TMP runtime font fallback failed: " + ex.Message, this);
                runtimeKoreanFontAsset = null;
                return;
            }
        }

        if (runtimeKoreanFontAsset != null)
        {
            newsOutputText.font = runtimeKoreanFontAsset;
        }
    }

    private Font CreateKoreanOsFont()
    {
        string[] candidateFonts =
        {
            "Malgun Gothic",
            "\uB9D1\uC740 \uACE0\uB515",
            "Apple SD Gothic Neo",
            "NanumGothic",
            "Arial Unicode MS"
        };

        foreach (string fontName in candidateFonts)
        {
            Font font = Font.CreateDynamicFontFromOSFont(fontName, 32);
            if (font != null)
            {
                return font;
            }
        }

        return null;
    }

    private RegionStateSnapshot CaptureRegionState()
    {
        if (ScoreManager.Instance == null)
        {
            return default;
        }

        return new RegionStateSnapshot
        {
            univLevel = GetRegionLevel(ScoreManager.Instance.devUniv),
            silverLevel = GetRegionLevel(ScoreManager.Instance.devSilver),
            industryLevel = GetRegionLevel(ScoreManager.Instance.devIndustry),
            houseLevel = GetRegionLevel(ScoreManager.Instance.devHouse),
            isUnivDeactivated = ScoreManager.Instance.isUnivDeactivated,
            isSilverDeactivated = ScoreManager.Instance.isSilverDeactivated,
            isIndustryDeactivated = ScoreManager.Instance.isIndustryDeactivated,
            isHouseDeactivated = ScoreManager.Instance.isHouseDeactivated
        };
    }

    private int GetRegionLevel(float devValue)
    {
        if (devValue >= 50f) return 3;
        if (devValue >= 20f) return 2;
        return 1;
    }

    private int GetCurrentTurn()
    {
        return GameManager.Instance != null ? GameManager.Instance.CURRENT_TURN : 0;
    }

    private string GetPolicyLogText()
    {
        return UIManager.Instance != null && UIManager.Instance.policyLogText != null
            ? UIManager.Instance.policyLogText.text ?? string.Empty
            : string.Empty;
    }

    private string GetProposalText()
    {
        return UIManager.Instance != null && UIManager.Instance.ResultProposal != null
            ? UIManager.Instance.ResultProposal.text ?? string.Empty
            : string.Empty;
    }

    private string GetWarningText()
    {
        return UIManager.Instance != null && UIManager.Instance.newsWarningText != null
            ? UIManager.Instance.newsWarningText.text ?? string.Empty
            : string.Empty;
    }

    private QuestDefinition FindQuestByTitle(string questTitle)
    {
        if (string.IsNullOrWhiteSpace(questTitle))
        {
            return null;
        }

        foreach (QuestDefinition quest in QuestDatabase.AllQuests)
        {
            if (quest.questTitle == questTitle)
            {
                return quest;
            }
        }

        return null;
    }

    private string FormatRegionLevel(int regionLevel)
    {
        return regionLevel > 0 ? $"LV {regionLevel}" : PlaceholderText;
    }

    private string SafeValue(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? PlaceholderText : value.Trim();
    }
}
