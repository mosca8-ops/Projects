using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

using Object = UnityEngine.Object;

namespace TXT.WEAVR.Player
{
	/// <summary>
	/// This class is a wrapper for AssetBundle methods to be called using async/await pattern
	/// </summary>
	public class WeavrAssetBundle
	{

        #region [  STATIC PART  ]

        #region [  LOAD FROM FILE  ]

        /// <summary>
        /// Synchronously loads an <see cref="WeavrAssetBundle"/> from a file on disk.
        /// </summary>
        /// <param name="path">Path of the file on disk.</param>
        /// <returns>The <see cref="WeavrAssetBundle"/> requested</returns>
        public static WeavrAssetBundle LoadFromFile(string path) => new WeavrAssetBundle(AssetBundle.LoadFromFile(path));

        /// <summary>
        /// Synchronously loads an <see cref="WeavrAssetBundle"/> from a file on disk.
        /// </summary>
        /// <param name="path">Path of the file on disk.</param>
        /// <param name="crc">An optional CRC-32 checksum of the uncompressed content. If this is non-zero,
        ///    then the content will be compared against the checksum before loading it, and
        ///    give an error if it does not match.
        /// </param>
        /// <returns>The <see cref="WeavrAssetBundle"/> requested</returns>
        public static WeavrAssetBundle LoadFromFile(string path, uint crc) => new WeavrAssetBundle(AssetBundle.LoadFromFile(path, crc));

        /// <summary>
        /// Synchronously loads an <see cref="WeavrAssetBundle"/> from a file on disk.
        /// </summary>
        /// <param name="path">Path of the file on disk.</param>
        /// <param name="crc">An optional CRC-32 checksum of the uncompressed content. If this is non-zero,
        ///    then the content will be compared against the checksum before loading it, and
        ///    give an error if it does not match.
        /// </param>
        /// <param name="offset">An optional byte offset. This value specifies where to start reading the AssetBundle from.</param>
        /// <returns>The <see cref="WeavrAssetBundle"/> requested</returns>
        public static WeavrAssetBundle LoadFromFile(string path, uint crc, ulong offset) => new WeavrAssetBundle(AssetBundle.LoadFromFile(path, crc, offset));

        /// <summary>
        /// Asynchronously loads an <see cref="WeavrAssetBundle"/> from a file on disk.
        /// </summary>
        /// <param name="path">Path of the file on disk.</param>
        /// <returns>The <see cref="WeavrAssetBundle"/> requested</returns>
        public static async Task<WeavrAssetBundle> LoadFromFileAsync(string path)
            => await CreateBundleAsync(AssetBundle.LoadFromFileAsync(path), CancellationToken.None, null);

        /// <summary>
        /// Asynchronously loads an <see cref="WeavrAssetBundle"/> from a file on disk.
        /// </summary>
        /// <param name="path">Path of the file on disk.</param>
        /// <param name="crc">An optional CRC-32 checksum of the uncompressed content. If this is non-zero,
        ///    then the content will be compared against the checksum before loading it, and
        ///    give an error if it does not match.
        /// </param>
        /// <returns>The <see cref="WeavrAssetBundle"/> requested</returns>
        public static async Task<WeavrAssetBundle> LoadFromFileAsync(string path, uint crc)
            => await CreateBundleAsync(AssetBundle.LoadFromFileAsync(path, crc), CancellationToken.None, null);

        /// <summary>
        /// Asynchronously loads an <see cref="WeavrAssetBundle"/> from a file on disk.
        /// </summary>
        /// <param name="path">Path of the file on disk.</param>
        /// <param name="crc">An optional CRC-32 checksum of the uncompressed content. If this is non-zero,
        ///    then the content will be compared against the checksum before loading it, and
        ///    give an error if it does not match.
        /// </param>
        /// <param name="offset">An optional byte offset. This value specifies where to start reading the AssetBundle from.</param>
        /// <returns>The <see cref="WeavrAssetBundle"/> requested</returns>
        public static async Task<WeavrAssetBundle> LoadFromFileAsync(string path, uint crc, ulong offset)
            => await CreateBundleAsync(AssetBundle.LoadFromFileAsync(path, crc, offset), CancellationToken.None, null);

