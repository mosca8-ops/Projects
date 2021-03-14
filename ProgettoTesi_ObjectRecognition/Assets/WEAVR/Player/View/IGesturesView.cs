using System;
using TXT.WEAVR.InteractionUI;

namespace TXT.WEAVR.Player.Views
{
    public interface IGesturesView : IView
    {
        IInteractablePanel GetPanel();
    }
}
