using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace TXT.WEAVR.Player.Views
{
    public struct PopupOption
    {
        public Texture2D image;
        public string text;

        public PopupOption(string text)
        {
            this.text = text;
            image = null;
        }

        public static implicit operator PopupOption(string s)
        {
            return new PopupOption()
            {
                text = s,
            };
        }
    }

    public interface IDropdownPopup : IPopup
    {
        void Show(string title, string description, int selectedOption, IEnumerable<PopupOption> options, Action<int> onSelection);
        Task<int> ShowAsync(string title, string description, int selectedOption, IEnumerable<PopupOption> options);
    }
}
