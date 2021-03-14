using System;

namespace TXT.WEAVR.Editor
{
    [AttributeUsage(AttributeTargets.Class)]
    public class InitializeOnEditorStartAttribute : Attribute
    {
        public string MethodName { get; private set; }

        public InitializeOnEditorStartAttribute() { }
        public InitializeOnEditorStartAttribute(string methodName) { MethodName = methodName; }
    }
}