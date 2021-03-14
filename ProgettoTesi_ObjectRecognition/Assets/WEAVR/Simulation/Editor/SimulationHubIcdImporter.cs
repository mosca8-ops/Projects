using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.Simulation
{
    public class ImportIcd : EditorWindow
    {

        private enum ConfigurationStatus
        {
            OK = 0x00,
            MissingXML = 0x01,
            InvalidXMLPath = 0x02,
            MissingOutputPath = 0x04,
            MissingSharedMemoryName = 0x08,
        }

        private enum GenerationStatus
        {
            OK,
            Failed,
            None,
        }

        private static readonly Dictionary<ConfigurationStatus, string> mConfigurationMessages =
            new Dictionary<ConfigurationStatus, string>()
            {
                {ConfigurationStatus.MissingXML, "Please provide the path to the XML ICD"},
                {ConfigurationStatus.InvalidXMLPath, "Please provide a valid XML ICD path"},
                {ConfigurationStatus.MissingOutputPath, "Please provide the desired output path"},
                {ConfigurationStatus.MissingSharedMemoryName, "Please provide a SharedMemoryName"}
            };

        private static readonly Dictionary<GenerationStatus, string> mGenerationStatusMessages =
            new Dictionary<GenerationStatus, string>()
            {
                {GenerationStatus.OK, "Generation Successful"},
                {GenerationStatus.Failed, "Generation Failed"},
            };

        private string mXmlPath;
        private string mOutputPath;
        private string mSharedMemoryName = "SimHubShm";
        private string mMessage;
        private string mSimulationModelFilePath;
        private const string cSimulationModelFileName = "SimulationModel.cs";
        private string mSimulationStructureFilePath;
        private const string cSimulationStructureFileName = "SimulationHubIcd.cs";
        private ConfigurationStatus mConfigurationStatus = ConfigurationStatus.OK;
        private GenerationStatus mGenerationStatus = GenerationStatus.None;

        public virtual void OnEnable()
        {
            mOutputPath = Path.Combine(Application.dataPath, "Generated", "Simulation", "Player");
        }

#if !WEAVR_DLL
        [MenuItem("WEAVR/Simulation/Import SimulationHub ICD", priority = 40)]
#endif
        private static void ImportIcdWindow()
        {
            ImportIcd wImportIcd = CreateInstance(typeof(ImportIcd)) as ImportIcd;
            if (wImportIcd != null)
            {
                wImportIcd.ShowUtility();
                wImportIcd.maxSize = new Vector2(640, 160);
                wImportIcd.minSize = wImportIcd.maxSize;
                wImportIcd.titleContent = new GUIContent("SimulationHub Icd Importer");
            }
        }

        private void UpdateStatus()
        {
            mConfigurationStatus = ConfigurationStatus.OK;
            if (string.IsNullOrWhiteSpace(mXmlPath))
            {
                mConfigurationStatus = ConfigurationStatus.MissingXML;
            }
            else if (!File.Exists(mXmlPath))
            {
                mConfigurationStatus = ConfigurationStatus.InvalidXMLPath;
            }
            else if (string.IsNullOrWhiteSpace(mOutputPath))
            {
                mConfigurationStatus = ConfigurationStatus.MissingOutputPath;
            }
            else if (string.IsNullOrWhiteSpace(mSharedMemoryName))
            {
                mConfigurationStatus = ConfigurationStatus.MissingSharedMemoryName;
            }

            if (!mConfigurationMessages.TryGetValue(mConfigurationStatus, out mMessage))
            {
                mGenerationStatusMessages.TryGetValue(mGenerationStatus, out mMessage);
            }

            if (!string.IsNullOrWhiteSpace(mOutputPath))
            {
                mSimulationStructureFilePath = Path.Combine(mOutputPath, cSimulationStructureFileName);
                mSimulationModelFilePath = Path.Combine(mOutputPath, cSimulationModelFileName);
            }

        }

        private void OnGUI()
        {
            UpdateStatus();
            EditorGUILayout.BeginVertical();
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            mXmlPath = EditorGUILayout.TextField("Xml Path", mXmlPath);
            if (GUILayout.Button("Browse", GUILayout.MaxWidth(100)))
            {
                mXmlPath = EditorUtility.OpenFilePanel("Icd xml path", "", "xml");
                if (mXmlPath.Length != 0)
                {
                    //var fileContent = File.ReadAllBytes(path);
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            mOutputPath = EditorGUILayout.TextField("Output Path", mOutputPath);
            if (GUILayout.Button("Browse", GUILayout.MaxWidth(100)))
            {
                mOutputPath = EditorUtility.OpenFolderPanel("Output file path", "", "xml");
                if (mOutputPath.Length != 0)
                {
                    mOutputPath = Path.Combine(mOutputPath, "");
                }
            }
            EditorGUILayout.EndHorizontal();
            mSharedMemoryName = EditorGUILayout.TextField("SharedMemory Name", mSharedMemoryName);
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(mConfigurationStatus != ConfigurationStatus.OK);
            if (GUILayout.Button("Generate", GUILayout.MaxWidth(100)))
            {
                try
                {
                    SimulationHub.Generator wGenerator = new SimulationHub.Generator(mXmlPath);
                    wGenerator.GenerateUnityStruct(mSimulationStructureFilePath, true);
                    wGenerator.GenerateUnitySimulationModel(mSimulationModelFilePath, mSharedMemoryName);
                    mGenerationStatus = GenerationStatus.OK;
                }
                catch (Exception e)
                {
                    mGenerationStatus = GenerationStatus.Failed;
                    WeavrDebug.LogError(this, e.Message);
                }

            }
            EditorGUI.EndDisabledGroup();
            GUILayout.Label(mMessage);
            EditorGUILayout.EndHorizontal();
            GUILayout.Label("Generated Files", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Simulation Structures:", mSimulationStructureFilePath);
            EditorGUILayout.LabelField("Simulation Model:", mSimulationModelFilePath);
            EditorGUILayout.EndVertical();
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }
    }
}