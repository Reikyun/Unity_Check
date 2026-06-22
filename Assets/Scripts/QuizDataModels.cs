using UnityEngine;
using System.Collections.Generic;
using TMPro;

[System.Serializable]
public class PlayerData
{
    [Header("CSVから読み込む固定データ")]
    public int paperRank;
    public string playerName;
    public string schoolName;
    public string grade;

    [Header("ゲーム進行中に変化する動的データ")]
    public int score = 0;
    public int wrongCount = 0;
    public int pointMultiplier = 1;
    public bool isCleared = false;
    public bool isDisqualified = false;
    public int winOrder = 0;

    [Header("UIへの参照")]
    public TextMeshProUGUI nameText;   // ★追加：名前の参照枠を新設
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI wrongText;
    public TextMeshProUGUI statusText;
}

[System.Serializable]
public class QuestionData
{
    public string questionText;
    public string answerText;
}

public class QuizStateSnapshot
{
    public int questionIndex;
    public List<PlayerStateData> playerStates = new List<PlayerStateData>();
}

public class PlayerStateData
{
    public int score;
    public int wrongCount;
    public int pointMultiplier;
    public bool isCleared;
    public bool isDisqualified;
    public int winOrder;
}