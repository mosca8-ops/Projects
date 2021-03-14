namespace TXT.WEAVR.Core
{
    public interface IPropertyCache
    {
        string ModuleId { get; }
        bool TryGetProperty(object owner, string propertyPath, out Property cachedProperty);
    }
}