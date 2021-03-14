using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

namespace TXT.WEAVR.Player.Views
{
    public class ClearCachePage : BaseView
    {
        // STATIC PART??

        [Header("Clear Cache Page Items")]
        [Tooltip("Cancel clear cache button")]
        [Draggable]
        public Button CancelClearCacheButton;
        [Tooltip("Reset clear cache button")]
        [Draggable]
        public Button ResetClearCacheButton;
    }
}
