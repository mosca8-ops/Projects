using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR;
using TXT.WEAVR.Core;
using TXT.WEAVR.Procedure;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

using UnityObject = UnityEngine.Object;

namespace TXT.WEAVR
{

    public class MultiSelectionReorderableList<T> : ReorderableList, IEditorWindowClient where T : ScriptableObject
    {
        public delegate bool IsSelectableDelegate(T elem);
        public delegate void OnPasteElementsDelegate(IEnumerable<object> pastedElements);
        public delegate void OnDeleteSelection(List<T> selection);
        public delegate int GetIndex();

        private const float k_offset = 32f;

        private Dictionary<T, int> m_selection;
        private Dictionary<T, VisibilityData> m_visibles;
        private UnityObject m_target;
        private IList<T> m_list;
        private Vector2 m_prevPosition;
        private Event m_lastEvent;
        private bool m_canComputeHeight;

        public IReadOnlyDictionary<T, int> Selection => m_selection;

        private bool m_hoverEnabled;
        private Color? m_selectColor;
        private Color m_hoverColor;
        private bool m_drawNotVisible;

        public Color? selectionColor {
            get => m_selectColor;
            set
            {
                if(m_selectColor != value)
                {
                    m_selectColor = value;
                    if (m_selectColor.HasValue)
                    {
                        m_hoverColor = m_selectColor.Value;
                        m_hoverColor.a *= 0.5f;
                    }
                }
            }
        }

        public bool enableHover {
            get => m_hoverEnabled;
            set => m_hoverEnabled = value;
        }

        public bool drawNotVisibleElements
        {
            get => m_drawNotVisible;
            set => m_drawNotVisible = value;
        }
        
        public EditorWindow Window {
            get;
            set;
        }

        protected bool useScreenCoords;
        protected float minHeight;
        protected float maxHeight;

        protected float startPosition;

        public MultiSelectionReorderableList(UnityObject target, IList<T> elements, bool draggable, bool displayHeader, bool displayAddButton, bool displayRemoveButton) : base(elements as IList, typeof(T), draggable, displayHeader, displayAddButton, displayRemoveButton)
        {
            m_selection = new Dictionary<T, int>();
            m_visibles = new Dictionary<T, VisibilityData>();
            m_target = target;
            m_list = elements;

            base.onSelectCallback = OnSelected;
            base.onReorderCallbackWithDetails = ReorderedWithDetails;
            //base.onRemoveCallback = Reordered;
#if UNITY_2018_4_OR_NEWER
            base.onMouseDragCallback = l => { };
#endif
            base.onMouseUpCallback = MouseUp;
            base.drawElementBackgroundCallback = DrawElementBackground;
            base.drawElementCallback = DrawElement;
            base.elementHeightCallback = GetElementHeight;

            isSelectable = e => true;
        }
        
        public IsSelectableDelegate isSelectable;
        public OnPasteElementsDelegate onElementsPaste;
        public OnDeleteSelection onDeleteSelection;
        public GetIndex onGetPasteIndexCallback;
        public new SelectCallbackDelegate onSelectCallback;
        public new ReorderCallbackDelegateWithDetails onReorderCallbackWithDetails;
        public new SelectCallbackDelegate onMouseUpCallback;
        public new ReorderCallbackDelegate onReorderCallback;
        public new ElementCallbackDelegate drawElementCallback;
        public new ElementCallbackDelegate drawElementBackgroundCallback;
        public new ElementHeightCallbackDelegate elementHeightCallback;

