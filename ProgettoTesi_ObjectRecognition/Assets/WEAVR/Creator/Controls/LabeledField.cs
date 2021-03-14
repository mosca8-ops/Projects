using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;


namespace TXT.WEAVR
{

    public class FieldMouseDragger<T>
    {
        readonly Action m_OnDragFinished;
        public FieldMouseDragger(IValueField<T> drivenField, Action onDragFinished = null)
        {
            m_DrivenField = drivenField;
            m_DragElement = null;
            m_DragHotZone = new Rect(0, 0, -1, -1);
            m_OnDragFinished = onDragFinished;
            dragging = false;
        }

        IValueField<T> m_DrivenField;
        VisualElement m_DragElement;
        Rect m_DragHotZone;

        public bool dragging;
        public T startValue;

        public void SetDragZone(VisualElement dragElement)
        {
            SetDragZone(dragElement, new Rect(0, 0, -1, -1));
        }

        public void SetDragZone(VisualElement dragElement, Rect hotZone)
        {
            if (m_DragElement != null)
            {
                m_DragElement.UnregisterCallback<MouseDownEvent>(UpdateValueOnMouseDown);
                m_DragElement.UnregisterCallback<MouseMoveEvent>(UpdateValueOnMouseMove);
                m_DragElement.UnregisterCallback<MouseUpEvent>(UpdateValueOnMouseUp);
                m_DragElement.UnregisterCallback<KeyDownEvent>(UpdateValueOnKeyDown);
            }

            m_DragElement = dragElement;
            m_DragHotZone = hotZone;

            if (m_DragElement != null)
            {
                dragging = false;
                m_DragElement.RegisterCallback<MouseDownEvent>(UpdateValueOnMouseDown);
                m_DragElement.RegisterCallback<MouseMoveEvent>(UpdateValueOnMouseMove);
                m_DragElement.RegisterCallback<MouseUpEvent>(UpdateValueOnMouseUp);
                m_DragElement.RegisterCallback<KeyDownEvent>(UpdateValueOnKeyDown);
            }
        }

        void UpdateValueOnMouseDown(MouseDownEvent evt)
        {
            if (evt.button == 0 && (m_DragHotZone.width < 0 || m_DragHotZone.height < 0 || m_DragHotZone.Contains(m_DragElement.WorldToLocal(evt.mousePosition))))
            {
                m_DragElement.CaptureMouse();

                // Make sure no other elements can capture the mouse!
                evt.StopPropagation();

                dragging = true;
                startValue = m_DrivenField.value;

                EditorGUIUtility.SetWantsMouseJumping(1);
            }
        }

        void UpdateValueOnMouseMove(MouseMoveEvent evt)
        {
            if (dragging)
            {
                DeltaSpeed s = evt.shiftKey ? DeltaSpeed.Fast : (evt.altKey ? DeltaSpeed.Slow : DeltaSpeed.Normal);
                m_DrivenField.ApplyInputDeviceDelta(evt.mouseDelta, s, startValue);
            }
        }

        void UpdateValueOnMouseUp(MouseUpEvent evt)
        {
            if (dragging)
            {
                dragging = false;
                MouseCaptureController.ReleaseMouse();
                EditorGUIUtility.SetWantsMouseJumping(0);
                m_OnDragFinished?.Invoke();
            }
        }

        void UpdateValueOnKeyDown(KeyDownEvent evt)
        {
            if (dragging && evt.keyCode == KeyCode.Escape)
            {
                dragging = false;
                m_DrivenField.value = startValue;
                MouseCaptureController.ReleaseMouse();
                EditorGUIUtility.SetWantsMouseJumping(0);
            }
        }
    }
    public class LabeledField<T, U> : BaseField<U> where T : VisualElement, INotifyValueChanged<U>, new()
    {
        protected Label m_Label;
        protected T m_Control;

        public VisualElement m_IndeterminateLabel;

        public LabeledField(Label existingLabel) : base(existingLabel.text, null)
        {
            m_Label = existingLabel;

            //CreateControl();
            SetupLabel();
        }

