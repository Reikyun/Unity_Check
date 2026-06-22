using System.Collections.Generic;
using UnityEngine;

public class Rule3DStrategy : IQuizRuleStrategy
{
    public string RuleName => "3R-D 10 by 10";
    public int WinNorma => 100; // 100ポイントで勝ち抜け
    public int LoseNorma => 6;   // 6×で失格

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
            p.score = 0;        // 正解数 (○)
            p.wrongCount = 0;   // 誤答数 (×)
            p.pointMultiplier = 0;
            p.isCleared = false;
            p.isDisqualified = false;
            p.winOrder = 0;
        }
    }

    public void OnCorrect(int pIndex, List<PlayerData> players, ref int currentWinCount)
    {
        PlayerData p = players[pIndex];
        p.score++;

        int currentPoints = p.score * (10 - p.wrongCount);
        if (currentPoints >= WinNorma && !p.isCleared)
        {
            p.isCleared = true;
            currentWinCount++;
            p.winOrder = currentWinCount;
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
    }

    public void OnThrough(List<PlayerData> players)
    {
        // 10 by 10 ではスルー時の処理はなし
    }

    public void UpdatePlayerStatus(int myIndex, int selectedIndex, List<PlayerData> players)
    {
        PlayerData p = players[myIndex];
        if (p.statusText == null) return;

        Color normalColor = Color.white;

        if (p.scoreText != null && !p.isCleared)
        {
            int currentPoints = p.score * (10 - p.wrongCount);
            p.scoreText.text = currentPoints.ToString();
        }
        if (p.wrongText != null && !p.isDisqualified)
        {
            p.wrongText.text = $"{p.score}○/{p.wrongCount}×";
        }

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
            int remainingLife = LoseNorma - p.wrongCount;
            if (remainingLife <= 2)
            {
                p.statusText.text = $"あと{remainingLife}×で失格!";
                p.statusText.color = new Color32(255, 51, 51, 255);
            }
            else
            {
                p.statusText.text = "10 BY 10";
                p.statusText.color = Color.yellow;
            }
            p.nameText.color = normalColor;
        }
    }
} // ★ここできれいにクラスが閉じます