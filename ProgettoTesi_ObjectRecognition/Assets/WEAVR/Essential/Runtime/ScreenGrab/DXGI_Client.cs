using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.ScreenGrab
{
    [Serializable]
    public class DXGI_Data
    {
        public string id;
        public int monitor;
        public Vector2 offset;
        public Vector2 size;
        public Vector2Int flipAxis;

        public Vector2 NormalizedOffset(Texture2D texture)
        {
            return new Vector2(offset.x / texture.width, offset.y / texture.height);
        }

        public Vector2 NormalizedSize(Texture2D texture)
        {
            return new Vector2(size.x / texture.width, size.y / texture.height);
        }

        public Vector2 NormalizedFlippedOffset(Texture2D texture)
        {
            return new Vector2((flipAxis.x != 0 ? (size.x + offset.x - texture.width) : offset.x) / texture.width,
                               (flipAxis.y != 0 ? (size.y + offset.y - texture.height) : offset.y) / texture.height);
        }

        public Vector2 NormalizedFlippedSize(Texture2D texture)
        {
            return new Vector2((size.x / texture.width) * (flipAxis.x != 0 ? -1 : 1), (size.y / texture.height) * (flipAxis.y != 0 ? -1 : 1));
        }
    }

    [Serializable]
    public class DXGI_GroupData
    {
        public string groupId;
        public DXGI_Data[] data;
    }

    public interface IDXGI_DataClient
    {
        string Id { get; }
        DXGI_Data Data { get; set; }
    }

    public interface IDXGI_GroupDataClient
    {
        string Id { get; }
        DXGI_GroupData GroupData { get; set; }
    }
}
