using System;

namespace TXT.WEAVR.Player.Views
{


    public interface IControlsView : IView
    {
        event Action OnQuit;
        event Action OnLogout;
        event Action OnSettings;
        event Action OnHome;
        event Action OnNotifications;
        event Action OnCollaboration;

        event Action OnMuteAudio;
        event Action OnUnmuteAudio;

        event OnUserActionDelegate OnUserAction;
    }
}
