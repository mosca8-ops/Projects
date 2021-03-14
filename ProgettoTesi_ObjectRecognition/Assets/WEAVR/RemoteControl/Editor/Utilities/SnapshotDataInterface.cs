using System.Collections;
using System.Collections.Generic;
using System.IO;
using TXT.WEAVR.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TXT.WEAVR.RemoteControl
{

    public class SnapshotDataInterface
    {
#if !WEAVR_DLL && WEAVR_INTERNAL_USE
        [MenuItem("WEAVR/Diagnostics/Snapshot RC to C#")]
#endif
        public static void SnapshotCurrentSceneToCSharp()
        {
            Serializer serializer = new Serializer();
            WeavrRemoteControl.SetupConverters(serializer);
            DataInterface dface = new DataInterface(serializer, true);

            foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach(var commandUnit in root.GetComponentsInChildren<ICommandUnit>())
                {
                    dface.BindAllRemoteMethods(commandUnit);
                    dface.RegisterRemoteEvents(commandUnit);
                }
            }

            var filepath = EditorUtility.SaveFilePanel("Save Snapshot", Application.dataPath + "/../", "WeavrRC_Commands", "cs");
            File.WriteAllText(filepath, dface.SnapshotCommandsAndEventsToCSharp());
        }

#if !WEAVR_DLL && WEAVR_INTERNAL_USE
        [MenuItem("WEAVR/Diagnostics/Snapshot RC to File")]
#endif
        public static void SnapshotCurrentScene()
        {
            Serializer serializer = new Serializer();
            WeavrRemoteControl.SetupConverters(serializer);
            DataInterface dface = new DataInterface(serializer, true);

            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (var commandUnit in root.GetComponentsInChildren<ICommandUnit>())
                {
                    dface.BindAllRemoteMethods(commandUnit);
                    dface.RegisterRemoteEvents(commandUnit);
                }
            }

            var filepath = EditorUtility.SaveFilePanel("Save Snapshot", Application.dataPath + "/../", "WeavrRC_Commands", "txt");
            File.WriteAllText(filepath, dface.SnapshotCommandsAndEventsToFile());
        }
    }
}
