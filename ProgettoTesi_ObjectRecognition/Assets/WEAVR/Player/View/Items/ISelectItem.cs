namespace TXT.WEAVR.Player.Views
{
    public delegate void OnSelectedDelegate(ISelectItem item);

    public interface ISelectItem : IViewItem
    {
        bool IsSelected { get; set; }

        event OnSelectedDelegate OnSelected;
    }
}