        public void DoLayoutList(Rect viewport, bool isOutsideLayoutGroup)
        {
            if (!m_drawNotVisible)
            {
                var e = Event.current;
                if (!m_canComputeHeight && e.type == EventType.Layout && e.type != m_lastEvent?.type)
                {
                    m_canComputeHeight = true;
                }
                else// if(Event.current.type == EventType.Layout)
                {
                    m_canComputeHeight = e.type == EventType.Repaint;
                }
                m_lastEvent = e;

                useScreenCoords = false;
                if (e.type == EventType.Repaint)
                {
                    if (isOutsideLayoutGroup)
                    {
                        var lastRect = GUILayoutUtility.GetLastRect();
                        minHeight = viewport.y - lastRect.yMax - headerHeight - k_offset;
                        maxHeight = viewport.yMax - lastRect.yMax - headerHeight;
                    }
                    else
                    {
                        minHeight = viewport.y - headerHeight - k_offset;
                        maxHeight = viewport.yMax - headerHeight;
                    }
                }
                else if (minHeight == 0 && Window)
                {
                    useScreenCoords = true;
                    minHeight = GUIUtility.GUIToScreenPoint(Window.position.min).y - startPosition - k_offset;
                    maxHeight = GUIUtility.GUIToScreenPoint(Window.position.max).y - startPosition;
                }


                startPosition = m_list.Count > 0 ? float.MaxValue : 0;
            }

            base.DoLayoutList();

            if (HasKeyboardControl())
            {
                HandleCommands();
            }
            else if (m_selection.Count > 0)
            {
                m_selection.Clear();
            }
        }

        public new void DoLayoutList()
        {
            if (!m_drawNotVisible)
            {
                var e = Event.current;
                if (!m_canComputeHeight && e.type == EventType.Layout && e.type != m_lastEvent?.type)
                {
                    m_canComputeHeight = true;
                }
                else// if(Event.current.type == EventType.Layout)
                {
                    m_canComputeHeight = e.type == EventType.Repaint;
                }
                m_lastEvent = e;

                useScreenCoords = true;
                if (Window)
                {
                    minHeight = GUIUtility.GUIToScreenPoint(Window.position.min).y - startPosition - k_offset;
                    maxHeight = GUIUtility.GUIToScreenPoint(Window.position.max).y - startPosition;

                    startPosition = m_list.Count > 0 ? float.MaxValue : 0;
                }
                else
                {
                    minHeight = 0;
                    maxHeight = Screen.height;
                }
            }

            base.DoLayoutList();
            if (HasKeyboardControl())
            {
                HandleCommands();
            }
            else if (m_selection.Count > 0)
            {
                m_selection.Clear();
            }
        }

        public new void DoList(Rect rect)
        {
            base.DoList(rect);
            if (HasKeyboardControl())
            {
                HandleCommands();
            }
            else if (m_selection.Count > 0)
            {
                m_selection.Clear();
            }
        }

        private void HandleCommands()
        {
            var e = Event.current;
            if (e.type != EventType.KeyUp)
            {
                return;
            }
            switch (WeavrEditor.Commands[e.modifiers, e.keyCode])
            {
                case Command.Copy:
                    CopySelection();
                    e.Use();
                    break;
                case Command.Cut:
                    CopySelection();
                    DeleteSelection();
                    e.Use();
                    break;
                case Command.Duplicate:
                    CopySelection();
                    PasteSelection(GUIUtility.systemCopyBuffer);
                    e.Use();
                    break;
                case Command.Paste:
                    PasteSelection(GUIUtility.systemCopyBuffer);
                    e.Use();
                    break;
                case Command.Delete:
                    DeleteSelection();
                    e.Use();
                    break;
            }
        }

        public void PasteSelection(string serializedData)
        {
            if (string.IsNullOrEmpty(serializedData))
            {
                return;
            }

            SerializationNodesList deserializedData = null;
            try
            {
                deserializedData = SerializationNodesList.Deserialize(serializedData);
            }
            catch
            {
                return;
            }
            if (deserializedData == null)
            {
                return;
            }

            index = m_selection.Count > 0 ? m_selection.Values.OrderBy(i => i).Last() : this.index;
            if (index < 0 && onGetPasteIndexCallback != null)
            {
                index = onGetPasteIndexCallback();
            }
            else
            {
                index = index >= m_list.Count - 1 ? m_list.Count - 1 : index + 1;
            }
            if (index < 0) { index = 0; }

            var elements = deserializedData.DeserializeAll();

            foreach(var elem in elements)
            {
                if(elem is T)
                {
                    Undo.RegisterCreatedObjectUndo(elem, "Paste Element");
                }
            }

            Undo.RegisterCompleteObjectUndo(m_target, "Added Elements");
            foreach (var elem in elements)
            {
                if (elem is T)
                {
                    m_list.Insert(index++, elem as T);
                }
            }

            m_selection.Clear();
            foreach (var elem in elements)
            {
                if (elem is T)
                {
                    m_selection[elem as T] = m_list.IndexOf(elem as T);
                }
            }

            onElementsPaste?.Invoke(elements);
            onChangedCallback?.Invoke(this);
        }

