using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TXT.WEAVR.Player
{ 
    public interface ISceneLoadingListener
    {
        /// <summary>
        /// Called when old scenes have been unloaded
        /// </summary>
        /// <returns></returns>
        Task OnScenesUnload();
        /// <summary>
        /// Called when all scene have been loaded
        /// </summary>
        /// <returns></returns>
        Task OnScenesLoaded();
        void ProgressUpdate(float progress);
    }
}
