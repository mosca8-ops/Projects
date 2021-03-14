namespace TXT.WEAVR.Cockpit
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using TXT.WEAVR.Editor;
    using UnityEngine;
    using UnityEngine.Assertions;

    [UnityEditor.InitializeOnLoad]
    public static class StateEditor
    {
        private static Dictionary<Type, Type> _drawers;

        static StateEditor() {
            ReloadPropertyDrawerTypes();
        }

        static void ReloadPropertyDrawerTypes() {
            _drawers = new Dictionary<Type, Type>();

            // Look for all the valid attribute decorators
            var types = EditorTools.GetAllAssemblyTypes()
                            .Where(
                                t => t.IsSubclassOf(typeof(BaseStateDrawer))
                                  && t.IsDefined(typeof(StateDrawerAttribute), false)
                                  && !t.IsAbstract
                            );

            // Store them
            foreach (var type in types) {
                var attr = type.GetAttribute<StateDrawerAttribute>();
                var decorator = (BaseStateDrawer)Activator.CreateInstance(type);
                _drawers.Add(attr.StateType, type);
            }
        }

        public static BaseStateDrawer GetDrawer(Type stateType) {
            Type drawerType = null;
            if(_drawers.TryGetValue(stateType, out drawerType)) {
                return (BaseStateDrawer)Activator.CreateInstance(drawerType);
            }
            return null;
        }

        private static BaseStateDrawer GetDrawer(BaseState state, Type stateType) {
            Type drawerType = null;
            if (_drawers.TryGetValue(stateType, out drawerType)) {
                var drawer = (BaseStateDrawer)Activator.CreateInstance(drawerType);
                if (drawer != null) {
                    drawer.SetTargets(state);
                    return drawer;
                }
            }
            return null;
        }

        public static BaseStateDrawer GetDrawer(BaseState state) {
            Assert.IsNotNull(state, "StateEditor: State parameter must be not null");
            Type drawerType = null;
            Type stateType = state.GetType();
            while (stateType != null && !_drawers.TryGetValue(stateType, out drawerType)) {
                stateType = stateType.BaseType;
            }
            if (drawerType != null) {
                var drawer = (BaseStateDrawer)Activator.CreateInstance(drawerType);
                drawer.SetTargets(state);
                return drawer;
            }
            return null;
        }
    }
}