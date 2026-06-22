using System.Collections.Generic;
using UnityEngine;

public class RuleFStrategy : IQuizRuleStrategy
{
    public string RuleName => "FINAL Triple Seven Voyage";
    public int WinNorma => 7;
    public int LoseNorma => 3;

    private Dictionary<PlayerData, int> setCounts = new Dictionary<PlayerData, int>();

    public void OnRoundStart(List<PlayerData> players)
    {
        var qm = Object.FindAnyObjectByType<QuizManager>();
        if (qm != null)
        {
            qm.SetQuestionUIVisibility(true);
            qm.SetupTimer(0f, false);
        }

        setCounts.Clear();
        foreach (var p in players)
        {
            setCounts[p] = 0;
            ResetSetScore(p);
        }
    }

    private void ResetSetScore(PlayerData p)
    {
        p.score = 0;
        p.wrongCount = 0;
        p.isCleared = false;
        p.isDisqualified = false;
        p.winOrder = 0;
    }

    public void OnCorrect(int pIndex, List<PlayerData> players, ref int currentWinCount)
    {
        players[pIndex].score++;
    }

    public void OnWrong(int pIndex, List<PlayerData> players)
    {
        players[pIndex].wrongCount++;
    }

    public void OnThrough(List<PlayerData> players) { }

    // ★インターフェース規格としてDキー処理を実装
    public void OnManualClear(int pIndex, List<PlayerData> players, ref int currentWinCount)
    {
        PlayerData p = players[pIndex];
        if (!setCounts.ContainsKey(p)) setCounts[p] = 0;

        setCounts[p]++;

        if (setCounts[p] >= 3)
        {
            p.isCleared = true; // 3セット先取で完全優勝
        }
    }

    // ★インターフェース規格としてMキー処理（セットリセット）を実装
    public void OnManualReset(List<PlayerData> players)
    {
        foreach (var p in players)
        {
            ResetSetScore(p);
        }
    }

    public void UpdatePlayerStatus(int myIndex, int selectedIndex, List<PlayerData> players)
    {
        PlayerData p = players[myIndex];
        if (p.statusText == null) return;

        if (!setCounts.ContainsKey(p)) setCounts[p] = 0;
        int currentSets = setCounts[p];

        if (p.isCleared)
        {
            p.statusText.text = "完全優勝 !!";
            p.statusText.color = Color.yellow;
            p.nameText.color = Color.yellow;
        }
        else if (p.isDisqualified)
        {
            p.statusText.text = $"{currentSets}セット獲得中 (休)";
            p.statusText.color = Color.gray;
            p.nameText.color = Color.gray;
        }
        else
        {
            p.statusText.text = $"{currentSets}セット獲得中";
            p.statusText.color = Color.yellow;
            p.nameText.color = Color.white;
        }
    }
}