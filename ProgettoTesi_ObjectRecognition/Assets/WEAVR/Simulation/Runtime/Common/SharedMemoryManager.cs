namespace TXT.WEAVR.Simulation
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using TXT.WEAVR.Core;
    using UnityEngine;

    public enum SharedMemoryAccess
    {
        Read,
        Write,
        ReadWrite
    }

    public enum TextureStreamFormat
    {
        Alpha8,
        ARGB4444,
        RGB24,
        RGBA32,
        ARGB32,
        RGB565,
        R16,
        RGBA4444,
        BGRA32,
        RHalf,
        RGHalf,
        RGBAHalf,
        RFloat,
        RGFloat,
        RGBAFloat,
        RG16,
        R8,
    }

    [DoNotExpose]
    [AddComponentMenu("WEAVR/Simulation/Shared Memory Manager")]
    public class SharedMemoryManager : MonoBehaviour
    {

        #region [  Static Part  ]

        private static SharedMemoryManager _instance;

        public static SharedMemoryManager Instance {
            get {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<SharedMemoryManager>();
                    if (_instance == null)
                    {
                        // If no object is active, then create a new one
                        GameObject go = new GameObject("SharedMemoryManager");
                        _instance = go.AddComponent<SharedMemoryManager>();
                        _instance.transform.SetParent(ObjectRetriever.WEAVR.transform, false);
                    }
                    _instance.Initialize();
                }
                return _instance;
            }
        }

        static readonly Dictionary<TextureStreamFormat, FormatInfo> _formats = new Dictionary<TextureStreamFormat, FormatInfo>() {
            {TextureStreamFormat.Alpha8,    new FormatInfo(TextureFormat.Alpha8,        1) },
            {TextureStreamFormat.ARGB4444,  new FormatInfo(TextureFormat.ARGB4444,      2) },
            {TextureStreamFormat.RGB24,     new FormatInfo(TextureFormat.RGB24,         3) },
            {TextureStreamFormat.RGBA32,    new FormatInfo(TextureFormat.RGBA32,        4) },
            {TextureStreamFormat.ARGB32,    new FormatInfo(TextureFormat.ARGB32,        4) },
            {TextureStreamFormat.RGB565,    new FormatInfo(TextureFormat.RGB565,        2) },
            {TextureStreamFormat.R16,       new FormatInfo(TextureFormat.R16,           2) },
            {TextureStreamFormat.RGBA4444,  new FormatInfo(TextureFormat.RGBA4444,      2) },
            {TextureStreamFormat.BGRA32,    new FormatInfo(TextureFormat.BGRA32,        4) },
            {TextureStreamFormat.RHalf,     new FormatInfo(TextureFormat.RHalf,         2) },
            {TextureStreamFormat.RGHalf,    new FormatInfo(TextureFormat.RGHalf,        4) },
            {TextureStreamFormat.RGBAHalf,  new FormatInfo(TextureFormat.RGBAHalf,      8) },
            {TextureStreamFormat.RFloat,    new FormatInfo(TextureFormat.RFloat,        4) },
            {TextureStreamFormat.RGFloat,   new FormatInfo(TextureFormat.RGFloat,       8) },
            {TextureStreamFormat.RGBAFloat, new FormatInfo(TextureFormat.RGBAFloat,     16) },
            {TextureStreamFormat.RG16,      new FormatInfo(TextureFormat.RG16,          2) },
            {TextureStreamFormat.R8,        new FormatInfo(TextureFormat.R8,            1) },
        };

    #endregion


        /// <summary>
        /// Writes a variable to the specified handle
        /// </summary>
        /// <param name="handle">Memory pointer where to write the variable</param>
        /// <param name="variable">The variable to write</param>
        public void WriteVariable(IntPtr handle, object variable)
        {
            Marshal.StructureToPtr(variable, handle, true);
        }

        /// <summary>
        /// Read from memory into the variable
        /// </summary>
        /// <param name="handle">Memory pointer where to read the variable</param>
        /// <param name="variable">The variable to read</param>
        public void ReadVariable(IntPtr handle, ref object variable)
        {
            Marshal.PtrToStructure(handle, variable);
        }

        /// <summary>
        /// Read from memory into the variable
        /// </summary>
        /// <param name="handle">Memory pointer where to read the variable</param>
        /// <param name="variable">The variable to read</param>
        /// <param name="fallbackType">The type of the variable in case the variable to read is null</param>
        /// <returns>The newly read variable value</returns>
        public object ReadVariable(IntPtr handle, object variable, Type fallbackType)
        {
            Type type = variable != null ? variable.GetType() : fallbackType;
            return Marshal.PtrToStructure(handle, type);
        }


        /// <summary>
        /// Gets a display handler which streams bitmaps from shared memory
        /// </summary>
        /// <param name="memoryId">The memory file name</param>
        /// <param name="size">The size of the texture</param>
        /// <param name="textureFormat">The format of the texture</param>
        /// <returns>The handler or null if creation failed</returns>
        public DisplayHandler GetDisplayHandler(string memoryId, Vector2Int size, TextureStreamFormat textureFormat)
        {
            IntPtr wShmPtr = TXT.WEAVR.Simulation.SharedMemory.WeavrShmCreateOrOpen(memoryId, 0);
            return new DisplayHandler(memoryId, wShmPtr, size, _formats[textureFormat]);
        }

        public void WriteTexture(IntPtr fileHandle, Texture2D texture)
        {

            texture.GetRawTextureData();
        }

        private void Initialize()
        {
          _instance = this;
        }

        // Use this for initialization
        void Awake()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            SharedMemory.WeavrShmCloseAll();
        }

        internal struct FormatInfo
        {
            public TextureFormat format;
            public int bytesPerPixel;

            public FormatInfo(TextureFormat format, int bytesPerPixel)
            {
                this.format = format;
                this.bytesPerPixel = bytesPerPixel;
            }
        }

        /// <summary>
        /// Class which handles the image streaming from Shared Memory
        /// </summary>
        public class DisplayHandler
        {
            private readonly IntPtr _shmPtr;
            private readonly byte[] _imageBytes;
            private readonly int _byteSize;

            public string MemoryId { get; private set; }
            public Texture2D Texture { get; private set; }

            internal DisplayHandler(string memoryId, IntPtr shmPtr, Vector2Int size, FormatInfo info)
            {
                MemoryId = memoryId;
                _shmPtr = shmPtr;
                _byteSize = size.x * size.y * info.bytesPerPixel;
                _imageBytes = new byte[_byteSize];
                Texture = new Texture2D(size.x, size.y, info.format, false);
            }

            /// <summary>
            /// Loads the shared memory content into the specified texture
            /// </summary>
            /// <param name="texture">The texture where to load the image data</param>
            /// <returns>True if operation succedeed, False otherwise</returns>
            public bool LoadTexture(Texture2D texture)
            {
                return _shmPtr != IntPtr.Zero
                    && UpdateImageBytes()
                    && LoadImage(texture);
            }

            /// <summary>
            /// Updates the internal texture with the shared memory content
            /// </summary>
            /// <returns>True if operation succedeed, False otherwise</returns>
            public bool UpdateTexture()
            {
                return UpdateImageBytes() && LoadImage();
            }

            private bool UpdateImageBytes()
            {
                Marshal.Copy(_shmPtr, _imageBytes, 0, (int)_byteSize);

                return true;
            }

            private bool LoadImage()
            {
                Texture.LoadRawTextureData(_imageBytes);
                Texture.Apply(false);
                return true;
            }

            private bool LoadImage(Texture2D texture)
            {
                if (_byteSize > texture.width * texture.height * 4) { return false; }
                texture.LoadRawTextureData(_imageBytes);
                texture.Apply(false);
                return true;
            }
        }
    }
}