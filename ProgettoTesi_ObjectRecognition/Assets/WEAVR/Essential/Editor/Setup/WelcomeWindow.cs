using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TXT.WEAVR.Editor;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace TXT.WEAVR.Core
{

    [InitializeOnLoad]
    public class WelcomeWindow : EditorWindow
    {
        private const string k_WelcomeWindowStepKey = "WVR_WelcomeWindow_Step";
        private const string k_FirstRunFilename = ".WeavrInstalled.tmp";
        private const string k_RenderAssetsRelativePath = "Assets/WEAVR/Essential/Rendering/URP/Settings/";

        private static string FlagFilePath => k_FirstRunFilename;

        [MenuItem("WEAVR/Setup/Welcome Screen", priority = 100)]
        static void TestShowWindow()
        {
            PlayerPrefs.DeleteKey(k_WelcomeWindowStepKey);
            GetWindow<WelcomeWindow>();
            //RemoveInstallFlagFile();
        }

        private static void RemoveInstallFlagFile()
        {
            if(File.Exists(FlagFilePath))
            {
                File.Delete(FlagFilePath);
            }
        }

        private static bool IsFileFlagPresent() => File.Exists(FlagFilePath);

        private static void CreateFileFlag() => File.WriteAllText(FlagFilePath, $"WEAVR: {Weavr.VERSION}");

        static WelcomeWindow()
        {
            CheckIfWindowShouldBeDisplayed();
        }

        private static async void CheckIfWindowShouldBeDisplayed()
        {
            if (!IsFileFlagPresent())
            {
                await Task.Delay(1000);
                ShowWindowDelayed();
            }
        }

        private static async void ShowWindowDelayed()
        {
            await Task.Delay(1000);
            if(EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                return;
            }
            GetWindow<WelcomeWindow>();
        }

        VisualElement m_activePanel;
        VisualElement m_welcomeText;
        VisualElement m_extensionsPanel;
        VisualElement m_urpPresentMessage;
        VisualElement m_urpNotPresentMessage;

        Button m_nextButton;
        Button m_installURPButton;

        Action m_nextAction;

        private void OnEnable()
        {
            titleContent.text = "Welcome WEAVR";
            minSize = new Vector2(600, 700);
            var window = WeavrStyles.CreateFromTemplate("Windows/WelcomeWindow");
            rootVisualElement.Add(window);
            window.StretchToParentSize();

            window.AddStyleSheetPath("Styles/WelcomeWindow");

            m_activePanel = window.Q("CurrentStepContainer");
            m_welcomeText = window.Q("WelcomeText");
            m_extensionsPanel = window.Q("ExtensionsPanel");
            m_urpPresentMessage = window.Q("URP_PresentMessage");
            m_urpNotPresentMessage = window.Q("URP_NotPresentMessage");
            m_nextButton = window.Q<Button>("AcceptButton");
            m_installURPButton = window.Q<Button>("InstallURP");

            m_activePanel.Clear();

            m_nextButton.clicked -= NextButton_Clicked;
            m_nextButton.clicked += NextButton_Clicked;

            int currentStep = PlayerPrefs.GetInt(k_WelcomeWindowStepKey, 0);
            switch (currentStep)
            {
                case 0:
                    m_activePanel.Add(m_welcomeText);
                    m_nextAction = CheckURPPackage;
                    break;
                case 1:
                    CheckURPPackage();
                    break;
                case 2:
                    ShowExtensions();
                    break;
            }
        }

        private async void CheckURPPackage()
        {
            // Here we create the file flag (not to annoy the users if they don't want to finish the welcome)
            if (!IsFileFlagPresent()) { CreateFileFlag(); }

            m_nextAction = ShowExtensions;
            PlayerPrefs.SetInt(k_WelcomeWindowStepKey, 1);

            m_activePanel.Clear();
            var infoContainer = new VisualElement();
            infoContainer.AddToClassList("horizontal-layout");
            infoContainer.AddToClassList("centered-layout");
            var label = new Label("Please wait while fetching installed packages...");
            label.AddToClassList("gray-text");
            label.AddToClassList("wrapped-text");
            label.AddToClassList("centered-text");
            label.AddToClassList("medium-text");
            infoContainer.Add(label);
            m_activePanel.Add(infoContainer);

            var request = Client.List(true);

            while (request?.IsCompleted == false)
            {
                await Task.Yield();
            }

            if (request?.IsCompleted == true)
            {
                if (!string.IsNullOrEmpty(request.Error?.message))
                {
                    label.text = $"Error fetching packages: {request.Error.message}";
                }
                else
                {
                    label.text = "Fetching successful";
                    var packages = request.Result;
                    bool urpPresent = false;
                    foreach (var package in packages)
                    {
                        if (package.name.StartsWith("com.unity.render-pipelines.universal"))
                        {
                            urpPresent = true;
                            break;
                        }
                    }
                    if (urpPresent)
                    {
                        m_activePanel.Add(m_urpPresentMessage);
                        m_nextAction = ApplyURPSettings;
                        m_nextButton.text = "Next";
                    }
                    else
                    {
                        m_activePanel.Add(m_urpNotPresentMessage);
                        m_installURPButton.clicked += () => InstallURPButton_Clicked(label);
                        m_nextButton.text = "Skip";
                    }
                }
            }
            else
            {
                ShowExtensions();
            }
        }

        private void ApplyURPSettings()
        {
            var lowQualityAsset = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(k_RenderAssetsRelativePath + "WEAVR_URP-LowQuality.asset");
            var mediumQualityAsset = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(k_RenderAssetsRelativePath + "WEAVR_URP-MediumQuality.asset");
            var highQualityAsset = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(k_RenderAssetsRelativePath + "WEAVR_URP-HighQuality.asset");

            while(QualitySettings.GetQualityLevel() > 0)
            {
                QualitySettings.DecreaseLevel(false);
            }

            QualitySettings.renderPipeline = lowQualityAsset;

            int prevLevel = 0;
            QualitySettings.IncreaseLevel(false);
            while(QualitySettings.GetQualityLevel() != prevLevel)
            {
                QualitySettings.renderPipeline = mediumQualityAsset;
                prevLevel++;
                QualitySettings.IncreaseLevel(false);
            }

            QualitySettings.renderPipeline = highQualityAsset;

            ShowExtensions();
        }

        private async void InstallURPButton_Clicked(Label label)
        {
            m_installURPButton.SetEnabled(false);

            label.text = "Searching for URP...";
            var request = Client.Search("com.unity.render-pipelines.universal");

            while (request?.IsCompleted == false)
            {
                await Task.Yield();
            }

            if (request?.IsCompleted == true)
            {
                if (!string.IsNullOrEmpty(request.Error?.message))
                {
                    label.text = $"Error fetching packages: {request.Error.message}";
                }
                else
                {
                    label.text = "Cannot find URP package...";
                    var packages = request.Result;
                    foreach (var package in packages)
                    {
                        if (package.name.StartsWith("com.unity.render-pipelines.universal"))
                        {
                            label.text = "Installing URP package...";
                            try
                            {
                                m_nextButton.SetEnabled(false);
                                var addRequest = Client.Add(package.packageId);
                                while (addRequest?.IsCompleted == false)
                                {
                                    await Task.Yield();
                                }
                                if (addRequest?.IsCompleted == true)
                                {
                                    if (!string.IsNullOrEmpty(addRequest.Error?.message))
                                    {
                                        label.text = $"Error installing URP package: {request.Error.message}";
                                    }
                                    else
                                    {
                                        label.text = "URP Package installed successfully...";
                                        m_urpNotPresentMessage?.RemoveFromHierarchy();
                                        m_activePanel.Add(m_urpPresentMessage);
                                        m_nextAction = ApplyURPSettings;
                                        m_nextButton.text = "Next";
                                    }
                                }
                            }
                            finally
                            {
                                m_nextButton.SetEnabled(true);
                            }
                            break;
                        }
                    }
                }
            }
        }

        private void ShowExtensions()
        {
            // Here we create the file flag (not to annoy the users if they don't want to finish the welcome)
            if (!IsFileFlagPresent()) { CreateFileFlag(); }

            m_activePanel.Clear();
            m_activePanel.Add(m_extensionsPanel);
            PlayerPrefs.SetInt(k_WelcomeWindowStepKey, 2);

            ExtensionManager.ResetExtensions();

            var extensionToggles = m_extensionsPanel.Query<Toggle>(className: "extension-toggle").ToList();

            // VR
            extensionToggles[0].value = ExtensionManager.Extension_VR.IsEnabled;
            extensionToggles[0].RegisterValueChangedCallback(e => ExtensionManager.Extension_VR.IsEnabled = e.newValue);

            // AR
            extensionToggles[1].value = ExtensionManager.Extension_AR.IsEnabled;
            extensionToggles[1].RegisterValueChangedCallback(e => ExtensionManager.Extension_AR.IsEnabled = e.newValue);

            // Network
            extensionToggles[2].value = ExtensionManager.Extension_Network.IsEnabled;
            extensionToggles[2].RegisterValueChangedCallback(e => ExtensionManager.Extension_Network.IsEnabled = e.newValue);

            m_nextButton.text = "Finalize";
            m_nextAction = ApplyExtensions;
        }

        private void ApplyExtensions()
        {
            ExtensionManager.ApplyExtensionSetup(applyToAllGroups: true);

            FinalizeWelcome();
        }

        private void FinalizeWelcome()
        {
            PlayerPrefs.DeleteKey(k_WelcomeWindowStepKey);
            Close();
        }

        private void NextButton_Clicked()
        {
            m_nextAction?.Invoke();
        }
    }
}
