using System.Collections.Generic;
using UnityEngine;

public class RuleSFStrategy : IQuizRuleStrategy
{
    private int currentSet = 1;
    public string RuleName => $"SF Nine Hundred [第{currentSet}セット]";

    public int WinNorma => 999;
    public int LoseNorma => -999;

    private int plusPoint = 1;
    private int minusPoint = -1;

    public RuleSFStrategy(int setNumber = 1)
    {
        currentSet = setNumber;
        UpdateScoreSettings();
    }

    private void UpdateScoreSettings()
    {
        if (currentSet == 1) { plusPoint = 1; minusPoint = -1; }
        else if (currentSet == 2) { plusPoint = 1; minusPoint = -2; }
        else if (currentSet == 3) { plusPoint = 2; minusPoint = -2; }
    }

    public void OnRoundStart(List<PlayerData> players)
    {
        var qm = Object.FindAnyObjectByType<QuizManager>();
        if (qm != null)
        {
            qm.SetQuestionUIVisibility(false);
            qm.SetupTimer(300f, true);
        }

        // 完全な第1セットの開始時のみスコアを初期化。2セット目以降は何もせず引き継ぐ
        if (currentSet == 1)
        {
            foreach (var p in players)
            {
                p.score = 0;
                p.wrongCount = 0;
                p.isCleared = false;
                p.isDisqualified = false;
                p.winOrder = 0;
            }
        }
    }

    public void OnCorrect(int pIndex, List<PlayerData> players, ref int currentWinCount)
    {
        players[pIndex].score += plusPoint;
    }

    public void OnWrong(int pIndex, List<PlayerData> players)
    {
        players[pIndex].score += minusPoint;
    }

    public void OnThrough(List<PlayerData> players) { }

    public void UpdatePlayerStatus(int myIndex, int selectedIndex, List<PlayerData> players)
    {
        PlayerData p = players[myIndex];
        if (p.statusText == null) return;

        var qm = Object.FindAnyObjectByType<QuizManager>();
        bool isMasked = (qm != null) && !qm.isSFScoreRevealed;

        if (p.isCleared)
        {
            p.statusText.text = $"第{p.winOrder}SET通過";
            p.statusText.color = Color.white;
            p.nameText.color = Color.white;
        }
        else if (p.isDisqualified)
        {
            p.statusText.text = "失格脱落";
            p.statusText.color = Color.gray;
            p.nameText.color = Color.gray;
        }
        else if (isMasked)
        {
            p.statusText.text = "LOCKED";
            p.statusText.color = new Color32(100, 100, 100, 255);
            p.nameText.color = Color.white;
        }
        else
        {
            p.statusText.text = $"SF 生存中";
            p.statusText.color = Color.yellow;
            p.nameText.color = Color.white;
        }
    }
}