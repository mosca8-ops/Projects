using System;
using System.Threading.Tasks;

namespace TXT.WEAVR.Player.Views
{

    public interface IPopup
    {
        void Show();
        Task ShowAsync();
        void Hide();
    }
}
