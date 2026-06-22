using System.Collections.Generic;
using UnityEngine;

public class Rule3AStrategy : IQuizRuleStrategy
{
    public string RuleName => "3R-A Swedish 10";
    public int WinNorma { get; }
    public int LoseNorma { get; }

    // コンストラクタ（デフォルトは10○、10×失格）
    public Rule3AStrategy(int winNorma = 10, int loseNorma = 10)
    {
        WinNorma = winNorma;
        LoseNorma = loseNorma;
    }

    public void OnRoundStart(List<PlayerData> players)
    {
        // ★通常ラウンドに戻すため、タイマーを消して問題文を復活させる
        var qm = GameObject.FindObjectOfType<QuizManager>();
        if (qm != null)
        {
            qm.SetQuestionUIVisibility(true);
            qm.SetupTimer(0f, false);
        }

        // 3R-A開始時の初期化
        foreach (var p in players)
        {
            p.score = 0;
            p.wrongCount = 0;
            p.pointMultiplier = 1; // Swedishでは通常1ずつの加算
            p.isCleared = false;
            p.isDisqualified = false;
            p.winOrder = 0;
        }
    }

    public void OnCorrect(int pIndex, List<PlayerData> players, ref int currentWinCount)
    {
        PlayerData p = players[pIndex];
        p.score++; // 1点追加

        // 10○到達で勝ち抜け
        if (p.score >= WinNorma && !p.isCleared)
        {
            p.isCleared = true;
            currentWinCount++;
            p.winOrder = currentWinCount;
        }
    }

    public void OnWrong(int pIndex, List<PlayerData> players)
    {
        PlayerData p = players[pIndex];

        // 【最重要】現在の○数（p.score）に応じて、×の増加量を決定する（ピラミッド構造）
        int penalty = 1;
        if (p.score >= 1 && p.score <= 2) penalty = 2;
        else if (p.score >= 3 && p.score <= 5) penalty = 3;
        else if (p.score >= 6 && p.score <= 9) penalty = 4;

        p.wrongCount += penalty;

        // 合計10×（LoseNorma）以上で失格
        if (p.wrongCount >= LoseNorma)
        {
            p.isDisqualified = true;
        }
    }

    public void OnThrough(List<PlayerData> players)
    {
        // Swedish 10 ではスルー時の変動はなし
    }

    public void UpdatePlayerStatus(int myIndex, int selectedIndex, List<PlayerData> players)
    {
        PlayerData p = players[myIndex];
        if (p.statusText == null) return;

        Color normalColor = Color.white;

        if (p.isCleared)
        {
            p.statusText.text = "WIN";
            p.nameText.color = normalColor;
        }
        else if (p.isDisqualified)
        {
            p.statusText.text = "失格";
            p.nameText.color = Color.gray;
        }
        else
        {
            // ○数に応じたペナルティ計算
            int nextPenalty = 1;
            if (p.score >= 1 && p.score <= 2) nextPenalty = 2;
            else if (p.score >= 3 && p.score <= 5) nextPenalty = 3;
            else if (p.score >= 6 && p.score <= 9) nextPenalty = 4;

            // 「解答中」の表示を廃止し、常に一律でペナルティ数のみを表示
            p.statusText.text = $"次+{nextPenalty}×";
            p.statusText.color = Color.yellow;
            p.nameText.color = normalColor;
        }
    }
}