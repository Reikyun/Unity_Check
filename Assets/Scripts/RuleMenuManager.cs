using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class RuleMenuManager : MonoBehaviour
{
    private class MenuRoundItem
    {
        public string roundName;
        public IQuizRuleStrategy strategy;
        public List<int> targetPaperRanks = new List<int>();
        public int startQuestionOffset = 0;
    }

    [Header("依存コンポーネント")]
    [SerializeField] private QuizManager quizManager;

    [Header("メニューUIの参照")]
    [SerializeField] private GameObject ruleMenuPanel;
    [SerializeField] private TextMeshProUGUI menuListLeft;  // ★MenuListLeftを紐づけ
    [SerializeField] private TextMeshProUGUI menuListRight; // ★MenuListRightを紐づけ

    private List<MenuRoundItem> menuItems = new List<MenuRoundItem>();
    private int selectedMenuIndex = 0;
    private int selectedSubIndex = 0; // 0: 参加者指定, 1: 開始問題番号
    private bool isMenuOpen = false;

    // 入力文字列のキャッシュバッファ
    private string inputBuffer = "";

    void Awake()
    {
        // 全ラウンドの初期構成（蛇腹方式のペーパー順位を直接指定）
        menuItems.Add(new MenuRoundItem
        {
            roundName = "2R - 1組目 (5○2×)",
            strategy = new Rule2RStrategy(5, 2),
            targetPaperRanks = new List<int> { 1, 8, 9, 16, 17, 24, 25, 32, 33, 40, 41, 48 }
        });

        menuItems.Add(new MenuRoundItem
        {
            roundName = "2R - 2組目 (5○2×)",
            strategy = new Rule2RStrategy(5, 2),
            targetPaperRanks = new List<int> { 2, 7, 10, 15, 18, 23, 26, 31, 34, 39, 42, 47 }
        });

        menuItems.Add(new MenuRoundItem
        {
            roundName = "2R - 3組目 (5○2×)",
            strategy = new Rule2RStrategy(5, 2),
            targetPaperRanks = new List<int> { 3, 6, 11, 14, 19, 22, 27, 30, 35, 38, 43, 46 }
        });

        menuItems.Add(new MenuRoundItem
        {
            roundName = "2R - 4組目 (5○2×)",
            strategy = new Rule2RStrategy(5, 2),
            targetPaperRanks = new List<int> { 4, 5, 12, 13, 20, 21, 28, 29, 36, 37, 44, 45 }
        });

        // 3R以降
        menuItems.Add(new MenuRoundItem
        {
            roundName = "3R-A Swedish 10",
            strategy = new Rule3AStrategy(10, 10),
            targetPaperRanks = new List<int>()
        });

        menuItems.Add(new MenuRoundItem
        {
            roundName = "3R-B 10 up-down",
            strategy = new Rule3BStrategy(10, 3),
            targetPaperRanks = new List<int>()
        }); // ★きれいに閉じました

        menuItems.Add(new MenuRoundItem
        {
            roundName = "3R-C Freeze 10",
            strategy = new Rule3CStrategy(10),
            targetPaperRanks = new List<int>()
        });

        menuItems.Add(new MenuRoundItem
        {
            roundName = "3R-D 10 by 10",
            strategy = new Rule3DStrategy(),
            targetPaperRanks = new List<int>()
        });

        // SFラウンド 3セット制タイムレース
        menuItems.Add(new MenuRoundItem
        {
            roundName = "SF - 1st Set (9人➔6人)",
            strategy = new RuleSFStrategy(1),
            targetPaperRanks = Enumerable.Range(1, 9).ToList()
        });

        menuItems.Add(new MenuRoundItem
        {
            roundName = "SF - 2nd Set (6人➔3人)",
            strategy = new RuleSFStrategy(2),
            targetPaperRanks = Enumerable.Range(1, 9).ToList()
        });

        menuItems.Add(new MenuRoundItem
        {
            roundName = "SF - 3rd Set (3人➔1人通過)",
            strategy = new RuleSFStrategy(3),
            targetPaperRanks = Enumerable.Range(1, 9).ToList()
        });

        // --- RuleMenuManager.cs の Awake() の最末尾に追加 ---
        menuItems.Add(new MenuRoundItem
        {
            roundName = "FINAL - 決勝 Triple Seven",
            strategy = new RuleFStrategy(),
            targetPaperRanks = new List<int>() // 決勝進出者3名をその場で手動打ち込み
        });

    }

    void Start()
    {
        if (ruleMenuPanel != null) ruleMenuPanel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            ToggleMenu();
        }

        if (!isMenuOpen) return;

        HandleMenuNavigation();
        HandleDirectInput();

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            ConfirmSelection();
        }
    }

    private void ToggleMenu()
    {
        isMenuOpen = !isMenuOpen;
        if (ruleMenuPanel != null) ruleMenuPanel.SetActive(isMenuOpen);

        if (isMenuOpen)
        {
            int currentProgess = quizManager.GetCurrentQuestionIndex();
            foreach (var item in menuItems)
            {
                if (item.startQuestionOffset == 0)
                {
                    item.startQuestionOffset = currentProgess;
                }
            }
            ResetInputBuffer();
            UpdateMenuDisplay();
        }
    }

    private void HandleMenuNavigation()
    {
        // 上下キーでラウンド、または設定項目を選択
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            if (selectedSubIndex > 0)
            {
                selectedSubIndex--;
            }
            else
            {
                selectedMenuIndex = (selectedMenuIndex - 1 + menuItems.Count) % menuItems.Count;
                selectedSubIndex = 1; // 上のラウンドの「問題番号」にフォーカス
            }
            ResetInputBuffer();
            UpdateMenuDisplay();
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            if (selectedSubIndex < 1)
            {
                selectedSubIndex++;
            }
            else
            {
                selectedMenuIndex = (selectedMenuIndex + 1) % menuItems.Count;
                selectedSubIndex = 0; // 下のラウンドの「参加者」にフォーカス
            }
            ResetInputBuffer();
            UpdateMenuDisplay();
        }

        // 左右キーによる問題番号の1問ずつの微調整も残しておく（便利なので）
        if (selectedSubIndex == 1)
        {
            MenuRoundItem currentItem = menuItems[selectedMenuIndex];
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                currentItem.startQuestionOffset++;
                ResetInputBuffer();
                UpdateMenuDisplay();
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                if (currentItem.startQuestionOffset > 0)
                {
                    currentItem.startQuestionOffset--;
                    ResetInputBuffer();
                    UpdateMenuDisplay();
                }
            }
        }
    }

    private void HandleDirectInput()
    {
        MenuRoundItem currentItem = menuItems[selectedMenuIndex];

        // キーボードからの文字入力をバッファに蓄積
        foreach (char c in Input.inputString)
        {
            if (selectedSubIndex == 0) // [参加者指定] の入力（数字、カンマ、スペース）
            {
                if (char.IsDigit(c) || c == ',' || c == ' ')
                {
                    inputBuffer += c;
                    ParseBufferToRanks(currentItem);
                    UpdateMenuDisplay();
                }
            }
            else if (selectedSubIndex == 1) // [開始問題番号] の入力（数字のみ）
            {
                if (char.IsDigit(c))
                {
                    inputBuffer += c;
                    if (int.TryParse(inputBuffer, out int num))
                    {
                        // 画面表示は「#1」から始まるが内部インデックスは「0」からなので-1する
                        currentItem.startQuestionOffset = Mathf.Max(0, num - 1);
                    }
                    UpdateMenuDisplay();
                }
            }
        }

        // Backspaceで消去
        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            if (inputBuffer.Length > 0)
            {
                inputBuffer = inputBuffer.Substring(0, inputBuffer.Length - 1);

                if (selectedSubIndex == 0)
                {
                    ParseBufferToRanks(currentItem);
                }
                else
                {
                    if (int.TryParse(inputBuffer, out int num))
                    {
                        currentItem.startQuestionOffset = Mathf.Max(0, num - 1);
                    }
                    else
                    {
                        currentItem.startQuestionOffset = 0;
                    }
                }
                UpdateMenuDisplay();
            }
        }

        // Deleteキーでクリア
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            ResetInputBuffer();
            if (selectedSubIndex == 0) currentItem.targetPaperRanks.Clear();
            else currentItem.startQuestionOffset = 0;
            UpdateMenuDisplay();
        }
    }

    private void ResetInputBuffer()
    {
        inputBuffer = "";
    }

    private void ParseBufferToRanks(MenuRoundItem item)
    {
        string[] split = inputBuffer.Split(new char[] { ',', ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
        List<int> newRanks = new List<int>();

        foreach (var s in split)
        {
            if (int.TryParse(s, out int rank)) newRanks.Add(rank);
        }
        item.targetPaperRanks = newRanks;
    }

    private void UpdateMenuDisplay()
    {
        if (menuListLeft == null || menuListRight == null) return;

        string leftDisplay = "";
        string rightDisplay = "";

        // 前半の項目（例: 2R, 3R系）は左列、後半の項目（例: SF系）は右列に振り分ける
        // ここでは全9項目のうち、最初の5個を左、残りを右に配置します
        int splitIndex = 5;

        for (int i = 0; i < menuItems.Count; i++)
        {
            MenuRoundItem item = menuItems[i];
            bool isRoundSelected = (i == selectedMenuIndex);
            string blockText = "";

            if (isRoundSelected)
            {
                // 選択中のラウンドヘッダー
                blockText += $"<color=#FFCC00><b>▶ 【 {item.roundName.ToUpper()} 】</b></color>\n";

                // --- サブ項目1：参加者指定 ---
                bool isSub0 = (selectedSubIndex == 0);
                string sub0Cursor = isSub0 ? "<color=#00FFFF>  ● </color>" : "     ";
                string sub0Color = isSub0 ? "<color=#FFFFFF>" : "<color=#777777>";
                string ranksStr = item.targetPaperRanks.Count > 0 ? string.Join(", ", item.targetPaperRanks.Select(r => r + "位")) : "未入力";
                if (isSub0 && !string.IsNullOrEmpty(inputBuffer)) ranksStr = inputBuffer + " █";

                blockText += $"{sub0Cursor}{sub0Color}参加者: {ranksStr}</color>\n";

                // --- サブ項目2：開始問題番号 ---
                bool isSub1 = (selectedSubIndex == 1);
                string sub1Cursor = isSub1 ? "<color=#00FFFF>  ● </color>" : "     ";
                string sub1Color = isSub1 ? "<color=#FFFFFF>" : "<color=#777777>";
                string qStr = $"#{item.startQuestionOffset + 1}";
                if (isSub1 && !string.IsNullOrEmpty(inputBuffer)) qStr = inputBuffer + " █";

                blockText += $"{sub1Cursor}{sub1Color}問題番: {qStr}</color>\n\n";
            }
            else
            {
                // 非選択ラウンド
                blockText += $"<color=#444444>   【 {item.roundName} 】</color>\n\n";
            }

            // 振り分けロジック
            if (i < splitIndex)
            {
                leftDisplay += blockText;
            }
            else
            {
                rightDisplay += blockText;
            }
        }

        // それぞれのテキストUIに反映
        menuListLeft.text = leftDisplay;
        menuListRight.text = rightDisplay;
    }

    private void ConfirmSelection()
    {
        MenuRoundItem currentItem = menuItems[selectedMenuIndex];

        if (currentItem.targetPaperRanks.Count == 0)
        {
            Debug.LogWarning("参加者が一人も指定されていないため、ラウンドを開始できません。");
            return;
        }

        quizManager.SetupRound(currentItem.strategy, currentItem.targetPaperRanks, currentItem.startQuestionOffset);
        ToggleMenu();
    }
}