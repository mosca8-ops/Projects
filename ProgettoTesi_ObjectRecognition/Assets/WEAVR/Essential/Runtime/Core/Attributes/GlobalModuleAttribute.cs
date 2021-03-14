namespace TXT.WEAVR
{
    using System;

    [AttributeUsage(AttributeTargets.Class)]
    public class GlobalModuleAttribute : Attribute
    {
        private static char[] _SEPARATOR = new char[] { ';' };
        public readonly string ModuleName;
        public readonly string Description;
        public readonly string[] Dependencies;

        /// <summary>
        /// Register this module to the global modules
        /// </summary>
        /// <param name="moduleName">The module name</param>
        /// <param name="dependencies">The dependencies separated by semicolon ';'</param>
        public GlobalModuleAttribute(string moduleName, string description, string dependencies = "") {
            ModuleName = moduleName;
            Description = description;
            Dependencies = (dependencies ?? "").Split(_SEPARATOR, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}