        /// <summary>
        /// Asynchronously loads an <see cref="WeavrAssetBundle"/> from a file on disk.
        /// </summary>
        /// <param name="path">Path of the file on disk.</param>
        /// <param name="progressUpdateCallback">The callback to notify the progress of the call. Default is Null</param>
        /// <param name="cancellationToken">[Optional] The <see cref="CancellationToken"/> to prematurely cancel the asynchronous call</param>
        /// <returns>The <see cref="WeavrAssetBundle"/> requested</returns>
        public static async Task<WeavrAssetBundle> LoadFromFileAsync(string path, Action<float> progressUpdateCallback, CancellationToken? cancellationToken = null)
            => await CreateBundleAsync(AssetBundle.LoadFromFileAsync(path), cancellationToken ?? CancellationToken.None, progressUpdateCallback);

        /// <summary>
        /// Asynchronously loads an <see cref="WeavrAssetBundle"/> from a file on disk.
        /// </summary>
        /// <param name="path">Path of the file on disk.</param>
        /// <param name="crc">An optional CRC-32 checksum of the uncompressed content. If this is non-zero,
        ///    then the content will be compared against the checksum before loading it, and
        ///    give an error if it does not match.
        /// </param>
        /// <param name="progressUpdateCallback">The callback to notify the progress of the call. Default is Null</param>
        /// <param name="cancellationToken">[Optional] The <see cref="CancellationToken"/> to prematurely cancel the asynchronous call</param>
        /// <returns>The <see cref="WeavrAssetBundle"/> requested</returns>
        public static async Task<WeavrAssetBundle> LoadFromFileAsync(string path, uint crc, Action<float> progressUpdateCallback, CancellationToken? cancellationToken = null)
            => await CreateBundleAsync(AssetBundle.LoadFromFileAsync(path, crc), cancellationToken ?? CancellationToken.None, progressUpdateCallback);

        /// <summary>
        /// Asynchronously loads an <see cref="WeavrAssetBundle"/> from a file on disk.
        /// </summary>
        /// <param name="path">Path of the file on disk.</param>
        /// <param name="crc">An optional CRC-32 checksum of the uncompressed content. If this is non-zero,
        ///    then the content will be compared against the checksum before loading it, and
        ///    give an error if it does not match.
        /// </param>
        /// <param name="offset">An optional byte offset. This value specifies where to start reading the AssetBundle from.</param>
        /// <param name="progressUpdateCallback">The callback to notify the progress of the call. Default is Null</param>
        /// <param name="cancellationToken">[Optional] The <see cref="CancellationToken"/> to prematurely cancel the asynchronous call</param>
        /// <returns>The <see cref="WeavrAssetBundle"/> requested</returns>
        public static async Task<WeavrAssetBundle> LoadFromFileAsync(string path, uint crc, ulong offset, Action<float> progressUpdateCallback, CancellationToken? cancellationToken = null)
            => await CreateBundleAsync(AssetBundle.LoadFromFileAsync(path, crc, offset), cancellationToken ?? CancellationToken.None, progressUpdateCallback);

        #endregion

        #region [  LOAD FROM BYTES  ]

        /// <summary>
        /// Synchronously loads an <see cref="WeavrAssetBundle"/> from provided byte array.
        /// </summary>
        /// <param name="binary">The bytes representing the asset bundle.</param>
        /// <param name="crc">An optional CRC-32 checksum of the uncompressed content. If this is non-zero,
        ///    then the content will be compared against the checksum before loading it, and
        ///    give an error if it does not match.
        /// </param>
        /// <returns>The <see cref="WeavrAssetBundle"/> requested</returns>
        public static WeavrAssetBundle LoadFromMemory(byte[] binary, uint crc) => new WeavrAssetBundle(AssetBundle.LoadFromMemory(binary, crc));

        /// <summary>
        /// Synchronously loads an <see cref="WeavrAssetBundle"/> from provided byte array.
        /// </summary>
        /// <param name="binary">The bytes representing the asset bundle.</param>
        /// <returns>The <see cref="WeavrAssetBundle"/> requested</returns>
        public static WeavrAssetBundle LoadFromMemory(byte[] binary) => new WeavrAssetBundle(AssetBundle.LoadFromMemory(binary));

        /// <summary>
        /// Asynchronously loads an <see cref="WeavrAssetBundle"/> from provided byte array.
        /// </summary>
        /// <param name="binary">The bytes representing the asset bundle.</param>
        /// <param name="crc">An optional CRC-32 checksum of the uncompressed content. If this is non-zero,
        ///    then the content will be compared against the checksum before loading it, and
        ///    give an error if it does not match.
        /// </param>
        /// <returns>The <see cref="WeavrAssetBundle"/> requested</returns>
        public static async Task<WeavrAssetBundle> LoadFromMemoryAsync(byte[] binary, uint crc)
            => await CreateBundleAsync(AssetBundle.LoadFromMemoryAsync(binary, crc), CancellationToken.None, null);

