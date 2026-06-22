using System.Collections.Generic;
using UnityEngine;

public class Rule3CStrategy : IQuizRuleStrategy
{
    public string RuleName => "3R-C Freeze 10";
    public int WinNorma { get; }
    public int LoseNorma => 999; // 一発失格はなし

    public Rule3CStrategy(int winNorma = 10)
    {
        WinNorma = winNorma;
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

        foreach (var p in players)
        {
            p.score = 0;
            p.wrongCount = 0;
            p.pointMultiplier = 0; // ★Freezeルールでは、pointMultiplierを「残りフリーズ問数」として流用します
            p.isCleared = false;
            p.isDisqualified = false;
            p.winOrder = 0;
        }
    }

    public void OnCorrect(int pIndex, List<PlayerData> players, ref int currentWinCount)
    {
        PlayerData p = players[pIndex];
        p.score++;

        if (p.score >= WinNorma && !p.isCleared)
        {
            p.isCleared = true;
            currentWinCount++;
            p.winOrder = currentWinCount;
        }

        // 誰かが正解したため、本人以外のフリーズカウントを1減らす
        CountDownFreeze(pIndex, players);
    }

    public void OnWrong(int pIndex, List<PlayerData> players)
    {
        PlayerData p = players[pIndex];

        // 【最重要】フリーズ問数 = 現在の○数 + 現在の×数 + 1 (今回の誤答分)
        int freezeTurns = p.wrongCount + 1;
        p.pointMultiplier = freezeTurns; // 残りフリーズ問数をセット

        p.wrongCount++; // ×数自体もカウントアップ

        // 誰かが誤答したため、本人以外のフリーズカウントを1減らす
        CountDownFreeze(pIndex, players);
    }

    public void OnThrough(List<PlayerData> players)
    {
        // スルー時は全員のフリーズカウントを1減らす
        CountDownFreeze(-1, players);
    }

    private void CountDownFreeze(int activeIndex, List<PlayerData> players)
    {
        for (int i = 0; i < players.Count; i++)
        {
            if (i == activeIndex) continue; // 行動を起こした本人はカウントダウンしない
            if (players[i].pointMultiplier > 0)
            {
                players[i].pointMultiplier--;
            }
        }
    }

    public void UpdatePlayerStatus(int myIndex, int selectedIndex, List<PlayerData> players)
    {
        PlayerData p = players[myIndex];
        if (p.statusText == null) return;

        if (p.isCleared)
        {
            p.statusText.text = "WIN";
            p.nameText.color = Color.white;
        }
        else if (p.pointMultiplier > 0)
        {
            // フリーズ中の表示。名前をグレーアウトして残り問数を出す
            p.statusText.text = $"FREEZE ({p.pointMultiplier})";
            p.statusText.color = Color.red;
            p.nameText.color = Color.gray;
        }
        else
        {
            // 通常状態。次に誤答した際のリスク（○+×+1）をプレビュー表示
            int nextRisk = p.score + p.wrongCount + 1;
            p.statusText.text = "";
            p.statusText.color = Color.yellow;
            p.nameText.color = Color.white;
        }
    }
}