namespace TXT.WEAVR.Tools
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using UnityEditor;
    using UnityEditorInternal;
    using UnityEngine;

    public class DirectivesContainer
    {
        public List<DirectiveArray> PlatformDirectives { get; private set; }
        public DirectiveArray CommonDirectives { get; private set; }
        public DirectiveArray PlayerDirectives { get; private set; }
        public DirectiveArray UnityRelativeDirectives { get; private set; }
        public UnityVersionDictionary UnityVersion { get; private set; }

        public Dictionary<TextAsset, AssetDirective> TextAssets { get; private set; }

        #region Default Initialization
        private void InitializeDirectiveWrappers()
        {
            PlatformDirectives = new List<DirectiveArray>()
            {
                new DirectiveArray(BuildTargetGroup.Standalone)
                {
                    new DirectiveWrapper("UNITY_STANDALONE"),
                    new DirectiveWrapper("!UNITY_STANDALONE"),
                    new DirectiveWrapper("UNITY_STANDALONE_WIN"),
                    new DirectiveWrapper("!UNITY_STANDALONE_WIN"),
                    new DirectiveWrapper("UNITY_STANDALONE_OSX"),
                    new DirectiveWrapper("!UNITY_STANDALONE_OSX"),
                    new DirectiveWrapper("UNITY_STANDALONE_LINUX"),
                    new DirectiveWrapper("!UNITY_STANDALONE_LINUX"),
                },
                new DirectiveArray(BuildTargetGroup.iOS)
                {
                    new DirectiveWrapper("UNITY_IOS"),
                    new DirectiveWrapper("!UNITY_IOS"),
                },
                new DirectiveArray(BuildTargetGroup.Android)
                {
                    new DirectiveWrapper("UNITY_ANDROID"),
                    new DirectiveWrapper("!UNITY_ANDROID"),
                },
                new DirectiveArray(BuildTargetGroup.WebGL)
                {
                    new DirectiveWrapper("UNITY_WEBGL"),
                    new DirectiveWrapper("!UNITY_WEBGL"),
                },
                new DirectiveArray(BuildTargetGroup.WSA)
                {
                    new DirectiveWrapper("UNITY_WSA"),
                    new DirectiveWrapper("!UNITY_WSA"),
                    new DirectiveWrapper("UNITY_WSA_10_0"),
                    new DirectiveWrapper("!UNITY_WSA_10_0"),
                },
            };

            CommonDirectives = new DirectiveArray("Common Directives")
            {
                new DirectiveWrapper("ENABLE_MONO"),
                new DirectiveWrapper("!ENABLE_MONO"),
                new DirectiveWrapper("ENABLE_IL2CPP"),
                new DirectiveWrapper("!ENABLE_IL2CPP"),
                new DirectiveWrapper("NET_2_0"),
                new DirectiveWrapper("!NET_2_0"),
                new DirectiveWrapper("NET_2_0_SUBSET"),
                new DirectiveWrapper("!NET_2_0_SUBSET"),
                new DirectiveWrapper("NET_4_6"),
                new DirectiveWrapper("!NET_4_6"),
            };

            UnityRelativeDirectives = new DirectiveArray("Unity Related")
            {
                new DirectiveWrapper("", InternalEditorUtility.GetFullUnityVersion())
            };

            UnityVersion = new UnityVersionDictionary("Unity Version");

            PlayerDirectives = CreateCommonPlayerDirective(PlatformDirectives);

            //LoadSubDirectiveSymbols(PlatformDirectives);
        }
        #endregion

        public DirectivesContainer(params UnityEngine.Object[] assets)
        {
            InitializeDirectiveWrappers();
            TextAssets = new Dictionary<TextAsset, AssetDirective>();
            foreach (var asset in assets)
            {
                var textAsset = asset as TextAsset;
                if (textAsset != null)
                {
                    TextAssets[textAsset] = new AssetDirective();
                }
            }

            ComputeWrapperValues();
        }

        private void ComputeWrapperValues()
        {
            // First get all lines for all text assets
            Dictionary<TextAsset, AssetDirective> assetsWithLines = new Dictionary<TextAsset, AssetDirective>();
            foreach (var textAsset in TextAssets)
            {
                int directiveLines = 0;
                List<string> lines = new List<string>();
                StringReader stringReader = new StringReader(textAsset.Key.text);
                string line = stringReader.ReadLine();
                string directiveLine = "";
                while (line != null && line.TrimStart().StartsWith("#"))
                {
                    lines.Add(line + " ");
                    directiveLine += line;
                    line = stringReader.ReadLine();
                    directiveLines++;
                }
                assetsWithLines[textAsset.Key] = new AssetDirective();
                assetsWithLines[textAsset.Key].AddRange(lines);
                assetsWithLines[textAsset.Key].RemainingDirectives = directiveLine.Replace("#if", "").Trim() + " ";
                while (line != null)
                {
                    textAsset.Value.Add(line);
                    line = stringReader.ReadLine();
                }
                if ((textAsset.Value.Count - directiveLines) >= 0)
                {
                    textAsset.Value.RemoveRange(textAsset.Value.Count - directiveLines, directiveLines);
                }
            }

            // Then apply them to all directive wrappers
            foreach (var directiveArray in PlatformDirectives)
            {
                directiveArray.BatchSetValues(assetsWithLines);
            }

            CommonDirectives.BatchSetValues(assetsWithLines);
            PlayerDirectives.BatchSetValues(assetsWithLines);
            UnityRelativeDirectives.BatchSetValues(assetsWithLines);

            UnityVersion.ComputeBatchValue(assetsWithLines);

            foreach (var asset in assetsWithLines)
            {
                TextAssets[asset.Key].RemainingDirectives = asset.Value.RemainingDirectives;
            }
        }

        public void ApplyChanges()
        {
            StringBuilder stringBuilder;

            foreach (var asset in TextAssets)
            {
                stringBuilder = new StringBuilder();
                foreach (var directiveArray in PlatformDirectives)
                {
                    ApplyChanges(stringBuilder, asset.Key, directiveArray);
                }

                ApplyChanges(stringBuilder, asset.Key, CommonDirectives);

                ApplyChanges(stringBuilder, asset.Key, PlayerDirectives);

                ApplyChanges(stringBuilder, asset.Key, UnityRelativeDirectives);

                var versionString = UnityVersion.ApplyFor(asset.Key);
                if (!string.IsNullOrEmpty(versionString))
                {
                    stringBuilder.Append(" && ").Append(versionString);
                }

                asset.Value.RemainingDirectives = asset.Value.RemainingDirectives.Trim().Replace("&&", "&").Replace("||", "|");
                var andOperands = asset.Value.RemainingDirectives.Trim().Split(new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries);

                if (andOperands.Length > 0)
                {
                    foreach (var andOperand in andOperands)
                    {
                        var andOperandTrimmed = andOperand.Trim();
                        if (string.IsNullOrEmpty(andOperandTrimmed))
                        {
                            continue;
                        }
                        var orOperands = andOperandTrimmed.Split(new char[] { '|', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (orOperands.Length > 1)
                        {
                            stringBuilder.Append(" && (");
                            foreach (var orOperand in orOperands)
                            {
                                stringBuilder.Append(orOperand.Trim()).Append(" ||");
                            }
                            stringBuilder.Remove(stringBuilder.Length - 3, 3).Append(") ");
                        }
                        else
                        {
                            stringBuilder.Append(" && ").Append(andOperandTrimmed).Append(' ');
                        }
                    }
                }

                bool wrapWithDirectives = stringBuilder.Length > 0;

                if (wrapWithDirectives)
                {
                    stringBuilder.AppendLine();
                }

                foreach (var line in asset.Value)
                {
                    stringBuilder.AppendLine(line);
                }

                if (wrapWithDirectives)
                {
                    stringBuilder.AppendLine("#endif");
                    AssetsUtility.WriteAllToTextAsset(asset.Key, "#if " + stringBuilder.ToString().Substring(3));
                }
                else
                {
                    AssetsUtility.WriteAllToTextAsset(asset.Key, stringBuilder.ToString());
                }
            }

            AssetDatabase.Refresh();
        }

        private static void ApplyChanges(StringBuilder stringBuilder, TextAsset asset, DirectiveArray directiveArray)
        {
            foreach (var directiveWrapper in directiveArray)
            {
                if (directiveWrapper.Value)
                {
                    string directive = directiveWrapper.ApplyFor(asset);
                    if (!string.IsNullOrEmpty(directive))
                    {
                        stringBuilder.Append(" && ").Append(directive);
                    }
                }
            }
        }

        private void LoadSubDirectiveSymbols(IEnumerable<DirectiveArray> arrays)
        {
            foreach (var array in arrays)
            {
                if (array.BuildTarget.HasValue)
                {
                    var subDirectives = PlayerSettings.GetScriptingDefineSymbolsForGroup(array.BuildTarget.Value).ToString().Split(';');
                    foreach (var subDirective in subDirectives)
                    {
                        if (!string.IsNullOrEmpty(subDirective))
                        {
                            array.Add(new DirectiveWrapper(subDirective));
                            array.Add(new DirectiveWrapper("!" + subDirective));
                        }
                    }
                }
            }
        }

        private DirectiveArray CreateCommonPlayerDirective(IEnumerable<DirectiveArray> arrays)
        {
            var symbols = arrays.Where(a => a.BuildTarget.HasValue)
                                .SelectMany(a => PlayerSettings.GetScriptingDefineSymbolsForGroup(a.BuildTarget.Value).ToString().Split(';'))
                                .Where(s => !string.IsNullOrEmpty(s)).Distinct();
            var array = new DirectiveArray("Player Directives");
            foreach(var symbol in symbols)
            {
                array.Add(new DirectiveWrapper(symbol));
                array.Add(new DirectiveWrapper("!" + symbol));
            }
            return array;
        }

        public class AssetDirective : List<string>
        {
            public string RemainingDirectives { get; set; }
        }

        public class DirectiveArray : List<DirectiveWrapper>
        {
            public string Label { get; private set; }
            public BuildTargetGroup? BuildTarget { get; private set; }

            public bool Visible { get; set; }

            public DirectiveArray(string label) : base()
            {
                Label = label;
            }

            public DirectiveArray(BuildTargetGroup targetGroup) : base()
            {
                Label = targetGroup.ToString();
                BuildTarget = targetGroup;
            }

            public void BatchSetValues(Dictionary<TextAsset, AssetDirective> assetsWithLines)
            {
                foreach (var directiveWrapper in this)
                {
                    directiveWrapper.ComputeBatchValue(assetsWithLines);
                    Visible |= directiveWrapper.Value || directiveWrapper.IsMixedValue;
                }
            }
        }

        public class DirectiveWrapper
        {
            public bool IsMixedValue { get; set; }

            public bool Value
            {
                get
                {
                    return _value ?? false;
                }
                set
                {
                    if (_value.HasValue && _value.Value != value && IsMixedValue)
                    {
                        IsMixedValue = false;
                    }
                    _value = value;
                }
            }

            public string Label { get; set; }
            public string DirectiveName { get; set; }

            protected HashSet<TextAsset> _presenceAssets;

            protected bool? _value;

            public DirectiveWrapper(string directiveName, string label = null)
            {
                DirectiveName = !string.IsNullOrEmpty(directiveName) ? directiveName.ToUpper() : "";
                Label = label;
                if (Label == null)
                {
                    Label = directiveName;
                }
                _value = null;
                _presenceAssets = new HashSet<TextAsset>();
            }

            public virtual void ComputeBatchValue(Dictionary<TextAsset, AssetDirective> assetsWithLines)
            {
                if (assetsWithLines == null || assetsWithLines.Count == 0)
                {
                    return;
                }
                foreach (var assetPair in assetsWithLines)
                {
                    bool includeFile = false;
                    if (assetPair.Value.Count == 0)
                    {
                        IsMixedValue |= _value.HasValue && _value.Value;
                        continue;
                    }
                    foreach (var line in assetPair.Value)
                    {
                        bool contains = ContainsDirective(line);
                        if (contains && !string.IsNullOrEmpty(DirectiveName))
                        {
                            assetPair.Value.RemainingDirectives = assetPair.Value.RemainingDirectives.Replace(DirectiveName, "");
                        }
                        if (_value.HasValue && !IsMixedValue)
                        {
                            IsMixedValue |= contains ^ Value;
                        }
                        _value = contains;
                        includeFile |= contains;
                    }
                    if (includeFile)
                    {
                        _presenceAssets.Add(assetPair.Key);
                    }
                }
                _value &= !IsMixedValue;
            }

            protected virtual bool ContainsDirective(string line)
            {
                return line.Contains(string.Concat(" ", DirectiveName));
            }

            public virtual string ApplyFor(TextAsset asset)
            {
                return (_value.HasValue && _value.Value && !IsMixedValue) // Apply to all assets
                    || (!Value && IsMixedValue && _presenceAssets.Contains(asset)) ? DirectiveName : "";
            }
        }

        public class UnityVersionDictionary : DirectiveWrapper
        {
            private Dictionary<TextAsset, UnityVersionGroupDirective> _versions;

            public UnityVersionGroupDirective MainVersionGroup { get; private set; }

            public static Regex VersionRegex { get; private set; }

            public UnityVersionDictionary(string label) : base(null, label)
            {
                _versions = new Dictionary<TextAsset, UnityVersionGroupDirective>();
                MainVersionGroup = new UnityVersionGroupDirective();

                if (VersionRegex == null)
                {
                    VersionRegex = new Regex(@" \!?UNITY_(\d+)(_(\d+)(_(\d+|OR_NEWER))?)?");
                }
            }

            public override string ApplyFor(TextAsset asset)
            {
                if (_value.HasValue && MainVersionGroup != null)
                {
                    return _value.Value ? MainVersionGroup.BuildVersionString() : "";
                }
                if (_versions.ContainsKey(asset))
                {
                    return _versions[asset].BuildVersionString();
                }
                return "";
            }

            protected override bool ContainsDirective(string line)
            {
                return VersionRegex.IsMatch(line);
            }

            public override void ComputeBatchValue(Dictionary<TextAsset, AssetDirective> assetsWithLines)
            {
                if (assetsWithLines != null && assetsWithLines.Count > 0)
                {
                    UnityVersionGroupDirective lastVersionGroup = null;
                    bool lastValid = false;
                    foreach (var pair in assetsWithLines)
                    {
                        string lines = "";
                        foreach (var line in pair.Value)
                        {
                            lines += line;
                        }
                        var matches = VersionRegex.Matches(lines);
                        if (matches.Count == 0)
                        {
                            if (lastVersionGroup != null && lastValid)
                            {
                                IsMixedValue = true;
                            }
                            lastVersionGroup = null;
                            continue;
                        }
                        UnityVersionGroupDirective versionGroup = new UnityVersionGroupDirective();
                        if (matches.Count > 1 || (matches.Count == 1 && matches[0].Groups.Count > 5 && matches[0].Groups[5].Value == "OR_NEWER"))
                        {
                            lastValid = true;
                            versionGroup.IsRange = true;

                            versionGroup.UnityMinVersion.version = matches[0].Groups[1].Value;
                            versionGroup.UnityMinVersion.major = matches[0].Groups.Count > 3 ? matches[0].Groups[3].Value : "";

                            if (matches.Count > 1)
                            {
                                versionGroup.UnityMaxVersion.version = matches[1].Groups[1].Value;
                                versionGroup.UnityMaxVersion.major = matches[1].Groups.Count > 3 ? matches[0].Groups[3].Value : "";
                            }

                            pair.Value.RemainingDirectives.Replace(versionGroup.UnityMinVersion.ToDirective(false), "");
                            pair.Value.RemainingDirectives.Replace(versionGroup.UnityMaxVersion.ToDirective(false), "");
                        }
                        else
                        {
                            lastValid = true;
                            versionGroup.IsRange = false;

                            versionGroup.UnityPreciseVersion.version = matches[0].Groups[1].Value;
                            versionGroup.UnityPreciseVersion.major = matches[0].Groups.Count > 3 ? matches[0].Groups[3].Value : "";
                            versionGroup.UnityPreciseVersion.minor = matches[0].Groups.Count > 5 ? matches[0].Groups[5].Value : "";

                            pair.Value.RemainingDirectives.Replace(versionGroup.UnityPreciseVersion.ToDirective(false), "");
                        }

                        if (!IsMixedValue && lastVersionGroup != null
                                                    && (lastVersionGroup.IsRange != versionGroup.IsRange
                                                    || !lastVersionGroup.UnityPreciseVersion.IsSameVersion(versionGroup.UnityPreciseVersion)
                                                    || !lastVersionGroup.UnityMinVersion.IsSameVersion(versionGroup.UnityMinVersion)
                                                    || !lastVersionGroup.UnityMaxVersion.IsSameVersion(versionGroup.UnityMaxVersion)))
                        {
                            IsMixedValue = true;
                        }
                        if (lastValid)
                        {
                            _versions[pair.Key] = versionGroup;
                        }
                        lastVersionGroup = versionGroup;
                    }
                    if (lastVersionGroup != null && assetsWithLines.Count == 1)
                    {
                        MainVersionGroup = lastVersionGroup;
                        _value = lastValid;
                    }
                    if (!IsMixedValue && lastValid)
                    {
                        MainVersionGroup = lastVersionGroup;
                        _value = lastValid;
                    }
                }

            }
        }

        public class UnityVersionGroupDirective
        {
            public bool IsRange { get; set; }
            public UnityVersionDirective UnityPreciseVersion { get; private set; }
            public UnityVersionDirective UnityMinVersion { get; private set; }
            public UnityVersionDirective UnityMaxVersion { get; private set; }

            public UnityVersionGroupDirective()
            {
                UnityPreciseVersion = new UnityVersionDirective();
                UnityMinVersion = new UnityVersionDirective();
                UnityMaxVersion = new UnityVersionDirective();
            }

            public string BuildVersionString()
            {
                if (UnityPreciseVersion.IsValid && !IsRange)
                {
                    return UnityPreciseVersion.ToDirective(false);
                }
                else if (UnityMinVersion.IsValid && UnityMaxVersion.IsValid)
                {
                    return UnityMinVersion.ToDirective(true) + " && !" + UnityMaxVersion.ToDirective(true);
                }
                else if (UnityMinVersion.IsValid)
                {
                    return UnityMinVersion.ToDirective(true);
                }
                else if (UnityMaxVersion.IsValid)
                {
                    return "!" + UnityMaxVersion.ToDirective(true);
                }
                else
                {
                    return "";
                }
            }
        }

        public class UnityVersionDirective
        {
            public string version;
            public string major;
            public string minor;

            public bool IsValid { get { return !string.IsNullOrEmpty(version); } }

            public bool IsSameVersion(UnityVersionDirective other)
            {
                return version == other.version && major == other.major && minor == other.minor;
            }

            public string ToDirective(bool greaterOrEqual)
            {
                if (string.IsNullOrEmpty(version))
                {
                    return null;
                }
                return greaterOrEqual ? "UNITY_" + version + "_" + (string.IsNullOrEmpty(major) ? "0" : major) + "_OR_NEWER" :
                                        "UNITY_" + version + (!string.IsNullOrEmpty(minor) && !string.IsNullOrEmpty(major) ?
                                                                    "_" + major + "_" + minor :
                                                                    !string.IsNullOrEmpty(major) ? "_" + major :
                                                                    !string.IsNullOrEmpty(minor) ? "_" + minor :
                                                                    "");
            }
        }
    }
}