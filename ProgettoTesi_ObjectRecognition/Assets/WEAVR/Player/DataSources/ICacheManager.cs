using System;
using System.Threading.Tasks;
using System.Threading;
using TXT.WEAVR.Communication;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Player.DataSources
{
    public interface ICacheManager
    {
        Task<Texture2D> GetTexture(string url, Action<float> progresser = null);
        void ClearCache();
    }

    public interface ICacheUser
    {
        ICacheManager CacheManager { get; set; }
    }
}
