using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TXT.WEAVR.Procedure
{

    public class GraphUndo
    {

        private List<int> m_graphUndoIds = new List<int>();
        private int m_currentLevel = 0;

        public static Undo.UndoRedoCallback UndoRedoCallback;

        GraphUndo()
        {
            Undo.undoRedoPerformed += UndeRedoPerformed;
        }

        private void UndeRedoPerformed()
        {
            int currentGroup = Undo.GetCurrentGroup();

            while(m_graphUndoIds.Count > 0 && m_graphUndoIds[m_currentLevel - 1] > currentGroup)
            {
                m_currentLevel--;
            }
            
            if(m_graphUndoIds.Count > 0 && m_graphUndoIds[m_currentLevel-1] == currentGroup)
            {
                m_currentLevel--;
                UndoRedoCallback?.Invoke();
            }
        }

        private void RegisterGroup()
        {
            int currentGroup = Undo.GetCurrentGroup();
            while(m_graphUndoIds.Count > 0 && m_graphUndoIds.Count > m_currentLevel && m_graphUndoIds[m_currentLevel] > currentGroup)
            {
                m_currentLevel--;
            }
            if(m_graphUndoIds.Count > 0 && m_graphUndoIds[m_currentLevel - 1] == currentGroup)
            {
                return;
            }
            if(m_currentLevel >= m_graphUndoIds.Count)
            {
                m_graphUndoIds.Add(currentGroup);
                m_currentLevel++;
            }
            else if(m_graphUndoIds[m_currentLevel] != currentGroup)
            {
                m_graphUndoIds[m_currentLevel] = currentGroup;
                m_currentLevel++;
            }
        }

        private void ClearAllInternal()
        {
            m_currentLevel = 0;
            m_graphUndoIds.Clear();
        }

        private static GraphUndo s_instance;
        private static GraphUndo Instance
        {
            get
            {
                if(s_instance == null)
                {
                    s_instance = new GraphUndo();
                }
                return s_instance;
            }
        }

        public static Component AddComponent(GameObject gameObject, Type type) {
            var component = Undo.AddComponent(gameObject, type);
            Instance.RegisterGroup();
            return component;
        }

        public static T AddComponent<T>(GameObject gameObject) where T : Component {
            var component = Undo.AddComponent<T>(gameObject);
            Instance.RegisterGroup();
            return component;
        }
        //
        // Summary:
        //     Removes all undo and redo operations from respectively the undo and redo stacks.
        public static void ClearAll() {
            Undo.ClearAll();
            Instance.ClearAllInternal();
        }

        //
        // Summary:
        //     Removes all Undo operation for the identifier object registered using Undo.RegisterCompleteObjectUndo
        //     from the undo stack.
        //
        // Parameters:
        //   identifier:
        public static void ClearUndo(UnityEngine.Object identifier) {
            Undo.ClearUndo(identifier);
            Instance.RegisterGroup();
        }

        //
        // Summary:
        //     Collapses all undo operation up to group index together into one step.
        //
        // Parameters:
        //   groupIndex:
        public static void CollapseUndoOperations(int groupIndex) {
            Undo.CollapseUndoOperations(groupIndex);
            Instance.RegisterGroup();
        }

        //
        // Summary:
        //     Destroys the object and records an undo operation so that it can be recreated.
        //
        // Parameters:
        //   objectToUndo:
        //     The object that will be destroyed.
        public static void DestroyObjectImmediate(UnityEngine.Object objectToUndo) {
            Undo.DestroyObjectImmediate(objectToUndo);
            Instance.RegisterGroup();
        }

        //
        // Summary:
        //     Ensure objects recorded using RecordObject or ::ref:RecordObjects are registered
        //     as an undoable action. In most cases there is no reason to invoke FlushUndoRecordObjects
        //     since it's automatically done right after mouse-up and certain other events that
        //     conventionally marks the end of an action.
        public static void FlushUndoRecordObjects() {
            Undo.FlushUndoRecordObjects();
            Instance.RegisterGroup();
        }

        //
        // Summary:
        //     Unity automatically groups undo operations by the current group index.
        public static int GetCurrentGroup() {
            return Undo.GetCurrentGroup();
        }

        //
        // Summary:
        //     Get the name that will be shown in the UI for the current undo group.
        //
        // Returns:
        //     Name of the current group or an empty string if the current group is empty.
        public static string GetCurrentGroupName() {
            return Undo.GetCurrentGroupName();
        }
        
        //
        // Summary:
        //     Unity automatically groups undo operations by the current group index.
        public static void IncrementCurrentGroup() {
            Undo.IncrementCurrentGroup();
            Instance.RegisterGroup();
        }

        //
        // Summary:
        //     Move a GameObject from its current Scene to a new Scene. It is required that
        //     the GameObject is at the root of its current Scene.
        //
        // Parameters:
        //   go:
        //     GameObject to move.
        //
        //   scene:
        //     Scene to move the GameObject into.
        //
        //   name:
        //     Name of the undo action.
        public static void MoveGameObjectToScene(GameObject go, Scene scene, string name) {
            Undo.MoveGameObjectToScene(go, scene, name);
            Instance.RegisterGroup();
        }

        //
        // Summary:
        //     Perform an Redo operation.
        public static void PerformRedo() {
            Undo.PerformRedo();
        }

        //
        // Summary:
        //     Perform an Undo operation.
        public static void PerformUndo() {
            Undo.PerformUndo();
        }

        //
        // Summary:
        //     Records any changes done on the object after the RecordObject function.
        //
        // Parameters:
        //   objectToUndo:
        //     The reference to the object that you will be modifying.
        //
        //   name:
        //     The title of the action to appear in the undo history (i.e. visible in the undo
        //     menu).
        public static void RecordObject(UnityEngine.Object objectToUndo, string name) {
            Undo.RecordObject(objectToUndo, name);
            Instance.RegisterGroup();
        }

        //
        // Summary:
        //     Records multiple undoable objects in a single call. This is the same as calling
        //     Undo.RecordObject multiple times.
        //
        // Parameters:
        //   objectsToUndo:
        //
        //   name:
        public static void RecordObjects(UnityEngine.Object[] objectsToUndo, string name) {
            Undo.RecordObjects(objectsToUndo, name);
            Instance.RegisterGroup();
        }

        //
        // Summary:
        //     Stores a copy of the object states on the undo stack.
        //
        // Parameters:
        //   objectToUndo:
        //     The object whose state changes need to be undone.
        //
        //   name:
        //     The name of the undo operation.
        public static void RegisterCompleteObjectUndo(UnityEngine.Object objectToUndo, string name) {
            Undo.RegisterCompleteObjectUndo(objectToUndo, name);
            Instance.RegisterGroup();
        }

        //
        // Summary:
        //     This is equivalent to calling the first overload mutiple times, save for the
        //     fact that only one undo operation will be generated for this one.
        //
        // Parameters:
        //   objectsToUndo:
        //     An array of objects whose state changes need to be undone.
        //
        //   name:
        //     The name of the undo operation.
        public static void RegisterCompleteObjectUndo(UnityEngine.Object[] objectsToUndo, string name) {
            Undo.RegisterCompleteObjectUndo(objectsToUndo, name);
            Instance.RegisterGroup();
        }

        //
        // Summary:
        //     Register an undo operations for a newly created object.
        //
        // Parameters:
        //   objectToUndo:
        //     The object that was created.
        //
        //   name:
        //     The name of the action to undo. Think "Undo ...." in the main menu.
        public static void RegisterCreatedObjectUndo(UnityEngine.Object objectToUndo, string name) {
            Undo.RegisterCreatedObjectUndo(objectToUndo, name);
            Instance.RegisterGroup();
        }

        //
        // Summary:
        //     Copy the states of a hierarchy of objects onto the undo stack.
        //
        // Parameters:
        //   objectToUndo:
        //     The object used to determine a hierarchy of objects whose state changes need
        //     to be undone.
        //
        //   name:
        //     The name of the undo operation.
        public static void RegisterFullObjectHierarchyUndo(UnityEngine.Object objectToUndo, string name) {
            Undo.RegisterCreatedObjectUndo(objectToUndo, name);
            Instance.RegisterGroup();
        }

        //
        // Summary:
        //     Performs all undo operations up to the group index without storing a redo operation
        //     in the process.
        //
        // Parameters:
        //   group:
        public static void RevertAllDownToGroup(int group) {
            Undo.RevertAllDownToGroup(group);
            Instance.RegisterGroup(); 
        }

        //
        // Summary:
        //     Performs the last undo operation but does not record a redo operation.
        public static void RevertAllInCurrentGroup() {
            Undo.RevertAllInCurrentGroup();
            Instance.RegisterGroup();
        }

        //
        // Summary:
        //     Set the name of the current undo group.
        //
        // Parameters:
        //   name:
        //     New name of the current undo group.
        public static void SetCurrentGroupName(string name) {
            Undo.SetCurrentGroupName(name);
        }

        //
        // Summary:
        //     Sets the parent of transform to the new parent and records an undo operation.
        //
        // Parameters:
        //   transform:
        //     The Transform component whose parent is to be changed.
        //
        //   newParent:
        //     The parent Transform to be assigned.
        //
        //   name:
        //     The name of this action, to be stored in the Undo history buffer.
        public static void SetTransformParent(Transform transform, Transform newParent, string name) {
            Undo.SetTransformParent(transform, newParent, name);
            Instance.RegisterGroup();
        }
    }
}
