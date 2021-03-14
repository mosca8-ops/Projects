
using System;
using System.Threading.Tasks;
using TXT.WEAVR.Player.Views;
using UnityEngine;

namespace TXT.WEAVR.Player.Controller
{

    public class CoreController : BaseController, ICoreController
    {
        public IControlsView Controls { get; private set; }
        public Action OnBack { get; set; }

        public CoreController(IDataProvider provider) : base(provider)
        {
            Controls = provider.GetView<IControlsView>();

            SetupControlsView();
        }

        private void SetupControlsView()
        {
            Controls.OnQuit -= Controls_OnQuit;
            Controls.OnQuit += Controls_OnQuit;
            Controls.OnBack -= Controls_OnBack;
            Controls.OnBack += Controls_OnBack;
            Controls.OnLogout -= Controls_OnLogout;
            Controls.OnLogout += Controls_OnLogout;
            Controls.OnSettings -= Controls_OnSettings;
            Controls.OnSettings += Controls_OnSettings;

            Controls.OnMuteAudio -= Controls_OnMuteAudio;
            Controls.OnMuteAudio += Controls_OnMuteAudio;
            Controls.OnUnmuteAudio -= Controls_OnUnmuteAudio;
            Controls.OnUnmuteAudio += Controls_OnUnmuteAudio;
        }

        private void Controls_OnQuit()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.WebGLPlayer:
                    Application.OpenURL("about:blank");
                    break;
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.LinuxEditor:
                case RuntimePlatform.OSXEditor:
                    //UnityEditor.EditorApplication.isPlaying = false;
                    Application.Quit();
                    break;
                default:
                    Application.Quit();
                    break;
            }
        }

        private void Controls_OnSettings()
        {
            DataProvider.GetController<ISettingsController>().ShowView();
        }

        private async void Controls_OnLogout()
        {
            await DataProvider.GetController<IAuthenticationController>().Logout();
            await PopupManager.ShowInfoAsync(Translate(WeavrPlayer.Labels.Info), Translate("Logout Successful"));
            await DataProvider.GetController<IAuthenticationController>().Login();
        }

        private void Controls_OnBack(IView iView)
        {
            Back();
        }

        public void Controls_OnMuteAudio()
        {
            AudioListener.volume = 0;
        }

        public void Controls_OnUnmuteAudio()
        {
            AudioListener.volume = 1;
        }

        public async void Start()
        {
            DataProvider.GetView<IWelcomeView>()?.Hide();
            await DataProvider.GetController<IAuthenticationController>().Login();
            DataProvider.GetView<IWelcomeView>()?.Show();
            await Task.Delay(500);
            await DataProvider.GetController<ILibraryController>().ShowLibrary();
            DataProvider.GetView<IWelcomeView>()?.Hide();
        }

        public void SetOnBackCallback(Action callback)
        {
            OnBack = callback;
        }

        public void Restart()
        {
            throw new NotImplementedException();
        }

        public void Back()
        {
            // If there is already a logic for back, do it
            if (OnBack != null)
            {
                OnBack();
                OnBack = null;
                return;
            }
            // Otherwise backtrack in view history
            var (prevView, historyIndex) = DataProvider.ViewManager.GetViewFromHistory(-1);
            if (prevView != null)
            {
                switch (prevView)
                {
                    case ISettingsView view:
                        DataProvider.ViewManager.BacktrackHistoryToIndex(historyIndex, true);
                        DataProvider.GetController<ISettingsController>().ShowView();
                        break;
                }
            }
        }
    }
}