        /// <summary>
        /// Asynchronously loads an <see cref="WeavrAssetBundle"/> from provided byte array.
        /// </summary>
        /// <param name="binary">The bytes representing the asset bundle.</param>
        /// <returns>The <see cref="WeavrAssetBundle"/> requested</returns>
        public static async Task<WeavrAssetBundle> LoadFromMemoryAsync(byte[] binary)
            => await CreateBundleAsync(AssetBundle.LoadFromMemoryAsync(binary), CancellationToken.None, null);

        /// <summary>
        /// Asynchronously loads an <see cref="WeavrAssetBundle"/> from provided byte array.
        /// </summary>
        /// <param name="binary">The bytes representing the asset bundle.</param>
        /// <param name="crc">An optional CRC-32 checksum of the uncompressed content. If this is non-zero,
        ///    then the content will be compared against the checksum before loading it, and
        ///    give an error if it does not match.
        /// </param>
        /// <param name="progressUpdateCallback">The callback to notify the progress of the call. Default is Null</param>
        /// <param name="cancellationToken">[Optional] The <see cref="CancellationToken"/> to prematurely cancel the asynchronous call</param>
        /// <returns>The <see cref="WeavrAssetBundle"/> requested</returns>
        public static async Task<WeavrAssetBundle> LoadFromMemoryAsync(byte[] binary, uint crc, Action<float> progressUpdateCallback, CancellationToken? cancellationToken = null)
            => await CreateBundleAsync(AssetBundle.LoadFromMemoryAsync(binary, crc), cancellationToken ?? CancellationToken.None, progressUpdateCallback);

        /// <summary>
        /// Asynchronously loads an <see cref="WeavrAssetBundle"/> from provided byte array.
        /// </summary>
        /// <param name="binary">The bytes representing the asset bundle.</param>
        /// <param name="progressUpdateCallback">The callback to notify the progress of the call. Default is Null</param>
        /// <param name="cancellationToken">[Optional] The <see cref="CancellationToken"/> to prematurely cancel the asynchronous call</param>
        /// <returns>The <see cref="WeavrAssetBundle"/> requested</returns>
        public static async Task<WeavrAssetBundle> LoadFromMemoryAsync(byte[] binary, Action<float> progressUpdateCallback, CancellationToken? cancellationToken = null)
            => await CreateBundleAsync(AssetBundle.LoadFromMemoryAsync(binary), cancellationToken ?? CancellationToken.None, progressUpdateCallback);

        #endregion

        #region [  LOAD FROM STREAM  ]

        /// <summary>
        /// Synchronously loads an <see cref="WeavrAssetBundle"/> from provided stream.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> representing the asset bundle.</param>
        /// <param name="crc">An optional CRC-32 checksum of the uncompressed content. If this is non-zero,
        ///    then the content will be compared against the checksum before loading it, and
        ///    give an error if it does not match.
        /// </param>
        /// <returns>The <see cref="WeavrAssetBundle"/> requested</returns>
        public static WeavrAssetBundle LoadFromStream(Stream stream, uint crc) => new WeavrAssetBundle(AssetBundle.LoadFromStream(stream, crc));

        /// <summary>
        /// Synchronously loads an <see cref="WeavrAssetBundle"/> from provided stream.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> representing the asset bundle.</param>
        /// <returns>The <see cref="WeavrAssetBundle"/> requested</returns>
        public static WeavrAssetBundle LoadFromStream(Stream stream) => new WeavrAssetBundle(AssetBundle.LoadFromStream(stream));

        /// <summary>
        /// Synchronously loads an <see cref="WeavrAssetBundle"/> from provided stream.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> representing the asset bundle.</param>
        /// <param name="crc">An optional CRC-32 checksum of the uncompressed content. If this is non-zero,
        ///    then the content will be compared against the checksum before loading it, and
        ///    give an error if it does not match.
        /// </param>
        /// <param name="managedReadBufferSize">The size of the read buffer</param>
        /// <returns>The <see cref="WeavrAssetBundle"/> requested</returns>
        public static WeavrAssetBundle LoadFromStream(Stream stream, uint crc, uint managedReadBufferSize) => new WeavrAssetBundle(AssetBundle.LoadFromStream(stream, crc, managedReadBufferSize));

