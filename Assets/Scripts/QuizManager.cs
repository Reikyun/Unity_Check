using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class QuizManager : MonoBehaviour
{
    [Header("依存コンポーネント")]
    [SerializeField] private QuizDataLoader dataLoader;

    [Header("SF用タイマー参照")]
    [SerializeField] private TextMeshProUGUI timerTextUI;

    [Header("UI生成設定（プレイヤー用）")]
    [SerializeField] private GameObject playerPanelPrefab;
    [SerializeField] private Transform playerContainer;

    [Header("UI参照（問題文・解答用）")]
    [SerializeField] private TextMeshProUGUI questionTextUI;
    [SerializeField] private TextMeshProUGUI answerTextUI;
    [SerializeField] private TextMeshProUGUI roundTitleTextUI;

    [Header("演出・カラー設定")]
    public Color normalNameColor = Color.white;
    public Color comboBonusColor = Color.yellow;

    private float remainingTime = 300f;
    private bool isTimerRunning = false;

    private List<PlayerData> masterPlayers;
    private List<QuestionData> masterQuestions;
    private List<PlayerData> activePlayers = new List<PlayerData>();

    private int selectedPlayerIndex = -1;
    private int currentQuestionIndex = 0;
    private int currentWinCount = 0;

    private IQuizRuleStrategy currentRuleStrategy;
    private Stack<QuizStateSnapshot> actionHistory = new Stack<QuizStateSnapshot>();

    // ★SF用：現在得点を開示（オープン）しているかどうかのフラグ
    public bool isSFScoreRevealed { get; private set; } = false;

    void Start()
    {
        if (dataLoader == null) dataLoader = GetComponent<QuizDataLoader>();

        if (dataLoader != null)
        {
            masterPlayers = dataLoader.LoadedPlayers;
            masterQuestions = dataLoader.LoadedQuestions;
        }

        List<int> testStartRanks = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
        SetupRound(new Rule2RStrategy(5, 2), testStartRanks, 0);
        SetupTimer(0f, false);
    }

    public void SetupRound(IQuizRuleStrategy ruleStrategy, List<int> targetPaperRanks, int startQuestionIndex)
    {
        if (masterPlayers == null || masterPlayers.Count == 0) return;

        bool isMovingBetweenSF = (currentRuleStrategy is RuleSFStrategy && ruleStrategy is RuleSFStrategy);

        currentRuleStrategy = ruleStrategy;
        currentQuestionIndex = startQuestionIndex;
        selectedPlayerIndex = -1;
        actionHistory.Clear();

        if (!isMovingBetweenSF) currentWinCount = 0;

        // SFセット間の移動時は、前セットでついたWIN・失格・点数の状態を100%そのまま引き継ぐ
        if (!isMovingBetweenSF)
        {
            activePlayers.Clear();
            foreach (int rank in targetPaperRanks)
            {
                PlayerData matchPlayer = masterPlayers.FirstOrDefault(p => p.paperRank == rank);
                if (matchPlayer != null) activePlayers.Add(matchPlayer);
            }
        }

        if (roundTitleTextUI != null) roundTitleTextUI.text = currentRuleStrategy.RuleName;

        // 次のセット開始時は再び自動でマスク（隠す）状態に戻す
        isSFScoreRevealed = false;

        currentRuleStrategy.OnRoundStart(activePlayers);

        if (!isMovingBetweenSF)
        {
            GeneratePlayerUI();
        }

        UpdateAllUI();
        UpdateQuestionUI();
    }

    private void GeneratePlayerUI()
    {
        if (playerPanelPrefab == null || playerContainer == null) return;
        foreach (Transform child in playerContainer) Destroy(child.gameObject);

        for (int i = 0; i < activePlayers.Count; i++)
        {
            GameObject panelObj = Instantiate(playerPanelPrefab, playerContainer);
            PlayerPanelUI panelUI = panelObj.GetComponent<PlayerPanelUI>();
            if (panelUI != null) panelUI.Setup(activePlayers[i]);
        }
    }

    void Update()
    {
        HandlePlayerSelection();
        HandleAnswerInput();
        HandleThroughInput();
        HandleUndoInput();

        // ★SF専用：完全手動による演出キーコントロール
        if (currentRuleStrategy is RuleSFStrategy)
        {
            // Q: 得点一斉開示 (?? ➔ 数字)
            if (Input.GetKeyDown(KeyCode.Q))
            {
                isSFScoreRevealed = true;
                UpdateAllUI();
            }
            // W: 得点非表示 (数字 ➔ ??)
            else if (Input.GetKeyDown(KeyCode.W))
            {
                isSFScoreRevealed = false;
                UpdateAllUI();
            }
            // D: 選択中のプレイヤーを「通過(WIN)」にする
            else if (Input.GetKeyDown(KeyCode.D) && selectedPlayerIndex != -1)
            {
                SaveCurrentState();
                PlayerData p = activePlayers[selectedPlayerIndex];
                p.isCleared = true;
                currentWinCount++;
                p.winOrder = currentWinCount; // 通過したセット数などの識別に流用
                selectedPlayerIndex = -1; // 選択を解除
                UpdateAllUI();
            }
            // F: 選択中のプレイヤーを「失格」にする
            else if (Input.GetKeyDown(KeyCode.F) && selectedPlayerIndex != -1)
            {
                SaveCurrentState();
                PlayerData p = activePlayers[selectedPlayerIndex];
                p.isDisqualified = true;
                selectedPlayerIndex = -1; // 選択を解除
                UpdateAllUI();
            }
        }
        // ★★★ 決勝（FINAL）専用の完全手動セットマッチコントロール ★★★
        if (currentRuleStrategy is RuleFStrategy)
        {
            // 【Dキー】選択中のプレイヤーがそのセットを獲得（セットカウント+1）
            if (Input.GetKeyDown(KeyCode.D) && selectedPlayerIndex != -1)
            {
                SaveCurrentState();
                currentRuleStrategy.OnManualClear(selectedPlayerIndex, activePlayers, ref currentWinCount);
                selectedPlayerIndex = -1;
                UpdateAllUI();
            }

            // 【Fキー】選択中のプレイヤーがそのセットにおいて失格
            else if (Input.GetKeyDown(KeyCode.F) && selectedPlayerIndex != -1)
            {
                SaveCurrentState();
                PlayerData p = activePlayers[selectedPlayerIndex];
                p.isDisqualified = true;
                selectedPlayerIndex = -1;
                UpdateAllUI();
            }

            // 【Lキー】次のセットへ（全員の○と×の点数を0にリセット、獲得セット数は維持）
            else if (Input.GetKeyDown(KeyCode.L))
            {
                SaveCurrentState();
                currentRuleStrategy.OnManualReset(activePlayers);
                selectedPlayerIndex = -1;
                UpdateAllUI();
                Debug.Log("次のセットへ移行しました。○と×がリセットされました。");
            }
        }


        if (isTimerRunning && remainingTime > 0)
        {
            remainingTime -= Time.deltaTime;
            if (remainingTime <= 0)
            {
                remainingTime = 0;
                isTimerRunning = false;
            }
            UpdateTimerUI();
        }

        if (Input.GetKeyDown(KeyCode.P)) isTimerRunning = !isTimerRunning;
    }

    private void UpdateTimerUI()
    {
        if (timerTextUI == null) return;
        int minutes = Mathf.FloorToInt(remainingTime / 60f);
        int seconds = Mathf.FloorToInt(remainingTime % 60f);
        timerTextUI.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    public void SetupTimer(float seconds, bool activate)
    {
        remainingTime = seconds;
        isTimerRunning = false;
        if (timerTextUI != null)
        {
            timerTextUI.gameObject.SetActive(activate);
            UpdateTimerUI();
        }
    }

    public void SetQuestionUIVisibility(bool visible)
    {
        if (questionTextUI != null) questionTextUI.gameObject.SetActive(visible);
        if (answerTextUI != null) answerTextUI.gameObject.SetActive(visible);
    }

    private void HandlePlayerSelection()
    {
        if (activePlayers == null || activePlayers.Count == 0) return;

        // ★通過・失格済みの人も、手動でDやFのスタンプを押すために選択だけは可能にしておきます
        for (int i = 0; i < activePlayers.Count; i++)
        {
            if (i < 9 && Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                SelectPlayer(i);
            }
        }
    }

    private void SelectPlayer(int index)
    {
        selectedPlayerIndex = index;
        UpdateAllUI();
    }

    private void HandleAnswerInput()
    {
        if (selectedPlayerIndex == -1 || activePlayers == null) return;

        // 通常のクイズ解答（すでにWIN/失格の人は解答できないようにガード）
        if (activePlayers[selectedPlayerIndex].isCleared || activePlayers[selectedPlayerIndex].isDisqualified) return;

        if (Input.GetKeyDown(KeyCode.C)) ProcessCorrectAnswer(selectedPlayerIndex);
        else if (Input.GetKeyDown(KeyCode.V)) ProcessWrongAnswer(selectedPlayerIndex);
    }

    private void HandleThroughInput()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            SaveCurrentState();
            currentRuleStrategy.OnThrough(activePlayers);
            AdvanceQuestion();
        }
    }

    private void HandleUndoInput()
    {
        if (Input.GetKeyDown(KeyCode.Z)) UndoLastQuestion();
    }

    private void ProcessCorrectAnswer(int pIndex)
    {
        SaveCurrentState();
        currentRuleStrategy.OnCorrect(pIndex, activePlayers, ref currentWinCount);
        UpdateAllUI();
        AdvanceQuestion();
    }

    private void ProcessWrongAnswer(int pIndex)
    {
        SaveCurrentState();
        currentRuleStrategy.OnWrong(pIndex, activePlayers);
        UpdateAllUI();
        AdvanceQuestion();
    }

    private void SaveCurrentState()
    {
        QuizStateSnapshot snapshot = new QuizStateSnapshot { questionIndex = currentQuestionIndex };
        foreach (var p in activePlayers)
        {
            PlayerStateData state = new PlayerStateData
            {
                score = p.score,
                wrongCount = p.wrongCount,
                pointMultiplier = p.pointMultiplier,
                isCleared = p.isCleared,
                isDisqualified = p.isDisqualified,
                winOrder = p.winOrder
            };
            snapshot.playerStates.Add(state);
        }
        actionHistory.Push(snapshot);
    }

    private void UndoLastQuestion()
    {
        if (actionHistory.Count == 0) return;

        QuizStateSnapshot previousState = actionHistory.Pop();
        currentQuestionIndex = previousState.questionIndex;

        for (int i = 0; i < activePlayers.Count; i++)
        {
            PlayerData p = activePlayers[i];
            PlayerStateData saved = previousState.playerStates[i];
            p.score = saved.score; p.wrongCount = saved.wrongCount; p.pointMultiplier = saved.pointMultiplier;
            p.isCleared = saved.isCleared; p.isDisqualified = saved.isDisqualified; p.winOrder = saved.winOrder;
        }

        selectedPlayerIndex = -1;
        UpdateAllUI();
        UpdateQuestionUI();
    }

    private void UpdateQuestionUI()
    {
        if (masterQuestions == null || masterQuestions.Count == 0) return;
        if (currentQuestionIndex >= 0 && currentQuestionIndex < masterQuestions.Count)
        {
            questionTextUI.text = masterQuestions[currentQuestionIndex].questionText;
            answerTextUI.text = masterQuestions[currentQuestionIndex].answerText;
        }
    }

    private void AdvanceQuestion()
    {
        currentQuestionIndex++;
        selectedPlayerIndex = -1;
        UpdateQuestionUI();
    }

    private void UpdateAllUI()
    {
        if (activePlayers == null || currentRuleStrategy == null) return;

        bool isMasked = (currentRuleStrategy is RuleSFStrategy) && !isSFScoreRevealed;

        for (int i = 0; i < activePlayers.Count; i++)
        {
            PlayerData p = activePlayers[i];

            if (p.scoreText != null)
            {
                if (p.isCleared) p.scoreText.text = "通過";
                else if (p.isDisqualified) p.scoreText.text = "失格";
                else if (isMasked) p.scoreText.text = "??";
                else p.scoreText.text = p.score.ToString();
            }

            if (p.wrongText != null)
            {
                if (p.isCleared || p.isDisqualified || isMasked) p.wrongText.text = "";
                else p.wrongText.text = "0×";
            }

            currentRuleStrategy.UpdatePlayerStatus(i, selectedPlayerIndex, activePlayers);
        }
    }

    public int GetCurrentQuestionIndex()
    {
        return currentQuestionIndex;
    }
}