        private void CopySelection()
        {
            SerializationNodesList nodes = new SerializationNodesList();
            foreach (var elem in m_selection.OrderBy(p => p.Value).Select(p => p.Key))
            {
                nodes.Append(elem);
            }
            GUIUtility.systemCopyBuffer = nodes.Seal().Serialize();
        }

        private void DeleteSelection()
        {
            Undo.RegisterCompleteObjectUndo(m_target, "Removed Elements");
            if (onDeleteSelection != null)
            {
                onDeleteSelection(m_selection.Keys.ToList());
            }
            else
            {
                foreach (var key in m_selection.Keys)
                {
                    if (key is T)
                    {
                        m_list.Remove(key as T);
                    }
                }
            }
            m_selection.Clear();

            onChangedCallback?.Invoke(this);
        }

        private float GetElementHeight(int index)
        {
            if (m_drawNotVisible)
            {
                return elementHeightCallback?.Invoke(index) ?? elementHeight;
            }

            float min, max;
            T key = m_list[index];
            if (!m_visibles.TryGetValue(key, out VisibilityData data) || data.IsVisibleOnScreen(useScreenCoords, minHeight, maxHeight, out min, out max))
            {
                if (m_canComputeHeight)
                {
                    data.height = elementHeightCallback?.Invoke(index) ?? elementHeight;
                }
                data.isVisible = data.IsVisibleOnScreen(useScreenCoords, minHeight, maxHeight, out min, out max);
            }
            else
            {
                data.isVisible = false;
            }
            //if (data.isVisible)
            //{
            //    Debug.Log($"[Index {index}]: {data.Debug()}");
            //}

            if(min < startPosition)
            {
                startPosition = min;
            }

            if(index > 0)
            {
                data.minPosition = m_prevPosition;
            }
            else
            {
                data.minPosition.y = headerHeight;
            }

            data.maxPosition.y = m_prevPosition.y = data.minPosition.y + data.height;

            m_visibles[key] = data;
            return data.height;
        }

        private void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (isActive || m_drawNotVisible || index < 0 || !m_visibles.TryGetValue(m_list[index], out VisibilityData data) || data.isVisible)
            {
                drawElementCallback(rect, index, isActive, index >= 0 && m_selection.ContainsKey(m_list[index]));
            }
        }

        private void DrawElementBackground(Rect rect, int index, bool isActive, bool isFocused)
        {
            if(Event.current.type != EventType.Repaint) { return; }
            if(index < 0 || index > m_list.Count)
            {
                drawElementBackgroundCallback?.Invoke(rect, index, isActive, isFocused);
            }
            else if (m_drawNotVisible || !m_visibles.TryGetValue(m_list[index], out VisibilityData data) || data.isVisible)
            {
                isFocused = index >= 0 && m_selection.ContainsKey(m_list[index]);
                if (selectionColor.HasValue)
                {
                    if (isFocused)
                    {
                        EditorGUI.DrawRect(rect, selectionColor.Value);
                    }
                    else if (m_hoverEnabled && rect.Contains(Event.current.mousePosition))
                    {
                        EditorGUI.DrawRect(rect, m_hoverColor);
                    }
                }
                drawElementBackgroundCallback?.Invoke(rect, index, isActive, isFocused);
            }
        }

        private void Reordered(ReorderableList list)
        {
            onReorderCallback?.Invoke(this);
        }


