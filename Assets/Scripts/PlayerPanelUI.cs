using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerPanelUI : MonoBehaviour
{
    [Header("UIの参照（インスペクターで設定）")]
    public TextMeshProUGUI rankText;
    public TextMeshProUGUI schoolGradeText;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI wrongText;
    public TextMeshProUGUI statusText;       // インスペクターで割り当ててください
    public Image colorPlateImage;

    public void Setup(PlayerData data)
    {
        if (rankText != null) rankText.text = GetRankString(data.paperRank);
        if (schoolGradeText != null) schoolGradeText.text = $"{data.schoolName}\n{data.grade}";
        if (nameText != null) nameText.text = data.playerName;
        if (scoreText != null) scoreText.text = data.score.ToString();
        if (wrongText != null) wrongText.text = $"{data.wrongCount}×";
        if (statusText != null) statusText.text = "";

        if (colorPlateImage != null)
        {
            colorPlateImage.color = GetRankColor(data.paperRank);
        }

        // QuizManagerから一発で操作できるように全ての参照を渡す
        data.nameText = nameText;     // ★追加：名前を紐づけ
        data.scoreText = scoreText;
        data.wrongText = wrongText;
        data.statusText = statusText; // ★追加：ステータスを紐づけ
    }

    private string GetRankString(int rank)
    {
        if (rank == 11 || rank == 12 || rank == 13) return rank + "th";
        int lastDigit = rank % 10;
        if (lastDigit == 1) return rank + "st";
        if (lastDigit == 2) return rank + "nd";
        if (lastDigit == 3) return rank + "rd";
        return rank + "th";
    }

    private Color GetRankColor(int rank)
    {
        if (rank == 1) return new Color32(204, 0, 0, 255);
        if (rank <= 3) return new Color32(0, 102, 204, 255);
        if (rank <= 6) return new Color32(230, 180, 0, 255);
        return new Color32(0, 153, 51, 255);
    }
}