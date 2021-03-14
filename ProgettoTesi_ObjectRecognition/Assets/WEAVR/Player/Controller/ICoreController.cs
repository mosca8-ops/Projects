using System;

namespace TXT.WEAVR.Player.Controller
{
    public interface ICoreController : IController
    {
        void Start();
        void Restart();
        void SetOnBackCallback(Action callback);
        void Back();
    }
}