        /// <summary>
        /// Asynchronously loads an <see cref="WeavrAssetBundle"/> from provided stream.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> representing the asset bundle.</param>
        /// <param name="crc">An optional CRC-32 checksum of the uncompressed content. If this is non-zero,
        ///    then the content will be compared against the checksum before loading it, and
        ///    give an error if it does not match.
        /// </param>
        /// <returns>The <see cref="WeavrAssetBundle"/> requested</returns>
        public static async Task<WeavrAssetBundle> LoadFromStreamAsync(Stream stream, uint crc)
            => await CreateBundleAsync(AssetBundle.LoadFromStreamAsync(stream, crc), CancellationToken.None, null);

        /// <summary>
        /// Asynchronously loads an <see cref="WeavrAssetBundle"/> from provided stream.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> representing the asset bundle.</param>
        /// <returns>The <see cref="WeavrAssetBundle"/> requested</returns>
        public static async Task<WeavrAssetBundle> LoadFromStreamAsync(Stream stream)
            => await CreateBundleAsync(AssetBundle.LoadFromStreamAsync(stream), CancellationToken.None, null);

        /// <summary>
        /// Asynchronously loads an <see cref="WeavrAssetBundle"/> from provided stream.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> representing the asset bundle.</param>
        /// <param name="crc">An optional CRC-32 checksum of the uncompressed content. If this is non-zero,
        ///    then the content will be compared against the checksum before loading it, and
        ///    give an error if it does not match.
        /// </param>
        /// <param name="managedReadBufferSize">The size of the read buffer</param>
        /// <returns>The <see cref="WeavrAssetBundle"/> requested</returns>
        public static async Task<WeavrAssetBundle> LoadFromStreamAsync(Stream stream, uint crc, uint managedReadBufferSize)
            => await CreateBundleAsync(AssetBundle.LoadFromStreamAsync(stream, crc, managedReadBufferSize), CancellationToken.None, null);


        /// <summary>
        /// Asynchronously loads an <see cref="WeavrAssetBundle"/> from provided stream.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> representing the asset bundle.</param>
        /// <param name="crc">An optional CRC-32 checksum of the uncompressed content. If this is non-zero,
        ///    then the content will be compared against the checksum before loading it, and
        ///    give an error if it does not match.
        /// </param>
        /// <param name="progressUpdateCallback">The callback to notify the progress of the call. Default is Null</param>
        /// <param name="cancellationToken">[Optional] The <see cref="CancellationToken"/> to prematurely cancel the asynchronous call</param>
        /// <returns>The <see cref="WeavrAssetBundle"/> requested</returns>
        public static async Task<WeavrAssetBundle> LoadFromStreamAsync(Stream stream, uint crc, Action<float> progressUpdateCallback, CancellationToken? cancellationToken = null)
            => await CreateBundleAsync(AssetBundle.LoadFromStreamAsync(stream, crc), cancellationToken ?? CancellationToken.None, progressUpdateCallback);

        /// <summary>
        /// Asynchronously loads an <see cref="WeavrAssetBundle"/> from provided stream.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> representing the asset bundle.</param>
        /// <param name="progressUpdateCallback">The callback to notify the progress of the call. Default is Null</param>
        /// <param name="cancellationToken">[Optional] The <see cref="CancellationToken"/> to prematurely cancel the asynchronous call</param>
        /// <returns>The <see cref="WeavrAssetBundle"/> requested</returns>
        public static async Task<WeavrAssetBundle> LoadFromStreamAsync(Stream stream, Action<float> progressUpdateCallback, CancellationToken? cancellationToken = null)
            => await CreateBundleAsync(AssetBundle.LoadFromStreamAsync(stream), cancellationToken ?? CancellationToken.None, progressUpdateCallback);

        /// <summary>
        /// Asynchronously loads an <see cref="WeavrAssetBundle"/> from provided stream.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> representing the asset bundle.</param>
        /// <param name="crc">An optional CRC-32 checksum of the uncompressed content. If this is non-zero,
        ///    then the content will be compared against the checksum before loading it, and
        ///    give an error if it does not match.
        /// </param>
        /// <param name="managedReadBufferSize">The size of the read buffer</param>
        /// <param name="progressUpdateCallback">The callback to notify the progress of the call. Default is Null</param>
        /// <param name="cancellationToken">[Optional] The <see cref="CancellationToken"/> to prematurely cancel the asynchronous call</param>
        /// <returns>The <see cref="WeavrAssetBundle"/> requested</returns>
        public static async Task<WeavrAssetBundle> LoadFromStreamAsync(Stream stream, uint crc, uint managedReadBufferSize, Action<float> progressUpdateCallback, CancellationToken? cancellationToken = null)
            => await CreateBundleAsync(AssetBundle.LoadFromStreamAsync(stream, crc, managedReadBufferSize), cancellationToken ?? CancellationToken.None, progressUpdateCallback);

