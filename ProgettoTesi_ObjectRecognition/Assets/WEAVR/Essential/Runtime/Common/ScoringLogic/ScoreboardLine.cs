using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace TXT.WEAVR.Common
{

    [AddComponentMenu("WEAVR/Scoring System/Scoreboard Line")]
    public class ScoreboardLine : MonoBehaviour
    {
        [SerializeField]
        private Text m_index;
        [SerializeField]
        private Text m_name;
        [SerializeField]
        private Text m_score;
        [SerializeField]
        private Text m_date;

        public string Index
        {
            get => m_index ? m_index.text : string.Empty;
            set { if (m_index) m_index.text = value; }
        }

        public string PlayerName
        {
            get => m_name ? m_name.text : string.Empty;
            set { if (m_name) m_name.text = value; }
        }

        public float Score
        {
            get => m_score && float.TryParse(m_score.text, NumberStyles.Float, CultureInfo.InvariantCulture, out float result) ? result : float.NaN;
            set { if (m_score) m_score.text = value.ToString(); }
        }

        public string Date
        {
            get => m_date ? m_date.text : string.Empty;
            set { if (m_date) m_date.text = value; }
        }

        public void AutoFillIn(Scoreboard.ScoreItemJSON item)
        {
            PlayerName = item.name;
            Score = item.score;
            Date = item.Date.ToString();
        }
    }
}
