using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Editor;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    class BaseActionController : GraphObjectController<BaseAction>
    {
        public class Change
        {
            public const int StateChanged = 10;
        }

        public override bool HasPosition => false;

        public override bool IsSuperCollapsable => false;

        public override bool IsCollapsable => true;

        public ExecutionState CurrentState => Model.CurrentState;

        public string Description {
            get
            {
                try
                {
                    return Model.GetDescription();
                }
                catch(System.Exception e)
                {
                    string message = string.IsNullOrEmpty(e.Message) ? e.GetType().Name : e.Message;
                    return $"Error: {message}";
                }
            }
        }

        public bool HasErrors => !string.IsNullOrEmpty(Model.ErrorMessage);
        public string ErrorMessage => Model.ErrorMessage;
        public System.Exception Exception => Model.Exception;

        public bool HasWarning => !string.IsNullOrEmpty(WarningMessage);
        public string WarningMessage { get; private set; }
        public Color Color { get; private set; }

        public Texture2D Icon { get; private set; }
        public bool IsProgressElement => Model is IProgressElement;
        public float Progress => Mathf.Clamp01((Model as IProgressElement).Progress);


        public BaseActionController(BaseAction action, GraphObjectController owner): base(owner.ViewController, action)
        {
            action.OnModified -= ModelModified;
            action.OnModified += ModelModified;

            action.StateChanged -= Action_StateChanged;
            action.StateChanged += Action_StateChanged;
            //UpdateVisualItems(action);
        }

        private void Action_StateChanged(IFlowElement element, ExecutionState newState)
        {
            NotifyChange(Change.StateChanged);
        }

        private void UpdateVisualItems(BaseAction action)
        {
            var descriptor = ProcedureDefaults.Current.ActionsCatalogue.GetDescriptor(action);
            Color = descriptor?.Color ?? Color.clear;
            Icon = descriptor?.Icon;
        }

        public override void ResetState()
        {
            base.ResetState();
            Model.MuteEvents = true;
            Model.CurrentState = ExecutionState.NotStarted;
            Model.ErrorMessage = null;
            Model.Exception = null;
            (Model as IProgressElement)?.ResetProgress();
            Model.MuteEvents = false;

            NotifyChange(AnyThing);
        }

        protected override void ModelChanged(BaseAction action)
        {
            if(action && action.Exception != null && WeavrEditor.Settings.GetValue("LogErrors", false))
            {
                WeavrDebug.LogException(action, action.Exception);
                action.Exception = null;
            }
            RefreshInfo(action);
        }

        public void RefreshInfo(BaseAction action = null)
        {
            RefreshMessages(action ? action : Model);
            UpdateVisualItems(action ? action : Model);
        }

        protected virtual void RefreshMessages(BaseAction action)
        {
            WarningMessage = null;
            if (!HasErrors)
            {
                if (action is ITargetingObject targetting)
                {
                    if (!targetting.Target)
                    {
                        if (action is IVariablesUser varUser && !varUser.GetActiveVariablesFields().Any(v => targetting.TargetFieldName.StartsWith(v)))
                        {
                            WarningMessage = $"No object set for '{EditorTools.NicifyName(targetting.TargetFieldName)}'";
                        }
                    }
                    else if (targetting.Target.GetGameObject().scene.path != ViewController.Model.ScenePath)
                    {
                        WarningMessage = $"The object set for '{EditorTools.NicifyName(targetting.TargetFieldName)}' is not part of the procedure scene {ViewController.Model.SceneName}";
                    }
                }
            }
        }

        protected void ModelModified(ProcedureObject model)
        {
            if(model == Model)
            {
                ApplyChanges();
                NotifyChange(ModelHasChanges);
            }
        }
    }
}
