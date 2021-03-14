namespace TXT.WEAVR.Common
{

    public interface IDoor
    {
        void Lock();
        void Unlock();
        void Open();
        void Close();

        bool IsClosed { get; }
        bool IsLocked { get; }
    }
}
