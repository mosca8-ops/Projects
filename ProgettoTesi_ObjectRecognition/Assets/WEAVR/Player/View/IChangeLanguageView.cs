using System;
using System.Collections.Generic;
using TXT.WEAVR.Localization;

namespace TXT.WEAVR.Player.Views
{
    public interface IChangeLanguageView : IView
    {
        List<Language> Languages { get; set; }
        Language SelectedLanguage { get; set; }

        event Action<Language> OnLanguageSelected;
    }
}