        #endregion

        private static async Task<WeavrAssetBundle> CreateBundleAsync(AssetBundleCreateRequest request, CancellationToken cancellationToken, Action<float> updateProgressCallback)
        {
            while(!request.isDone && !cancellationToken.IsCancellationRequested)
            {
                await Task.Yield();
                updateProgressCallback?.Invoke(request.progress);
            }
            return new WeavrAssetBundle(request.assetBundle);
        }

        #endregion

        private AssetBundle m_assetBundle;

        internal AssetBundle Bundle => m_assetBundle;

        /// <summary>
        /// Return true if the AssetBundle is a streamed Scene AssetBundle.
        /// </summary>
        public bool isStreamedSceneAssetBundle => m_assetBundle.isStreamedSceneAssetBundle;

        private WeavrAssetBundle(AssetBundle assetBundle)
        {
            m_assetBundle = assetBundle;
        }


        /// <summary>
        /// Check if an AssetBundle contains a specific object.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool Contains(string name) => m_assetBundle.Contains(name);

        /// <summary>
        /// Return all asset names in the AssetBundle.
        /// </summary>
        /// <returns></returns>
        public string[] GetAllAssetNames() => m_assetBundle.GetAllAssetNames();

        /// <summary>
        /// Return all the Scene asset paths (paths to *.unity assets) in the AssetBundle.
        /// </summary>
        /// <returns></returns>
        public string[] GetAllScenePaths() => m_assetBundle.GetAllScenePaths();

        /// <summary>
        /// Loads all assets contained in the asset bundle that inherit from type.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public Object[] LoadAllAssets(Type type) => m_assetBundle.LoadAllAssets(type);

        /// <summary>
        /// Loads all assets contained in the asset bundle.
        /// </summary>
        /// <returns></returns>
        public Object[] LoadAllAssets() => m_assetBundle.LoadAllAssets();

        /// <summary>
        /// Loads all assets contained in the asset bundle that inherit from type T.
        /// </summary>
        /// <returns></returns>
        public T[] LoadAllAssets<T>() where T : Object => m_assetBundle.LoadAllAssets<T>();

        /// <summary>
        /// Loads all assets contained in the asset bundle that inherit from type asynchronously.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="progressUpdateCallback"></param>
        /// <returns></returns>
        public async Task<Object[]> LoadAllAssetsAsync(Type type, CancellationToken? cancellationToken = null, Action<float> progressUpdateCallback = null)
            => await HandleAsyncMultiple(m_assetBundle.LoadAllAssetsAsync(type), cancellationToken ?? CancellationToken.None, progressUpdateCallback);

        public async Task<T[]> LoadAllAssetsAsync<T>(CancellationToken? cancellationToken = null, Action<float> progressUpdateCallback = null)
            where T : Object
            => await HandleAsyncMultiple<T>(m_assetBundle.LoadAllAssetsAsync<T>(), cancellationToken ?? CancellationToken.None, progressUpdateCallback);

        /// <summary>
        /// Loads all assets contained in the asset bundle that inherit from T asynchronously.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <param name="progressUpdateCallback"></param>
        /// <returns></returns>
        public async Task<Object[]> LoadAllAssetsAsync(CancellationToken? cancellationToken = null, Action<float> progressUpdateCallback = null)
            => await HandleAsyncMultiple(m_assetBundle.LoadAllAssetsAsync(), cancellationToken ?? CancellationToken.None, progressUpdateCallback);

        public T LoadAsset<T>(string name) where T : Object => m_assetBundle.LoadAsset<T>(name);

        /// <summary>
        /// Loads asset with name of type T from the bundle.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Object LoadAsset(string name) => m_assetBundle.LoadAsset(name);

        /// <summary>
        /// Loads asset with name of a given type from the bundle.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public Object LoadAsset(string name, Type type) => m_assetBundle.LoadAsset(name, type);

