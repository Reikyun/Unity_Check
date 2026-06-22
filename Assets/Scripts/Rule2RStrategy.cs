using System.Collections.Generic;
using UnityEngine;

public class Rule2RStrategy : IQuizRuleStrategy
{
    public string RuleName => "2R 連答付き5○2×";
    public int WinNorma { get; }
    public int LoseNorma { get; }

    // コンストラクタでノルマ数を柔軟に受け取れるようにする
    public Rule2RStrategy(int winNorma = 5, int loseNorma = 2)
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

        // 2R開始時は全員の状態を綺麗にする
        foreach (var p in players)
        {
            p.score = 0;
            p.wrongCount = 0;
            p.pointMultiplier = 1;
            p.isCleared = false;
            p.isDisqualified = false;
            p.winOrder = 0;
        }
    }

    public void OnCorrect(int pIndex, List<PlayerData> players, ref int currentWinCount)
    {
        PlayerData p = players[pIndex];
        p.score += p.pointMultiplier;

        if (p.score >= WinNorma && !p.isCleared)
        {
            p.isCleared = true;
            currentWinCount++;
            p.winOrder = currentWinCount;
        }

        // 連答ボーナスのトグル処理（2点の次は1点、1点の次は2点）
        int nextMultiplier = (p.pointMultiplier == 2) ? 1 : 2;

        for (int i = 0; i < players.Count; i++)
        {
            players[i].pointMultiplier = (i == pIndex) ? nextMultiplier : 1;
        }
    }

    public void OnWrong(int pIndex, List<PlayerData> players)
    {
        PlayerData p = players[pIndex];
        p.wrongCount++;

        if (p.wrongCount >= LoseNorma)
        {
            p.isDisqualified = true;
        }

        // 誤答時は全員の連答ボーナスをリセット
        foreach (var player in players)
        {
            player.pointMultiplier = 1;
        }
    }

    public void OnThrough(List<PlayerData> players)
    {
        // 2Rではスルー時にプレイヤーデータの変動はなし
    }

    public void UpdatePlayerStatus(int myIndex, int selectedIndex, List<PlayerData> players)
    {
        PlayerData p = players[myIndex];
        if (p.statusText == null) return;

        Color normalColor = Color.white;
        Color comboColor = Color.yellow;

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
        else if (p.pointMultiplier == 2)
        {
            p.statusText.text = "連答中";
            p.statusText.color = comboColor;
            p.nameText.color = comboColor;
        }
        else
        {
            // 「解答中」の表示を廃止し、通常時は何も表示しない（すっきりさせる）
            p.statusText.text = "";
            p.nameText.color = normalColor;
        }
    }
}