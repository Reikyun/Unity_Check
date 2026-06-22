using System.Collections.Generic;

public interface IQuizRuleStrategy
{
    string RuleName { get; }
    int WinNorma { get; }
    int LoseNorma { get; }

    void OnRoundStart(List<PlayerData> players);
    void OnCorrect(int pIndex, List<PlayerData> players, ref int currentWinCount);
    void OnWrong(int pIndex, List<PlayerData> players);
    void OnThrough(List<PlayerData> players);
    void UpdatePlayerStatus(int myIndex, int selectedIndex, List<PlayerData> players);

    // ★決勝・特殊ルール用に共通規格として空メソッドを追加（エラー防止）
    void OnManualClear(int pIndex, List<PlayerData> players, ref int currentWinCount) { }
    void OnManualReset(List<PlayerData> players) { }
}