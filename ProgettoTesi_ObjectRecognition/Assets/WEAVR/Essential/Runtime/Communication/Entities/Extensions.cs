using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProcedureEntity = TXT.WEAVR.Communication.Entities.Procedure;

namespace TXT.WEAVR.Communication.Entities
{
    public static partial class EntitiesExtensions
    {
        public static Func<string> GetPlatformFunctor;

        private static string GetPlatform()
        {
            return GetPlatformFunctor?.Invoke() ?? Application.platform.ToString();
        }

        public static ProcedureVersion GetLastVersion(this ProcedureEntity procedure)
        {
            return procedure.ProcedureVersions.OrderByDescending(v => v.UpdatedAt).FirstOrDefault();
        }

        public static ProcedureVersion GetVersion(this ProcedureEntity procedure, Guid versionId)
        {
            return procedure.ProcedureVersions.FirstOrDefault(v => v.Id == versionId);
        }

        public static ProcedureVersionPlatform GetForCurrentPlatform(this ProcedureVersion version)
        {
            var platform = GetPlatform();
            return version.ProcedureVersionPlatforms.FirstOrDefault(p => p.PlatformPlayer == platform);
        }
        
        public static ProcedureVersion GetLastVersionForCurrentPlatform(this ProcedureEntity procedure)
        {
            var platform = GetPlatform();
            return procedure.ProcedureVersions
                            .Where(v => v.ProcedureVersionPlatforms.Any(p => p.PlatformPlayer == platform))
                            .OrderByDescending(v => v.UpdatedAt)
                            .FirstOrDefault();
        }

        public static ProcedureVersionPlatform GetLastVersionPlatform(this ProcedureEntity procedure)
        {
            var platform = GetPlatform();
            return procedure.ProcedureVersions
                            .OrderByDescending(v => v.UpdatedAt)
                            .Where(v => v.ProcedureVersionPlatforms.Any(p => p.PlatformPlayer == platform))
                            .SelectMany(v => v.ProcedureVersionPlatforms)
                            .FirstOrDefault();
        }

        public static SceneVersion GetLastVersion(this Scene scene)
        {
            return scene.SceneVersions.OrderByDescending(v => v.UpdatedAt).FirstOrDefault();
        }

        public static SceneVersion GetVersion(this Scene scene, Guid versionId)
        {
            return scene.SceneVersions.FirstOrDefault(v => v.Id == versionId);
        }

        public static SceneVersionPlatform GetForCurrentPlatform(this SceneVersion version)
        {
            var platform = GetPlatform();
            return version.SceneVersionPlatforms.FirstOrDefault(p => p.PlatformPlayer == platform);
        }

        public static SceneVersion GetLastVersionForCurrentPlatform(this Scene scene)
        {
            var platform = GetPlatform();
            return scene.SceneVersions
                            .Where(v => v.SceneVersionPlatforms.Any(p => p.PlatformPlayer == platform))
                            .OrderByDescending(v => v.UpdatedAt)
                            .FirstOrDefault();
        }

        public static SceneVersionPlatform GetLastVersionPlatform(this Scene scene)
        {
            var platform = GetPlatform();
            return scene.SceneVersions
                            .OrderByDescending(v => v.UpdatedAt)
                            .Where(v => v.SceneVersionPlatforms.Any(s => s.PlatformPlayer == platform))
                            .SelectMany(v => v.SceneVersionPlatforms)
                            .FirstOrDefault();
        }

        public static IEnumerable<ProcedureEntity> GetAllProcedures(this ProcedureHierarchy hierarchy)
        {
            return hierarchy.Procedures.Concat(hierarchy.Groups.SelectMany(g => g.Procedures));
        }

        public static IEnumerable<Scene> GetAllScenes(this ProcedureHierarchy hierarchy)
        {
            return hierarchy.Procedures.Concat(hierarchy.Groups.SelectMany(g => g.Procedures)).Select(p => p.Scene);
        }

        public static IEnumerable<(ProcedureEntity procedure, IEnumerable<ProcedureGroup> groups)> GetAllProceduresWithGroups(this ProcedureHierarchy hierarchy)
        {
            Dictionary<ProcedureEntity, IEnumerable<ProcedureGroup>> procedureGroups = new Dictionary<ProcedureEntity, IEnumerable<ProcedureGroup>>();
            foreach(var procedure in hierarchy.Procedures)
            {
                procedureGroups[procedure] = new HashSet<ProcedureGroup>() { null };
            }

            foreach(var group in hierarchy.Groups)
            {
                foreach(var procedure in group.Procedures)
                {
                    if(!procedureGroups.TryGetValue(procedure, out IEnumerable<ProcedureGroup> groups))
                    {
                        groups = new HashSet<ProcedureGroup>();
                        procedureGroups[procedure] = groups;
                    }
                    (groups as HashSet<ProcedureGroup>).Add(group);
                }
            }

            return procedureGroups.Select(p => (p.Key, p.Value));
        }
    }
}
