using System;
using TXT.WEAVR.Localization;

namespace TXT.WEAVR.Player.Controller
{

    public abstract class BaseController : IController, IDisposable
    {
        // Just a quick shortcut
        protected static string Translate(string value) => LocalizationManager.Translate(value);

        protected IDataProvider DataProvider { get; private set; }

        public virtual void Dispose()
        {
            
        }

        public BaseController(IDataProvider provider)
        {
            DataProvider = provider;
        }
    }
}
