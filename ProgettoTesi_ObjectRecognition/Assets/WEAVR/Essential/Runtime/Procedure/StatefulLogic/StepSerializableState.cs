using System;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{
    [Serializable]
    public class StepSerializableState
    {
        #region [ SERIALIZED FIELDS ]
        public string stepGUID;
        public string nodeGUID;
        public int snapshotId;
        public List<string> concurrentNodes;
        public List<GameObjectSerializableState> gameObjectsState;
        #endregion

        public bool Snapshot(GenericNode _step, int _snapshotId, List<GameObject> _gameObjects)
        {
            if (_step == null || _gameObjects == null || _gameObjects.Count == 0)
                return false;

            stepGUID = _step.StepGUID;
            nodeGUID = _step.Guid;
            snapshotId = _snapshotId;

            gameObjectsState = new List<GameObjectSerializableState>();

            GameObjectSerializableState newGameObjectState = null;
            foreach (var go in _gameObjects)
            {
                newGameObjectState = new GameObjectSerializableState();
                if (newGameObjectState.Snapshot(go))
                    gameObjectsState.Add(newGameObjectState);
            }

            return true;
        }

        public void AddConcurrentNode(string _nodeGUID)
        {
            if (!string.IsNullOrEmpty(_nodeGUID))
            {
                if (concurrentNodes == null)
                    concurrentNodes = new List<string>();

                if (!concurrentNodes.Contains(_nodeGUID) &&
                    _nodeGUID != stepGUID &&
                    _nodeGUID != nodeGUID)
                    concurrentNodes.Add(_nodeGUID);
            }
        }

        public void Restore()
        {
            foreach (var gameObjectState in gameObjectsState)
                gameObjectState.Restore();
        }

    }
}
