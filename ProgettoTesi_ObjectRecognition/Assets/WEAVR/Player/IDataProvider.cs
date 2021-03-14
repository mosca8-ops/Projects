using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using TXT.WEAVR.Player.Controller;
using TXT.WEAVR.Player.DataSources;
using TXT.WEAVR.Player.Model;
using TXT.WEAVR.Player.Views;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TXT.WEAVR.Player
{

    public interface IDataProvider
    {
        IViewManager ViewManager { get; }
        IDownloadManager DownloadManager { get; }
        ICacheManager CacheManager { get; }

        T GetView<T>() where T : IView;
        T GetModel<T>() where T : IModel;
        T GetController<T>() where T : IController;

        void SavePersistentData<T>(string key, T value);
        T GetPersistentData<T>(string key);

        void ClearData(string key);
    }
}
