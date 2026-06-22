using System.Collections.Generic;
using UnityEngine;

public class Rule3BStrategy : IQuizRuleStrategy
{
    public string RuleName => "3R-B 10 up-down";
    public int WinNorma { get; }
    public int LoseNorma { get; }

    // コンストラクタ（デフォルトは10○勝ち抜け、3×で失格）
    public Rule3BStrategy(int winNorma = 10, int loseNorma = 2)
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

        // 3R-B開始時の初期化
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
        p.score++; // 通常通り1点追加

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

        // 【最重要】これまでの○数をすべて没収し、0点に戻す（up-downペナルティ）
        p.score = 0;

        // ×（誤答数）を1つ増やす
        p.wrongCount++;

        // 3×以上（LoseNorma）に達したら失格
        if (p.wrongCount >= LoseNorma)
        {
            p.isDisqualified = true;
        }
    }

    public void OnThrough(List<PlayerData> players)
    {
        // 10 up-down ではスルー時の変動はなし
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
            // プレートの一番下（黄色いStatusText）には、現在の×状況に応じた警戒度を出す
            // 2×（あと1回で失格）のプレイヤーは赤文字で警告するなど、演出を少し凝ると大会が盛り上がります！
            if (p.wrongCount == 1)
            {
                p.statusText.text = "1×";
                p.statusText.color = new Color32(255, 51, 51, 255); // 鮮やかな赤色
            }
            else 
            {
                p.statusText.text = "";
                p.statusText.color = Color.yellow;
            }

            p.nameText.color = normalColor;
        }
    }
}