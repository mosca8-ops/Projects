namespace TXT.WEAVR.Editor
{

    public interface ICopyable
    {
        bool CopyFrom(object source);
    }

    public interface IRemovable<T>
    {
        T OnRemove();
    }

    //public interface ISaveAsAsset
    //{
    //    void SavedAsAsset();
    //}

    public interface ISelectable
    {
        bool IsSelected { get; set; }
    }

    //public interface ICopyable
    //{
    //    bool CopyFrom(object source);
    //}

    public interface IHighlighted
    {

    }

    //public interface IRemovable<T>
    //{
    //    T OnRemove();
    //}

    public interface ISaveAsAsset
    {
        void OnSavedAsAsset();
    }

}

namespace TXT.WEAVR
{
    public interface IEditorWindowClient
    {
        UnityEditor.EditorWindow Window { get; set; }
    }

    public interface IHasDescription
    {
        string Description { get; }
    }
}