using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace TXT.WEAVR.Player
{
	public class AssetBundleAwaiter : INotifyCompletion
	{
		private AssetBundleCreateRequest asyncOp;
		private Action continuation;

		public AssetBundleAwaiter(AssetBundleCreateRequest asyncOp)
		{
			this.asyncOp = asyncOp;
			asyncOp.completed -= OnRequestCompleted;
			asyncOp.completed += OnRequestCompleted;
		}

		public bool IsCompleted { get { return asyncOp.isDone; } }

		public void GetResult() { }

		public void OnCompleted(Action continuation)
		{
			this.continuation = continuation;
		}

		private void OnRequestCompleted(AsyncOperation obj)
		{
			continuation();
		}
	}

	public static partial class ExtensionMethods
	{
		public static AssetBundleAwaiter GetAwaiter(this AssetBundleCreateRequest asyncOp)
		{
			return new AssetBundleAwaiter(asyncOp);
		}
	}
}