        /// <summary>
        /// Asynchronously loads asset with name of a given type from the bundle.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="progressUpdateCallback"></param>
        /// <returns></returns>
        public async Task<Object> LoadAssetAsync(string name, Type type, CancellationToken? cancellationToken = null, Action<float> progressUpdateCallback = null) 
            => await HandleAsyncSingle(m_assetBundle.LoadAssetAsync(name, type), cancellationToken ?? CancellationToken.None, progressUpdateCallback);
        public async Task<T> LoadAssetAsync<T>(string name, CancellationToken? cancellationToken = null, Action<float> progressUpdateCallback = null) where T : Object 
            => await HandleAsyncSingle<T>(m_assetBundle.LoadAssetAsync<T>(name), cancellationToken ?? CancellationToken.None, progressUpdateCallback);

        /// <summary>
        /// Asynchronously loads asset with name of a given T from the bundle.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="progressUpdateCallback"></param>
        /// <returns></returns>
        public async Task<Object> LoadAssetAsync(string name, CancellationToken? cancellationToken = null, Action<float> progressUpdateCallback = null) 
            => await HandleAsyncSingle(m_assetBundle.LoadAssetAsync(name), cancellationToken ?? CancellationToken.None, progressUpdateCallback);

        /// <summary>
        /// Loads asset and sub assets with name of a given type from the bundle.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public Object[] LoadAssetWithSubAssets(string name, Type type) => m_assetBundle.LoadAssetWithSubAssets(name, type);
        public T[] LoadAssetWithSubAssets<T>(string name) where T : Object => m_assetBundle.LoadAssetWithSubAssets<T>(name);

        /// <summary>
        /// Loads asset and sub assets with name of type T from the bundle.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Object[] LoadAssetWithSubAssets(string name) => m_assetBundle.LoadAssetWithSubAssets(name);

        /// <summary>
        /// Loads asset with sub assets with name of a given type from the bundle asynchronously.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public async Task<Object[]> LoadAssetWithSubAssetsAsync(string name, Type type, CancellationToken? cancellationToken = null, Action<float> progressUpdateCallback = null)
            => await HandleAsyncMultiple(m_assetBundle.LoadAssetWithSubAssetsAsync(name, type), cancellationToken ?? CancellationToken.None, progressUpdateCallback);

        /// <summary>
        /// Loads asset with sub assets with name of type T from the bundle asynchronously.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public async Task<T[]> LoadAssetWithSubAssetsAsync<T>(string name, CancellationToken? cancellationToken = null, Action<float> progressUpdateCallback = null) where T : Object
           => await HandleAsyncMultiple<T>(m_assetBundle.LoadAssetWithSubAssetsAsync<T>(name), cancellationToken ?? CancellationToken.None, progressUpdateCallback);

        /// <summary>
        /// Unloads assets in the bundle.
        /// </summary>
        /// <param name="unloadAllLoadedObjects"></param>
        public void Unload(bool unloadAllLoadedObjects) => m_assetBundle.Unload(unloadAllLoadedObjects);

        private async Task<T> HandleAsyncSingle<T>(AssetBundleRequest request, CancellationToken cancellationToken, Action<float> progressUpdater) where T : Object
        {
            var doneRequest = await HandleAsync(request, cancellationToken, progressUpdater);
            return doneRequest.asset as T;
        }

        private async Task<Object> HandleAsyncSingle(AssetBundleRequest request, CancellationToken cancellationToken, Action<float> progressUpdater)
        {
            var doneRequest = await HandleAsync(request, cancellationToken, progressUpdater);
            return doneRequest.asset;
        }

        private async Task<T[]> HandleAsyncMultiple<T>(AssetBundleRequest request, CancellationToken cancellationToken, Action<float> progressUpdater) where T : Object
        {
            var doneRequest = await HandleAsync(request, cancellationToken, progressUpdater);
            return doneRequest.allAssets.Cast<T>().ToArray();
        }

        private async Task<Object[]> HandleAsyncMultiple(AssetBundleRequest request, CancellationToken cancellationToken, Action<float> progressUpdater)
        {
            var doneRequest = await HandleAsync(request, cancellationToken, progressUpdater);
            return doneRequest.allAssets;
        }

        private async Task<AssetBundleRequest> HandleAsync(AssetBundleRequest request, CancellationToken cancellationToken, Action<float> progressUpdateCallback)
        {
            while(!request.isDone && !cancellationToken.IsCancellationRequested)
            {
                await Task.Yield();
                progressUpdateCallback?.Invoke(request.progress);
            }
            return request;
        }

        public void UnloadAllAssetsAsync(bool unloadAllLoadedObjects)
        {
            m_assetBundle.Unload(unloadAllLoadedObjects);
        }
    }
}