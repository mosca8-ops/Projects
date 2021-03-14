using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Localization;
using UnityEngine;


namespace TXT.WEAVR.Procedure
{
    public interface IProcedureStep
    {
        bool IsMandatory { get; set; }
        string Title { get; set; }
        string StepGUID { get; }
        string Number { get; set; }
        string Description { get; set; }
        void SetDescription(LocalizedString description);
    }

    public interface IProcedureStartValidator
    {
        bool ValidateProcedureStart(Procedure procedure, ExecutionMode mode);
    }

    public interface ISearchTarget
    {
        bool Contains(string value);
    }

    public interface ITransitionOwner
    {
        void RemoveTransition(BaseTransition transition);
    }

    public interface ITargetingObject
    {
        Object Target { get; set; }

        string TargetFieldName { get; }
    }

    public interface IVariablesUser
    {
        IEnumerable<string> GetActiveVariablesFields();
    }

    public interface IRequiresValidation
    {
        void OnValidate();
    }

    public interface IPreviewElement
    {
        bool CanPreview();
    }

    public interface IParameterlessAction
    {
        string GetDescription();
    }

    public interface IPoseProvider : ITargetingObject
    {
        Pose GetOutputPose(Pose input);
    }

    public interface IPreviewAnimation : IPreviewElement
    {
        void ApplyPreview(GameObject previewGameObject);
    }

    public interface IProcedureObjectsContainer
    {
        List<ProcedureObject> Children { get; }
    }

    public interface ICreatedCloneCallback
    {
        void OnCreatedByCloning();
    }

    public interface IProcedureProvider
    {
        bool TryGetProcedure(string procedureGuid, out Procedure procedure);
    }

    public interface INetworkProcedureObject
    {
        bool IsGlobal { get; }
    }

    public interface ISerializedNetworkProcedureObject : INetworkProcedureObject
    {
        string IsGlobalFieldName { get; }
    }

    public delegate void OnNetworkObjectChangedDelegate(IActiveNetworkProcedureObject obj);
    public interface IActiveNetworkProcedureObject : INetworkProcedureObject
    {
        string ID { get; }
        object[] SerializeForNetwork();
        void ConsumeFromNetwork(object[] data);

        event OnNetworkObjectChangedDelegate OnLocalValueChanged;
    }

    public interface INetworkProcedureObjectsContainer
    {
        IEnumerable<INetworkProcedureObject> NetworkObjects { get; }
    }
}
