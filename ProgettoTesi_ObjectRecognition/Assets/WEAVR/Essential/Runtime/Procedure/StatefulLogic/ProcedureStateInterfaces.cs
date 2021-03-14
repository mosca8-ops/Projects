
namespace TXT.WEAVR.Procedure
{
    public interface ISnapshotCallbackReceiver
    {
        void OnBeforeSnapshot();
        void OnAfterSnapshot();
    }

    public interface IRestoreCallbackReceiver
    {
        void OnBeforeRestore();
        void OnAfterRestore();
    }
}
