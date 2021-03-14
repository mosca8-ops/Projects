using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.VR;
using TXT.WEAVR.Common;
using UnityEngine.Events;
using System;
using System.Linq;

namespace TXT.WEAVR.Common
{
    public class Outliner : IWeavrSingleton
    {

        public static Color DefaultColor { get; set; } = Color.green;

        private static List<IObjectOutliner> s_registeredOutliners = new List<IObjectOutliner>();
        private static Dictionary<GameObject, List<Color>> s_activeOutlines = new Dictionary<GameObject, List<Color>>();

        public static Action<GameObject, Color> Outlined;
        public static Action<GameObject, Color> OutlineRemoved;

        public static void Register(IObjectOutliner outliner)
        {
            if (!s_registeredOutliners.Contains(outliner))
            {
                s_registeredOutliners.Add(outliner);
            }
        }

        public static void Unregister(IObjectOutliner outliner)
        {
            s_registeredOutliners.Remove(outliner);
        }

        public static void Outline(GameObject go) => Outline(go, DefaultColor, false);

        public static void Outline(GameObject go, Color color) => Outline(go, color, false);

        public static void Outline(GameObject go, Color color, bool overwriteAllOutlines)
        {
            if (!go) { return; }

            if (overwriteAllOutlines)
            {
                RemoveOutline(go);
            }

            bool registerGO = false;
            for (int i = 0; i < s_registeredOutliners.Count; i++)
            {
                var outliner = s_registeredOutliners[i];
                if (outliner == null)
                {
                    s_registeredOutliners.RemoveAt(i--);
                }
                else if (outliner.Active)
                {
                    outliner.Outline(go, color);
                    registerGO = true;
                }
            }
            if (registerGO)
            {
                if(!s_activeOutlines.TryGetValue(go, out List<Color> colors))
                {
                    colors = new List<Color>();
                    s_activeOutlines[go] = colors;
                }
                if (!colors.Contains(color))
                {
                    colors.Add(color);
                }
            }

            Outlined?.Invoke(go, color);
        }

        public static void RemoveOutline(GameObject go, Color color)
        {
            if (!go) { return; }

            if(s_activeOutlines.TryGetValue(go, out List<Color> colors) && colors.Remove(color))
            {
                OutlineRemoved?.Invoke(go, color);
            }
            for (int i = 0; i < s_registeredOutliners.Count; i++)
            {
                var outliner = s_registeredOutliners[i];
                if (outliner == null)
                {
                    s_registeredOutliners.RemoveAt(i--);
                }
                else if (outliner.Active)
                {
                    outliner.RemoveOutline(go, color);
                }
            }
        }

        public static void RemoveOutline(GameObject go)
        {
            if (!go) { return; }

            if (s_activeOutlines.TryGetValue(go, out List<Color> colors))
            {
                foreach (var color in colors)
                {
                    for (int i = 0; i < s_registeredOutliners.Count; i++)
                    {
                        var outliner = s_registeredOutliners[i];
                        if (outliner == null)
                        {
                            s_registeredOutliners.RemoveAt(i--);
                        }
                        else if (outliner.Active)
                        {
                            outliner.RemoveOutline(go, color);
                        }
                    }
                }
                colors.Clear();
            }
        }

        public static void RemoveOutlineAll()
        {
            foreach (var go in s_activeOutlines.Keys)
            {
                RemoveOutline(go);
            }
        }

        public static bool HasOutline(GameObject go)
        {
            return go && s_activeOutlines.TryGetValue(go, out List<Color> colors) && colors.Count > 0;
        }

        public static bool TryGetOutlineColor(GameObject go, out Color? color)
        {
            if(go && s_activeOutlines.TryGetValue(go, out List<Color> colors) && colors.Count > 0)
            {
                color = colors.Last();
                return true;
            }

            color = null;
            return false;
        }

        public static IEnumerable<IObjectOutliner> GetActiveOutliners() => s_registeredOutliners.Where(a => a.Active);
    }
}