        private void ReorderedWithDetails(ReorderableList list, int oldIndex, int newIndex)
        {
            // Heavy reordering...
            var keyElement = m_list[newIndex];

            // Bring back the previous situation to save for undo
            var oldElement = m_list[oldIndex];
            m_list[oldIndex] = keyElement;
            m_list[newIndex] = oldElement;

            // Save the state
            Undo.RegisterCompleteObjectUndo(m_target, "Reordered Actions");

            // Redo the reordering
            m_list[newIndex] = keyElement;
            m_list[oldIndex] = oldElement;

            if (m_selection.Count > 1 && m_selection.Values.Contains(oldIndex))
            {

                // Remove the elements in selection
                foreach (var elem in m_selection.Keys)
                {
                    if (elem != keyElement && elem is T)
                    {
                        m_list.Remove(elem as T);
                    }
                }

                int index = m_list.IndexOf(keyElement);
                m_list.RemoveAt(index);

                index = Mathf.Clamp(0, index, m_list.Count - 1);

                // Reinsert new elements in order
                foreach (var key in m_selection.OrderBy(p => p.Value).Select(p => p.Key))
                {
                    if (key is T)
                    {
                        m_list.Insert(index++, key as T);
                    }
                }

                // Update Selection
                foreach (var key in m_selection.Keys.ToArray())
                {
                    if (key is T)
                    {
                        m_selection[key] = m_list.IndexOf(key as T);
                    }
                }

                //onChangedCallback?.Invoke(this);
            }

            onReorderCallbackWithDetails?.Invoke(this, oldIndex, newIndex);
        }

        private void MouseUp(ReorderableList list)
        {
            if (list.index < 0 || list.index >= m_list.Count)
            {
                m_selection.Clear();

                onMouseUpCallback?.Invoke(this);
                return;
            }

            var element = m_list[list.index];
            if (!(element is T))
            {
                m_selection.Clear();

                onMouseUpCallback?.Invoke(this);
                return;
            }
            var e = Event.current;
            if (e.shift)
            {
                int minIndex = list.index;
                int maxIndex = list.index;
                for (int i = 0; i < m_list.Count; i++)
                {
                    if (m_selection.ContainsKey(m_list[i]))
                    {
                        if (i < minIndex)
                        {
                            minIndex = i;
                        }
                        if (i > maxIndex)
                        {
                            maxIndex = i;
                        }
                    }
                }

                for (int i = minIndex; i <= maxIndex; i++)
                {
                    if (isSelectable?.Invoke(m_list[i]) ?? true)
                    {
                        m_selection[m_list[i]] = i;
                    }
                }
            }
            else if (e.control || e.command)
            {
                if (m_selection.ContainsKey(element))
                {
                    m_selection.Remove(element);
                    if (m_selection.Count == 0)
                    {
                        list.ReleaseKeyboardFocus();
                    }
                    else
                    {
                        list.index = m_selection.First().Value;
                    }
                }
                else
                {
                    m_selection.Add(element, list.index);
                }
            }
            else
            {
                m_selection.Clear();
                m_selection.Add(element, list.index);
            }

            onMouseUpCallback?.Invoke(this);
        }

        private void OnSelected(ReorderableList list)
        {
            onSelectCallback?.Invoke(this);
        }

        private struct VisibilityData
        {
            public bool isVisible;
            public Vector2 minPosition;
            public Vector2 maxPosition;
            public float height;

            public bool IsVisibleOnScreen(bool useScreenCoords, float minHeight, float maxHeight, out float min, out float max)
            {
                if (useScreenCoords)
                {
                    min = GUIUtility.GUIToScreenPoint(minPosition).y;
                    max = GUIUtility.GUIToScreenPoint(maxPosition).y;
                }
                else
                {
                    min = minPosition.y;
                    max = maxPosition.y;
                }
                return !(min > maxHeight || max < minHeight);
            }

            public string Debug()
            {
                return $"Visible = {isVisible} | [{EditorGUIUtility.GUIToScreenPoint(minPosition).y} > {Screen.height} OR {EditorGUIUtility.GUIToScreenPoint(maxPosition).y} < 0]";
            }
        }
    }
}
