using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace TXT.WEAVR.Common
{

    [AddComponentMenu("WEAVR/Scoring System/Scoreboard")]
    public class Scoreboard : MonoBehaviour
    {
        [Serializable]
        public class ScoresJSON
        {
            public ScoreItemJSON[] scores;

            public void OrderByScore() => scores = scores.OrderByDescending(s => s.score).ToArray();
            public void OrderByName() => scores = scores.OrderBy(s => s.name).ToArray();
            public void OrderByDate() => scores = scores.OrderByDescending(s => s.Date).ToArray();
        }

        [Serializable]
        public class ScoreItemJSON
        {
            public string name;
            public float score;
            [SerializeField]
            private string date;

            public DateTime Date
            {
                get => DateTime.Parse(date);
                set
                {
                    date = value.ToString();
                }
            }
        }

        public enum Ordering
        {
            None,
            DescendingByScore,
            AscendingByName,
            DescendingByDate,
        }

        [SerializeField]
        private string m_jsonFilename = "Scoreboard.json";

        public GameObject container;
        public ScoreboardLine lineSample;
        public bool autoStart = true;

        [SerializeField]
        private Ordering m_ordering = Ordering.DescendingByScore;


        public Ordering CurrentOrdering
        {
            get => m_ordering;
            set
            {
                if(m_ordering != value)
                {
                    m_ordering = value;
                    Refresh();
                }
            }
        }

        public string JsonFilepathRelative => Path.Combine(Application.streamingAssetsPath.Replace(Application.dataPath, "Assets"), m_jsonFilename);
        public string JsonFilepathComplete => Path.Combine(Application.streamingAssetsPath, m_jsonFilename);

        public ScoresJSON Scores { get; private set; }
        public string CurrentPlayerName { get; set; }

        private List<ScoreboardLine> m_lines = new List<ScoreboardLine>();

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                Refresh();
            }
        }

        void Start()
        {
            Scores = LoadFromFile() ?? new ScoresJSON();
            if (autoStart)
            {
                Refresh();
            }
        }

        public void Save() => File.WriteAllText(JsonFilepathComplete, JsonUtility.ToJson(Scores, true));

        public ScoresJSON LoadFromFile()
        {
            try
            {
                return JsonUtility.FromJson<ScoresJSON>(File.ReadAllText(JsonFilepathComplete));
            }
            catch
            {
                return null;
            }
        }

        public void ReloadFromFile()
        {
            Scores = LoadFromFile() ?? new ScoresJSON();
        }

        public void AddLine(string playerName, float score)
        {
            var list = Scores.scores.ToList();
            list.Add(new ScoreItemJSON()
            {
                name = playerName,
                score = score,
                Date = DateTime.Now,
            });
            Scores.scores = list.ToArray();
            Refresh();
        }

        public void AddLine(float score)
        {
            if (!string.IsNullOrEmpty(CurrentPlayerName))
            {
                AddLine(CurrentPlayerName, score);
            }
        }

        public void RemoveLine(int index)
        {
            if(index < 0 && index >= Scores.scores.Length) { return; }
            var list = Scores.scores.ToList();
            list.RemoveAt(index);
            Scores.scores = list.ToArray();
            Refresh();
        }

        public void RemoveAllLines(string playerName)
        {
            Scores.scores = Scores.scores.Where(s => s.name != playerName).ToArray();
            Refresh();
        }

        public void RemoveFirstLine(string playerName)
        {
            var first = Scores.scores.FirstOrDefault(s => s.name == playerName);
            if(first != null)
            {
                var list = Scores.scores.ToList();
                list.Remove(first);
                Scores.scores = list.ToArray();
                Refresh();
            }
        }

        public void Refresh()
        {
            foreach(var line in m_lines)
            {
                if (line)
                {
                    Destroy(line.gameObject);
                }
            }
            m_lines.Clear();

            switch (m_ordering)
            {
                case Ordering.DescendingByScore:
                    Scores.OrderByScore();
                    break;
                case Ordering.AscendingByName:
                    Scores.OrderByName();
                    break;
                case Ordering.DescendingByDate:
                    Scores.OrderByDate();
                    break;
            }

            int index = 1;
            foreach (var item in Scores.scores)
            {
                var line = Instantiate(lineSample);
                m_lines.Add(line);
                line.gameObject.SetActive(true);
                line.AutoFillIn(item);
                line.Index = index.ToString();
                line.transform.SetParent(container.transform);

                index++;
            }
        }

        public void OrderByScore()
        {
            CurrentOrdering = Ordering.DescendingByScore;
        }

        public void OrderByName()
        {
            CurrentOrdering = Ordering.AscendingByName;
        }

        public void OrderByDate()
        {
            CurrentOrdering = Ordering.DescendingByDate;
        }
    }
}
