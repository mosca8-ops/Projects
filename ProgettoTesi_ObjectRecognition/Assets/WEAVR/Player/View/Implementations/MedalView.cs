using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

namespace TXT.WEAVR.Player.Views
{
    public class MedalView : BaseView, IMedalView
    {
        // STATIC PART??

        [Header("Medal Page Items")]
        [Tooltip("User name - TMP text UI")]
        [Draggable]
        public TextMeshProUGUI userNameCongrats;
        [Tooltip("Proocedure name - TMP text UI")]
        [Draggable]
        public TextMeshProUGUI procedureName;
        [Tooltip("Medal Finish button")]
        [Draggable]
        public Button medalFinishButton;
        [Tooltip("Medal restart button")]
        [Draggable]
        public Button medalRestartButton;
        [Header("Stats")]
        [Tooltip("User procedure time - TMP text UI")]
        [Draggable]
        public TextMeshProUGUI userTime;
        [Tooltip("User score - TMP text UI")]
        [Draggable]
        public TextMeshProUGUI userScore;
        [Header("Medals")]
        [Tooltip("Gold medal image")]
        [Draggable]
        public Image goldMedal; // TODO: to activate/deactivate
        [Tooltip("Silver medal image")]
        [Draggable]
        public Image silverMedal; // TODO: to activate/deactivate
        [Tooltip("Bronze medal image")]
        [Draggable]
        public Image bronzeMedal; // TODO: to activate/deactivate

        public string UserNameCongrats { get => userNameCongrats.text; set => userNameCongrats.text = value; }
        public string ProcedureName { get => procedureName.text; set => procedureName.text = value; }
        public string UserTime { get => userTime.text; set => userTime.text = value; }
        public string UserScore { get => userScore.text; set => userScore.text = value; }

        public event UnityAction MedalFinish
        {
            add => medalFinishButton.onClick.AddListener(value);
            remove => medalFinishButton.onClick.RemoveListener(value);
        }

        public event UnityAction MedalRestart
        {
            add => medalRestartButton.onClick.AddListener(value);
            remove => medalRestartButton.onClick.RemoveListener(value);
        }
    }
}