        bool m_Indeterminate;

        public bool indeterminate
        {
            get { return m_Control.parent == null; }

            set
            {
                if (m_Indeterminate != value)
                {
                    m_Indeterminate = value;
                    if (value)
                    {
                        m_Control.RemoveFromHierarchy();
                        Add(m_IndeterminateLabel);
                    }
                    else
                    {
                        m_IndeterminateLabel.RemoveFromHierarchy();
                        Add(m_Control);
                    }
                }
            }
        }

        public LabeledField(string label) : base(label, null)
        {
            if (!string.IsNullOrEmpty(label))
            {
                m_Label = new Label() { text = label };
                m_Label.AddToClassList("label");

                Add(m_Label);
            }
            style.flexDirection = FlexDirection.Row;

            CreateControl();
            SetupLabel();
        }

        void SetupLabel()
        {
            if (typeof(IValueField<U>).IsAssignableFrom(typeof(T)))
            {
                //if (typeof(U) == typeof(float))
                //{
                //    var dragger = new FieldMouseDragger<float>((IValueField<float>)m_Control, DragValueFinished);
                //    dragger.SetDragZone(m_Label);
                //    m_Label.style.cursor = UnityEditor.EditorUtility.CreateDefaultCursorStyle(MouseCursor.SlideArrow);
                //}
                //else if (typeof(U) == typeof(double))
                //{
                //    var dragger = new FieldMouseDragger<double>((IValueField<double>)m_Control, DragValueFinished);
                //    dragger.SetDragZone(m_Label);
                //    m_Label.style.cursor = UnityEditor.EditorUtility.CreateDefaultCursorStyle(MouseCursor.SlideArrow);
                //}
                //else if (typeof(U) == typeof(long))
                //{
                //    var dragger = new FieldMouseDragger<long>((IValueField<long>)m_Control, DragValueFinished);
                //    dragger.SetDragZone(m_Label);
                //    m_Label.style.cursor = UnityEditor.EditorUtility.CreateDefaultCursorStyle(MouseCursor.SlideArrow);
                //}
                //else if (typeof(U) == typeof(int))
                //{
                //    var dragger = new FieldMouseDragger<int>((IValueField<int>)m_Control, DragValueFinished);
                //    dragger.SetDragZone(m_Label);
                //    m_Label.style.cursor = UnityEditor.EditorUtility.CreateDefaultCursorStyle(MouseCursor.SlideArrow);
                //}
            }

            m_IndeterminateLabel = new Label()
            {
                name = "indeterminate",
                text = FieldConstants.indeterminateText
            };
            m_IndeterminateLabel.SetEnabled(false);
        }

        void DragValueFinished()
        {
            onValueDragFinished?.Invoke(this);
        }

        public Action<LabeledField<T, U>> onValueDragFinished;

        private T CreateControl()
        {
            m_Control = new T();
            Add(m_Control);

            m_Control.RegisterCallback<ChangeEvent<U>>(OnControlChange);
            return m_Control;
        }

        void OnControlChange(ChangeEvent<U> e)
        {
            e.StopPropagation();
            using (ChangeEvent<U> evt = ChangeEvent<U>.GetPooled(e.previousValue, e.newValue))
            {
                evt.target = this;
                SendEvent(evt);
            }
        }

        public T control
        {
            get { return m_Control; }
        }

        public Label label
        {
            get { return m_Label; }
        }


        public new void RegisterValueChangedCallback(EventCallback<ChangeEvent<U>> callback)
        {
            (m_Control as INotifyValueChanged<U>).RegisterValueChangedCallback(callback);
        }

        public new void RemoveRegisterValueChangedCallback(EventCallback<ChangeEvent<U>> callback)
        {
            (m_Control as INotifyValueChanged<U>).UnregisterValueChangedCallback(callback);
        }

        public override void SetValueWithoutNotify(U newValue)
        {
            (m_Control as INotifyValueChanged<U>).SetValueWithoutNotify(newValue);
        }

        public override U value
        {
            get { return m_Control.value; }
            set { m_Control.value = value; }
        }
    }
}
