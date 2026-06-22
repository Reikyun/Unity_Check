using System.IO;
using System.Collections.Generic;
using UnityEngine;

public class QuizDataLoader : MonoBehaviour
{
    [Header("読み込まれた共通データ")]
    public List<PlayerData> players = new List<PlayerData>();
    public List<QuestionData> questions = new List<QuestionData>();

    // 他のスクリプト（QuizManagerなど）からデータを取得するためのプロパティ
    public List<PlayerData> LoadedPlayers => players;
    public List<QuestionData> LoadedQuestions => questions;

    void Awake()
    {
        // 起動時にデータをキャッシュするためAwakeで実行
        LoadPlayers("player.csv");
        LoadQuestions("questionALL.csv");
    }

    private void LoadPlayers(string fileName)
    {
        players.Clear();
        string path = Path.Combine(Application.streamingAssetsPath, fileName);

        if (!File.Exists(path))
        {
            Debug.LogError($"ファイルが見つかりません: {path}");
            return;
        }

        using (StreamReader sr = new StreamReader(path))
        {
            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) continue;

                string[] values = line.Split(',');

                if (values.Length >= 4)
                {
                    PlayerData p = new PlayerData();
                    int.TryParse(values[0], out p.paperRank);
                    p.playerName = values[1];
                    p.schoolName = values[2];
                    p.grade = values[3];

                    players.Add(p);
                }
            }
        }
        Debug.Log($"参加者データを {players.Count} 件読み込みました。");
    }

    private void LoadQuestions(string fileName)
    {
        questions.Clear();
        string path = Path.Combine(Application.streamingAssetsPath, fileName);

        if (!File.Exists(path))
        {
            Debug.LogError($"ファイルが見つかりません: {path}");
            return;
        }

        using (StreamReader sr = new StreamReader(path))
        {
            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) continue;

                string[] values = line.Split(',');

                if (values.Length >= 2)
                {
                    QuestionData q = new QuestionData();
                    q.questionText = values[0];
                    q.answerText = values[1];

                    questions.Add(q);
                }
            }
        }
        Debug.Log($"問題データを {questions.Count} 件読み込みました。");
    }
}