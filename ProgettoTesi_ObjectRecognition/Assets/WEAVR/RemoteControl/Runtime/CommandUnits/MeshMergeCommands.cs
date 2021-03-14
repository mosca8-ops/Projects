using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;

namespace TXT.WEAVR.RemoteControl
{

    [AddComponentMenu("WEAVR/Remote Control/Commands/Mesh Merge")]
    public class MeshMergeCommands : BaseCommandUnit
    {
        [RemotelyCalled]
        public void MergeModels(string[] paths)
        {
            foreach (var path in paths)
            {
                var go = Query.Find<GameObject>(QuerySearchType.Scene, path).First();
                if (go)
                {
                    var combiner = go.GetComponent<MeshCombiner>();
                    if (!combiner)
                    {
                        combiner = go.AddComponent<MeshCombiner>();
                    }
                    combiner.MergeMeshes();
                }
            }
        }

        [RemotelyCalled]
        public void UnmergeModels(string[] paths)
        {
            foreach (var path in paths)
            {
                var go = Query.Find<GameObject>(QuerySearchType.Scene, path).First();
                if (go)
                {
                    var combiner = go.GetComponent<MeshCombiner>();
                    if (combiner)
                    {
                        combiner.UnmergeMeshes();
                    }
                }
            }
        }

        [RemotelyCalled]
        public void MergeModels(Guid[] guids)
        {
            foreach (var guid in guids)
            {
                var go = Query.GetGameObjectByGuid(guid);
                if (go)
                {
                    var combiner = go.GetComponent<MeshCombiner>();
                    if (!combiner)
                    {
                        combiner = go.AddComponent<MeshCombiner>();
                    }
                    combiner.MergeMeshes();
                }
            }
        }

        [RemotelyCalled]
        public void UnmergeModels(Guid[] guids)
        {
            foreach (var guid in guids)
            {
                var go = Query.GetGameObjectByGuid(guid);
                if (go)
                {
                    var combiner = go.GetComponent<MeshCombiner>();
                    if (combiner)
                    {
                        combiner.UnmergeMeshes();
                    }
                }
            }
        }

        [RemotelyCalled]
        public void MergeChildren(string path)
        {
            var go = Query.Find<GameObject>(QuerySearchType.Scene, path).First();
            if (go)
            {
                var combiner = go.GetComponent<MeshCombiner>();
                if (!combiner)
                {
                    combiner = go.AddComponent<MeshCombiner>();
                }
                combiner.MergeMeshes();
            }
        }

        [RemotelyCalled]
        public void UnmergeChildren(string path)
        {
            var go = Query.Find<GameObject>(QuerySearchType.Scene, path).First();
            if (go)
            {
                var combiner = go.GetComponent<MeshCombiner>();
                if (!combiner)
                {
                    combiner = go.AddComponent<MeshCombiner>();
                }
                combiner.UnmergeMeshes();
            }
        }

        [RemotelyCalled]
        public void MergeChildren(Guid guid)
        {
            var go = Query.GetGameObjectByGuid(guid);
            if (go)
            {
                var combiner = go.GetComponent<MeshCombiner>();
                if (!combiner)
                {
                    combiner = go.AddComponent<MeshCombiner>();
                }
                combiner.MergeMeshes();
            }
        }

        [RemotelyCalled]
        public void UnmergeChildren(Guid guid)
        {
            var go = Query.GetGameObjectByGuid(guid);
            if (go)
            {
                var combiner = go.GetComponent<MeshCombiner>();
                if (!combiner)
                {
                    combiner = go.AddComponent<MeshCombiner>();
                }
                combiner.UnmergeMeshes();
            }
        }
    }
}
