namespace TXT.WEAVR.Legacy {
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Text;
    using System.Reflection;
    using TXT.WEAVR.Common;
    using TXT.WEAVR.Core;
    using TXT.WEAVR.Utility;
    using UnityEngine;

    public enum ExecutionMode {
        [ShortName("N")]
        None = 0,
        [ShortName("A")]
        Automatic = 1,
        [ShortName("G")]
        Guided = 2,
        [ShortName("F")]
        Feedback = 4
    }


    public static class ExecutionModeHelper {
        private static ExecutionMode[] _validModes;
        private static Dictionary<ExecutionMode, string> _shortNames;

        public static ExecutionMode Parse(string mode) {
            ExecutionMode execMode = ExecutionMode.None;
            if (string.IsNullOrEmpty(mode)) {
                return execMode;
            }
            foreach (var split in mode.Split('|', ',')) {
                var parsed = Enum.Parse(typeof(ExecutionMode), split.Trim());
                if (parsed != null) {
                    execMode |= (ExecutionMode)parsed;
                }
            }
            return execMode;
        }

        public static ExecutionMode Parse(int mode) {
            return mode < 0 ? (ExecutionMode)(-mode) : (ExecutionMode)mode;
        }

        public static ExecutionMode Parse(string[] modes) {
            ExecutionMode execMode = ExecutionMode.None;
            if (modes == null || modes.Length == 0) {
                return execMode;
            }
            foreach (var mode in modes) {
                var parsed = Enum.Parse(typeof(ExecutionMode), mode.Trim());
                if (parsed != null) {
                    execMode |= (ExecutionMode)parsed;
                }
            }
            return execMode;
        }

        public static ExecutionMode Parse(int[] modes) {
            ExecutionMode execMode = ExecutionMode.None;
            foreach (var mode in modes) {
                execMode |= mode < 0 ? (ExecutionMode)(-mode) : (ExecutionMode)mode;
            }
            return execMode;
        }

        public static bool Contains(this ExecutionMode mode, ExecutionMode flag) {
            return (mode & flag) == flag;
        }

        public static bool Intersects(this ExecutionMode mode, ExecutionMode other) {
            return (mode & other) != 0;
        }

        public static string ShortName(this ExecutionMode mode) {
            string name = null;
            if (_shortNames == null) {
                _shortNames = new Dictionary<ExecutionMode, string>();
            }
            if (!_shortNames.TryGetValue(mode, out name)) {
                //var attributes = mode.GetType().GetMember(mode.ToString())[0].GetCustomAttributes(typeof(ShortNameAttribute), true);
                //name = attributes.Length > 0 ? (attributes[0] as ShortNameAttribute).ShortName : "";
                //_shortNames.Add(mode, name);
                var attribute = mode.GetType().GetMember(mode.ToString())[0].GetCustomAttribute<ShortNameAttribute>(true);
                name = attribute != null ? (attribute as ShortNameAttribute).ShortName : "";
                _shortNames.Add(mode, name);
            }
            return name;
        }

        public static string ToFullString(this ExecutionMode mode) {
            StringBuilder fullString = new StringBuilder();
            foreach (ExecutionMode flag in Enum.GetValues(typeof(ExecutionMode))) {
                if (flag != ExecutionMode.None && (mode & flag) == flag) {
                    fullString.Append(" | ").Append(flag.ToString());
                }
            }
            if (fullString.Length > 0) {
                return fullString.Remove(0, 3).ToString();
            }
            return null;
        }

        public static bool IsValid(this ExecutionMode mode) {
            return mode != ExecutionMode.None;
        }

        public static ExecutionMode[] GetValidModes() {
            if (_validModes == null) {
                List<ExecutionMode> validModes = new List<ExecutionMode>();
                foreach (ExecutionMode en in Enum.GetValues(typeof(ExecutionMode))) {
                    if (en.IsValid()) {
                        validModes.Add(en);
                    }
                }
                _validModes = validModes.ToArray();
            }
            return _validModes;
        }
    }
}