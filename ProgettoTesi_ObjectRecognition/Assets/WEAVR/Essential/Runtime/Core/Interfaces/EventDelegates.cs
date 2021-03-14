namespace TXT.WEAVR.Core
{
    public delegate void OnValueChanged<T>(T value);
    public delegate void OnValueChanged<S, T>(S sender, T newData);
    public delegate void OnDataChanged<S, T>(S sender, T previous, T current);
    public delegate void StatusChanged<S>(S sender);
}
