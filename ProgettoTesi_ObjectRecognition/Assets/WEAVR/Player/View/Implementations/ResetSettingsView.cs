using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

namespace TXT.WEAVR.Player.Views
{
    public class ResetSettingsPage : BaseView
    {
        // STATIC PART??

        [Header("Reset Settings Page Items")]
        [Tooltip("Cancel reset settings button")]
        [Draggable]
        public Button CancelResetSettButton;
        [Tooltip("Reset reset settings button")]
        [Draggable]
        public Button ResetResetSettButton;
    }
}
