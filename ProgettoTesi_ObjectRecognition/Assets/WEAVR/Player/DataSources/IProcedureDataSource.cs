using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TXT.WEAVR.Communication.Entities;

using ProcedureEntity = TXT.WEAVR.Communication.Entities.Procedure;
using ProcedureAsset = TXT.WEAVR.Procedure.Procedure;
using UnityEngine;
using TXT.WEAVR.Core;

namespace TXT.WEAVR.Player.DataSources
{
    public interface IProcedureDataSource
    {
        bool IsAvailable { get; }
        Task<IHierarchyProxy> GetProceduresHierarchy(Guid userId, params Guid[] groupsIds);
        Task<IProcedureProxy> GetProcedureById(Guid procedureId);
        Task<ISceneProxy> GetScene(Guid sceneId);
        void CleanUp();
        void Clear();
    }

    public interface ISlowProcedureDataSource : IProcedureDataSource
    {
        Task<int> GetProceduresCount();
        Task<int> GetProceduresCountByUsers(params Guid[] usersIds);
        Task<int> GetProceduresCountByGroups(params Guid[] groupsIds);
    }

    public interface IProxy
    {
        Guid Id { get; }
        IProcedureDataSource Source { get; }
        ProcedureFlags Status { get; }
        Task Sync(Action<float> progressUpdate = null);
        event OnValueChanged<ProcedureFlags> StatusChanged;
        void Refresh();
    }

    public interface IHierarchyProxy
    {
        IProcedureDataSource Source { get; }
        IEnumerable<IProcedureGroupProxy> Groups { get; }
        IEnumerable<IProcedureProxy> UserProcedures { get; }
        void Merge(IHierarchyProxy otherHierarchy);
        IProcedureProxy GetProxy(Guid id);
    }

    public interface IProcedureGroupProxy
    {
        Guid Id { get; }
        string Name { get; }
        IProcedureDataSource Source { get; }
        Task<Group> GetGroup();
        IEnumerable<IProcedureProxy> Procedures { get; }
        void Merge(IProcedureGroupProxy otherGroup);
    }

    public interface IProcedureProxy : IProxy
    {
        IEnumerable<Guid> GetAssignedGroupsIds();
        Task<ProcedureEntity> GetEntity();
        Task<ProcedureAsset> GetAsset();
        Task<ISceneProxy> GetSceneProxy();
        Task<IEnumerable<ISceneProxy>> GetAdditiveScenesProxies();
        void AssignGroup(Guid groupId);
        Task<Texture2D> GetPreviewImage();
        Task<bool> Delete();
    }

    public interface ISceneProxy : IProxy
    {
        Task<Scene> GetSceneEntity();
        Task<string> GetUnityScene();
    }

    public interface IDisposableProxy
    {
        void Dispose();
    }
}
