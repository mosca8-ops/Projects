using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Common;
using TXT.WEAVR.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

using Object = UnityEngine.Object;
using UTools = UnityEditor.Tools;

namespace TXT.WEAVR.Procedure
{
    [CustomEditor(typeof(AnimationComposer), true)]
    class AnimationComposerEditor : ActionEditor
    {
        private const string k_ColorGroupName = "AnimationTracks";
        private const float k_Epsilon = 0.000001f;
        private AnimationComposer m_action;
        private bool m_collapsed;

        private class Styles : BaseStyles
        {
            public GUIContent playContent = new GUIContent(@" ▶");
            public GUIContent pauseContent = new GUIContent(@" ||");
            public GUIContent stopContent = new GUIContent(@" ■");
            public GUIContent removeContent = new GUIContent(@"✕");
            public GUIContent loopCountContent = new GUIContent("Count");
            public GUIContent previewContent = new GUIContent("P", "Preview Animations");
            public GUIContent viewInSceneContent = new GUIContent("S", "Preview in Scene");

            public GUIStyle box;
            public GUIStyle boxAnimated;
            public GUIStyle boxError;
            public float boxVertical;
            public float boxOffsetY;
            public GUIStyle removeButton;
            public GUIStyle addButton;
            public GUIStyle textToggle;
            public GUIStyle loopCountLabel;
            public GUIStyle blockIdLabel;

            public GUIStyle blockIdSceneLabel;

            protected override void InitializeStyles(bool isProSkin)
            {
                box = WeavrStyles.EditorSkin2.FindStyle("animationComposer_blockBox") ?? new GUIStyle("Box");
                boxAnimated = WeavrStyles.EditorSkin2.FindStyle("animationComposer_blockAnimated") ?? new GUIStyle("Box");
                boxError = WeavrStyles.EditorSkin2.FindStyle("animationComposer_blockError") ?? new GUIStyle("Box");
                boxVertical = box.margin.vertical + box.border.vertical + box.padding.vertical;
                boxOffsetY = box.margin.top + box.border.top + box.padding.top;
                removeButton = WeavrStyles.EditorSkin2.FindStyle("animationComposer_removeButton") ?? EditorStyles.miniButton;
                addButton = WeavrStyles.EditorSkin2.FindStyle("animationComposer_addButton") ?? new GUIStyle("Button");
                blockIdLabel = WeavrStyles.EditorSkin2.FindStyle("animationComposer_blockId") ?? new GUIStyle("Label");
                textToggle = new GUIStyle(WeavrStyles.EditorSkin2.FindStyle("actionEditor_TextToggle") ?? WeavrStyles.MiniToggleTextOn)
                { padding = new RectOffset(2, 2, 0, 2) };

                loopCountLabel = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                {
                    fontSize = 11,
                    alignment = TextAnchor.UpperLeft,
                    padding = new RectOffset(2, 0, 0, 1),
                };


                // Scene GUISTYLES
                blockIdSceneLabel = WeavrStyles.EditorSkin2.FindStyle("animationComposer_blockIdScene") ?? new GUIStyle("Label");
            }
        }

        private static Styles s_styles = new Styles();

        private GUIContent m_tempGUIContent = new GUIContent();

        private float m_time;
        private DateTime m_lastFrameTime;
        private float m_lastTime;
        private float m_duration;
        private int m_blocksCount;
        private bool m_isUpdatingTracks;
        private bool m_isUpdatingForwards;
        private bool m_showPreview;
        private bool m_viewInScene;
        private bool m_updatePreviewRoots;
        private bool m_isPlaying;
        private float m_sweepTime;
        private float m_direction = 1;
        private List<BaseAnimationBlock>[] m_tracks;
        protected List<PreviewRoot> m_previewRoots;
        private Dictionary<BaseAnimationBlock, Object> m_targets;
        private Dictionary<BaseAnimationBlock, float> m_durations;
        private HashSet<BaseAnimationBlock> m_previewableBlocks;
        private HashSet<string> m_errors;
        private float m_errorsHeight;
        protected Color[] m_trackColors = new Color[AnimationComposer.k_MaxTrackId + 1];

        protected override bool HasMiniPreview => !Application.isPlaying && m_action && m_action.AnimationBlocks.Count > 0;

        protected override float MiniPreviewHeight => s_baseStyles.fullLineHeight + m_errorsHeight;

        private MultiSelectionReorderableList<BaseAnimationBlock> m_blocksList;

        protected bool ShowPreview
        {
            get => m_showPreview;
            set
            {
                if (m_showPreview != value)
                {
                    m_showPreview = value;
                    ViewInScene = false;
                    //SceneView.onSceneGUIDelegate -= OnSceneGUI;
                    if (m_showPreview)
                    {
                        m_time = 0;
                        RebuildTrees();
                        ResetAnimationBlocks();
                        //UpdateAllPreviews();
                        ViewInScene = true;
                        //UpdateTracksStartPoints();
                        //SceneView.onSceneGUIDelegate += OnSceneGUI;
                    }
                    else
                    {
                        m_viewInScene = false;
                        Stop();
                        DisposePreviewNodes();
                        ResetAnimationBlocks();
                        //PopStates();
                    }
                    //if (SceneView.lastActiveSceneView != null)
                    //{
                    //    SceneView.lastActiveSceneView.Repaint();
                    //}
                }
            }
        }

        protected bool ViewInScene
        {
            get => m_viewInScene;
            set
            {
                if(m_viewInScene != value)
                {
                    m_viewInScene = value;
                    m_updatePreviewRoots = value;
                    if (value)
                    {
                        SetPreviewRootsVisibility(true);
                        Undo.undoRedoPerformed -= UndoPerformed;
                        Undo.undoRedoPerformed += UndoPerformed;
                        SceneView.duringSceneGui -= OnSceneGUI;
                        SceneView.duringSceneGui += OnSceneGUI;
                    }
                    else
                    {
                        SetPreviewRootsVisibility(false);
                        SceneView.duringSceneGui -= OnSceneGUI;
                        Undo.undoRedoPerformed -= UndoPerformed;
                    }
                    if (SceneView.lastActiveSceneView != null)
                    {
                        SceneView.lastActiveSceneView.Repaint();
                    }
                }
            }
        }
        
        public MultiSelectionReorderableList<BaseAnimationBlock> ReorderableList
        {
            get
            {
                if (m_blocksList == null)
                {
                    s_styles.Refresh();
                    m_blocksList = new MultiSelectionReorderableList<BaseAnimationBlock>(m_action, m_action.AnimationBlocks, true, true, false, false)
                    {
                        //onAddDropdownCallback = List_AddElement,
                        drawElementBackgroundCallback = List_DrawElementBackground,
                        drawElementCallback = List_DrawElement,
                        drawHeaderCallback = List_DrawHeader,
                        elementHeightCallback = List_GetElementHeight,
                        onChangedCallback = List_OnChanged,
                        headerHeight = 0,
                        drawNotVisibleElements = true,
                        onElementsPaste = List_OnElementsPaste,
                        //selectionColor = WeavrStyles.Colors.selection,
                        drawFooterCallback = List_DrawFooter,
                        footerHeight = s_styles.addButton.fixedHeight + s_styles.addButton.margin.vertical,
                        showDefaultBackground = false,
                        drawNoneElementCallback = List_NoElementsDraw,
                        onDeleteSelection = List_DeleteSelection,
                    };
                }
                return m_blocksList;
            }
        }

        private void List_OnElementsPaste(IEnumerable<object> pastedElements)
        {
            pastedElements?.Select(e => e as ProcedureObject).AssignProcedureToTree(m_action.Procedure, addToAssets: true);
        }

        protected override void OnEnable()
        {
            m_action = target as AnimationComposer;
            base.OnEnable();
            PrepareTracksColors();
            if (m_tracks == null)
            {
                m_tracks = new List<BaseAnimationBlock>[m_trackColors.Length];
                for (int i = 0; i < m_trackColors.Length; i++)
                {
                    m_tracks[i] = new List<BaseAnimationBlock>();
                }
            }
            if (m_action)
            {
                if (m_previewRoots == null)
                {
                    m_previewRoots = new List<PreviewRoot>();
                }
                if (m_targets == null)
                {
                    m_targets = new Dictionary<BaseAnimationBlock, Object>();
                }
                if (m_durations == null)
                {
                    m_durations = new Dictionary<BaseAnimationBlock, float>();
                }
                if(m_previewableBlocks == null)
                {
                    m_previewableBlocks = new HashSet<BaseAnimationBlock>(m_action.AnimationBlocks);
                }
                if(m_errors == null)
                {
                    m_errors = new HashSet<string>();
                }
                UpdateTracks();
                foreach (var block in m_action.AnimationBlocks)
                {
                    block.OnModified -= Block_OnModified;
                    block.OnModified += Block_OnModified;
                    if (block is ITargetingObject tObj)
                    {
                        m_targets[block] = tObj.Target;
                    }
                }
                m_blocksCount = m_action.AnimationBlocks.Count;
                m_action.OnModified -= Composer_OnModified;
                m_action.OnModified += Composer_OnModified;

                EditorApplication.playModeStateChanged -= EditorApplication_PlayModeStateChanged;
                EditorApplication.playModeStateChanged += EditorApplication_PlayModeStateChanged;
            }
        }

        private void EditorApplication_PlayModeStateChanged(PlayModeStateChange obj)
        {
            ShowPreview = false;
        }

        private void Composer_OnModified(ProcedureObject obj)
        {
            if(obj is AnimationComposer composer)
            {
                if (m_blocksCount != m_action.AnimationBlocks.Count)
                {
                    UpdateTracks();
                }
                m_blocksCount = m_action.AnimationBlocks.Count;
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            ShowPreview = false;
            EditorApplication.playModeStateChanged -= EditorApplication_PlayModeStateChanged;
            if (m_action)
            {
                m_action.OnModified -= Composer_OnModified;
                foreach (var elem in m_action.AnimationBlocks)
                {
                    DestroyEditor(elem);
                }
            }
        }

        protected virtual void Block_OnModified(ProcedureObject obj)
        {
            if (obj is BaseAnimationBlock block)
            {
                UpdateForwarding();
                if (!m_isUpdatingTracks && (!m_tracks[block.Track].Contains(block) || (m_durations.TryGetValue(block, out float d) && d != block.Duration)))
                {
                    UpdateTracks();
                }
                else if (m_showPreview)
                {
                    bool wasPlaying = IsPlaying();
                    
                    Stop();

                    if (block is ITargetingObject tObj && (!m_targets.TryGetValue(block, out Object target) || target != tObj.Target))
                    {
                        m_targets[block] = tObj.Target;
                        RebuildTrees();
                        UpdateAllPreviews();
                    }
                    else
                    {
                        UpdatePreview(block);
                    }

                    SetPreviewRootsVisibility(m_viewInScene);

                    if (wasPlaying)
                    {
                        Play();
                    }
                }
                m_action.Modified();
            }
        }
        
        private void ResetAnimationBlocks()
        {
            foreach (var block in m_action.AnimationBlocks)
            {
                block.Reset();
            }
        }

        private void UpdateForwarding()
        {
            if (m_isUpdatingForwards || !m_action || m_action.AnimationBlocks.Count == 0) { return; }
            m_isUpdatingForwards = true;
            BaseAnimationBlock[] lastTrackBlocks = new BaseAnimationBlock[AnimationComposer.k_MaxTrackId + 1];
            var prevBlock = m_action.AnimationBlocks[0];
            lastTrackBlocks[prevBlock.Track] = prevBlock;
            prevBlock.TargetSourceFrom = BaseAnimationBlock.DataSource.Self;
            for (int i = 1; i < m_action.AnimationBlocks.Count; i++)
            {
                var block = m_action.AnimationBlocks[i];
                if (block is ITargetingObject tt)
                {
                    switch (block.TargetSourceFrom)
                    {
                        case BaseAnimationBlock.DataSource.FromPrevious:
                            if (prevBlock is ITargetingObject prevTT)
                            {
                                tt.Target = prevTT.Target;
                            }
                            else
                            {
                                block.TargetSourceFrom = BaseAnimationBlock.DataSource.Self;
                            }
                            break;
                        case BaseAnimationBlock.DataSource.FromPreviousInTrack:
                            if (lastTrackBlocks[block.Track] is ITargetingObject prevTrackTT)
                            {
                                tt.Target = prevTrackTT.Target;
                            }
                            else
                            {
                                block.TargetSourceFrom = BaseAnimationBlock.DataSource.Self;
                            }
                            break;
                        case BaseAnimationBlock.DataSource.FromBlockIndex:
                            if (block.TargetSourceId == i)
                            {
                                block.TargetSourceFrom = BaseAnimationBlock.DataSource.Self;
                            }
                            else
                            {
                                int id = block.TargetSourceId;
                                bool isOk = false;
                                while (id >= 0 && id < m_action.AnimationBlocks.Count && m_action.AnimationBlocks[id] is ITargetingObject ttID)
                                {
                                    if(m_action.AnimationBlocks[id].TargetSourceFrom != BaseAnimationBlock.DataSource.FromBlockIndex)
                                    {
                                        tt.Target = ttID.Target;
                                        isOk = true;
                                        break;
                                    }
                                    if(m_action.AnimationBlocks[id].TargetSourceId == id)
                                    {
                                        break;
                                    }
                                    id = m_action.AnimationBlocks[id].TargetSourceId;
                                }
                                if (!isOk)
                                {
                                    block.TargetSourceFrom = BaseAnimationBlock.DataSource.Self;
                                }
                            }
                            break;
                    }
                    prevBlock = block;
                    lastTrackBlocks[block.Track] = block;
                }
            }
            m_isUpdatingForwards = false;
        }

        private void UpdatePreview(BaseAnimationBlock block)
        {
            foreach (var root in m_previewRoots)
            {
                root.UpdatePreview(block);
            }
        }

        private void UpdateAllPreviews()
        {
            foreach (var root in m_previewRoots)
            {
                if (root.animation is ITargetingObject tOjb && tOjb.Target)
                {
                    root.UpdatePreview(tOjb.Target.GetGameObject());
                }
            }
        }

        private void SetPreviewRootsVisibility(bool visible)
        {
            foreach (var root in m_previewRoots)
            {
                root.PropagateVisibility(true);
                root.PropagateVisibility(visible);
            }
        }


        protected void RebuildTrees()
        {
            DisposePreviewNodes();
            m_previewRoots = PreviewNode.BuildTrees(m_action.AnimationBlocks);
        }

        private void DisposePreviewNodes()
        {
            if (m_previewRoots != null)
            {
                foreach (var root in m_previewRoots)
                {
                    root.Dispose();
                }
                m_previewRoots.Clear();
                m_previewRoots = null;
            }
        }

        protected void UpdateTracks()
        {
            if (m_isUpdatingTracks) { return; }
            m_isUpdatingTracks = true;

            bool wasPlaying = IsPlaying();
            if (m_showPreview)
            {
                Stop();
            }
            // Rearange start time on tracks
            for (int i = 0; i < m_tracks.Length; i++)
            {
                m_tracks[i].Clear();
            }
            float[] endTimes = new float[AnimationComposer.k_MaxTrackId + 1];
            m_duration = 0.01f;
            foreach (var block in m_action.AnimationBlocks)
            {
                if (block.StartTime >= 0)
                {
                    block.StartTime = endTimes[block.Track];
                }
                float endTime = block is IAsyncAnimationBlock asyncBlock 
                             && asyncBlock.IsAsync 
                             && block != m_action.AnimationBlocks.Last() ? block.StartTime : block.EndTime;
                endTimes[block.Track] = endTime;
                m_duration = Mathf.Max(m_duration, endTime);
                m_tracks[block.Track].Add(block);
                m_durations[block] = block.Duration;
            }


            if (m_showPreview)
            {
                RebuildTrees();
                UpdateAllPreviews();
                SetPreviewRootsVisibility(m_viewInScene);
            }

            if (wasPlaying)
            {
                Play();
            }
            m_isUpdatingTracks = false;
        }

        private void PrepareTracksColors()
        {
            for (int i = 0; i < AnimationComposer.k_MinTrackId; i++)
            {
                m_trackColors[i] = Color.black;
            }
            var colorGroup = ProcedureDefaults.Current.ColorPalette.GetReadonlyGroup(k_ColorGroupName);
            for (int i = AnimationComposer.k_MinTrackId; i <= AnimationComposer.k_MaxTrackId; i++)
            {
                int color_i = i - AnimationComposer.k_MinTrackId;
                m_trackColors[i] = color_i < colorGroup.Count ? colorGroup[i - AnimationComposer.k_MinTrackId] : Color.black;
            }
        }

        protected override float GetHeightInternal()
        {
            return ReorderableList.GetHeight() + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }

        protected override void DrawProperties(Rect rect, SerializedProperty firstProperty)
        {
            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 100;
            EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), serializedObject.FindProperty("m_speed"));
            rect.y += s_baseStyles.fullLineHeight;
            rect.height -= s_baseStyles.fullLineHeight;
            var collapsed = serializedObject.FindProperty("m_uiCollapsed");
            collapsed.boolValue = !GUI.Toggle(new Rect(rect.x - 14, rect.y - 6, 16, 16), !collapsed.boolValue, GUIContent.none, EditorStyles.foldout);
            if(m_collapsed != collapsed.boolValue)
            {
                m_collapsed = collapsed.boolValue;
                if (m_showPreview) { SceneView.RepaintAll(); }
            }
            ReorderableList.DoList(rect);
            EditorGUIUtility.labelWidth = labelWidth;
        }

        private void List_OnChanged(ReorderableList list)
        {
            UpdateTracks();
        }

        private void List_DrawHeader(Rect rect)
        {

        }

        private void List_NoElementsDraw(Rect rect)
        {
            GUI.Label(rect, "No animation blocks", EditorStyles.centeredGreyMiniLabel);
        }

        private void List_DrawFooter(Rect rect)
        {
            if (m_action.AsyncThread != 0)
            {
                var r = new Rect(rect.x, rect.y, 34, rect.height);
                var property = serializedObject.FindProperty("m_loop");
                property.boolValue = GUI.Toggle(r, property.boolValue, "Loop", s_styles.textToggle);
                if (property.boolValue)
                {
                    r.x += r.width + 6;
                    r.width = 60;
                    property = serializedObject.FindProperty("m_alternate");
                    property.boolValue = GUI.Toggle(r, property.boolValue, "Alternate", s_styles.textToggle);

                    r.x += r.width + 6;
                    r.width = 40;
                    GUI.Label(r, s_styles.loopCountContent);
                    r.x += r.width + 4;
                    r.width = 60;
                    property = serializedObject.FindProperty("m_loopCount");
                    property.intValue = (int)GUI.HorizontalSlider(r, property.intValue, 1, AnimationComposer.k_MaxLoopCount);

                    r.x += r.width + 4;
                    r.width = 42;
                    GUI.Label(r, property.intValue >= AnimationComposer.k_MaxLoopCount ? "infinite" : property.intValue.ToString(), s_styles.loopCountLabel);
                    //property.intValue = EditorGUI.IntSlider(r, property.intValue, 1, AnimationComposer.k_MaxLoopCount);
                }
            }

            if (GUI.Button(new Rect(rect.x + rect.width - 50, rect.y, 50, rect.height), "+ Block", s_styles.addButton))
            {
                AddItemWindow.Show(rect, ProcedureDefaults.Current.AnimationBlocksCatalogue, d => AddAnimationBlock(d as AnimationDescriptor));
            }
        }

        private void AddAnimationBlock(AnimationDescriptor descriptor)
        {

            //m_preRenderAction = () => m_action.AnimationBlocks.Add(CreateInstance<DeltaMoveBlock>());
            m_preRenderAction = () =>
            {
                Type type = descriptor?.Sample.GetType();
                var lastSameAnimation = m_action.AnimationBlocks.LastOrDefault(a => a.GetType() == type && a.Variant == descriptor.Variant);

                var newAnimBlock = lastSameAnimation ? Instantiate(lastSameAnimation) : descriptor.Create();
                if (!(newAnimBlock is ITargetingObject))
                {
                    newAnimBlock.TryAssignSceneReferences(m_action.AnimationBlocks);
                }
                else if (/*!lastSameAnimation &&*/ !(newAnimBlock as ITargetingObject).Target)
                {
                    (newAnimBlock as ITargetingObject).Target = (m_action.AnimationBlocks.LastOrDefault(a => a is ITargetingObject tObj && tObj.Target) as ITargetingObject)?.Target;
                }

                m_action.RegisterProcedureObject(newAnimBlock);

                Undo.RegisterCreatedObjectUndo(newAnimBlock, "Created Animation Block");
                Undo.RegisterCompleteObjectUndo(m_action, "Inserted Animation Block");
                m_action.AnimationBlocks.Add(newAnimBlock);
                if (Get(newAnimBlock) is ISmartCreatedCallback smartCreated)
                {
                    smartCreated.OnSmartCreated(lastSameAnimation);
                }
                newAnimBlock.Composer = m_action;
                newAnimBlock.OnModified -= Block_OnModified;
                newAnimBlock.OnModified += Block_OnModified;
                newAnimBlock.Procedure = m_action.Procedure;
                m_action.Modified();
            };
        }

        private float List_GetElementHeight(int index)
        {
            var editor = Get(m_action.AnimationBlocks[index]) as AnimationBlockEditor;
            editor.ComposerEditor = this;
            return (m_collapsed ? editor.GetHeightCollapsed() : editor.GetHeight()) + s_styles.boxVertical;
        }

        private void List_DrawElementBackground(Rect rect, int index, bool isActive, bool isFocused)
        {
            rect.y += s_styles.box.margin.top;
            rect.height -= s_styles.box.margin.vertical;
            if(index < 0) {
                s_styles.box.Draw(rect, rect.Contains(Event.current.mousePosition), false, isFocused, false);
                return;
            }
            //bool isHovering = rect.Contains(Event.current.mousePosition);

            if (m_action.AnimationBlocks[index].HasErrors)
            {
                s_styles.boxError.Draw(rect, rect.Contains(Event.current.mousePosition), false, isFocused, false);
                m_errors.Add(m_action.AnimationBlocks[index].ErrorMessage);
            }
            else if (m_action.AnimationBlocks[index].IsAnimating)
            {
                s_styles.boxAnimated.Draw(rect, rect.Contains(Event.current.mousePosition), false, isFocused, false);
            }
            else
            {
                s_styles.box.Draw(rect, rect.Contains(Event.current.mousePosition), false, isFocused, false);
            }

            rect.width = 1;
            rect.y += s_styles.box.border.top;
            rect.height -= s_styles.box.border.vertical;
            EditorGUI.DrawRect(rect, m_trackColors[m_action.AnimationBlocks[index].Track]);

            rect.width = s_styles.blockIdLabel.fixedWidth;
            rect.x += s_styles.blockIdLabel.margin.left;
            s_styles.blockIdLabel.Draw(rect, index.ToString(), false, false, false, false);
            //s_styles.box.Draw(rect, false, false, false, false);
        }

        private void List_DrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            //EditorGUI.DrawRect(rect, Color.red);
            rect.y += s_styles.boxOffsetY;
            rect.height -= s_styles.boxVertical;

            if (m_collapsed)
            {
                (Get(m_action.AnimationBlocks[index]) as AnimationBlockEditor).DrawCollapsed(rect);
            }
            else
            {
                Get(m_action.AnimationBlocks[index]).DrawFull(rect);
            }

            Element_DrawRemoveButton(rect, index, isActive, isFocused);
        }

        private void List_DeleteSelection(List<BaseAnimationBlock> selection)
        {
            m_preRenderAction = () =>
            {
                Undo.RegisterCompleteObjectUndo(m_action, "Removed Animation Blocks");
                foreach (var element in selection)
                {
                    RemoveElement(element);
                }
                m_action.Modified();
            };
        }

        private void Element_DrawRemoveButton(Rect rect, int index, bool isActive, bool isFocused)
        {
            var size = s_styles.removeButton.CalcSize(s_styles.removeContent);
            rect.x += rect.width - size.x + s_styles.removeButton.margin.left;
            rect.y += s_styles.removeButton.margin.top;
            rect.size = size;
            if (GUI.Button(rect, s_styles.removeContent, s_styles.removeButton))
            {
                m_preRenderAction = () =>
                {
                    var element = m_action.AnimationBlocks[index];
                    Undo.RegisterCompleteObjectUndo(m_action, "Removed Animation Block");
                    RemoveElement(element);
                    m_action.Modified();
                };
            }
        }

        private void RemoveElement(BaseAnimationBlock element)
        {
            if (m_action.AnimationBlocks.Remove(element))
            {
                DestroyEditor(element);
                if (element && m_action.Procedure)
                {
                    m_action.Procedure.Graph.ReferencesTable.RemoveTargetCompletely(element);
                }
                element.DestroyAsset();
            }
        }

        public override void DrawLayoutSelective(List<string> propertiesToHide)
        {
            if(m_preRenderAction != null)
            {
                m_preRenderAction();
                m_preRenderAction = null;
            }
            s_baseStyles.Refresh();
            DoHeaderLayout();
            ReorderableList.DoLayoutList();
        }


        protected override void DrawMiniPreview(Rect r)
        {
            if(m_errors?.Count > 0)
            {
                m_errorsHeight = 0;
                if (GUI.Button(new Rect(r.x + r.width - 80, r.y, 80, EditorGUIUtility.singleLineHeight), "Clear Errors"))
                {
                    foreach (var block in m_action.AnimationBlocks)
                    {
                        block.MuteEvents = true;
                        block.ErrorMessage = null;
                        block.MuteEvents = false;
                    }
                    m_errors.Clear();
                    m_action.ErrorMessage = null;
                }
                else
                {
                    r.y += s_baseStyles.fullLineHeight;
                    foreach (var error in m_errors)
                    {
                        m_tempGUIContent.text = error;
                        r.height = EditorStyles.helpBox.CalcHeight(m_tempGUIContent, r.width);
                        m_errorsHeight += r.height + EditorGUIUtility.standardVerticalSpacing;
                        EditorGUI.HelpBox(r, error, MessageType.Error);
                        r.y += r.height + EditorGUIUtility.standardVerticalSpacing;
                    }
                    m_errorsHeight += s_baseStyles.fullLineHeight;
                }
            }

            if (m_showPreview)
            {
                ShowPreview = GUI.Toggle(new Rect(r.x, r.y, 25, EditorGUIUtility.singleLineHeight), m_showPreview,
                                    s_styles.previewContent, EditorStyles.miniButtonLeft);
            }
            else
            {
                ShowPreview = GUI.Toggle(new Rect(r.x, r.y, 25, EditorGUIUtility.singleLineHeight), m_showPreview,
                                        s_styles.previewContent, "Button");
            }

            if (!m_showPreview) { return; }

            ViewInScene = GUI.Toggle(new Rect(r.x + 25, r.y, 25, EditorGUIUtility.singleLineHeight), m_viewInScene, 
                                    s_styles.viewInSceneContent, EditorStyles.miniButtonRight);

            r.height = EditorGUIUtility.singleLineHeight;
            r.x += 55;
            r.width -= 110;
            var guiColor = GUI.color;
            bool wasEnabled = GUI.enabled;
            if(m_duration < 0.25f)
            {
                if (IsPlaying())
                {
                    Stop();
                }
                GUI.enabled = false;
            }
            GUI.color = m_time > k_Epsilon || IsPlaying() ? Color.cyan : GUI.color;
            var newTime = EditorGUI.Slider(r, m_time, 0, m_duration);
            if (newTime != m_time)
            {
                m_time = newTime;
                AnimateBlocks();
            }
            GUI.color = guiColor;
            r.x += r.width + 5;
            r.width = 25;

            if(IsPlaying())
            {
                if(GUI.Button(r, s_styles.pauseContent, EditorStyles.miniButtonLeft))
                {
                    Pause();
                }
            }
            else if (GUI.Button(r, s_styles.playContent, EditorStyles.miniButtonLeft))
            {
                if (IsPlaying())
                {
                    Stop();
                }
                Play();
            }

            r.x += r.width;
            EditorGUI.BeginDisabledGroup(m_time <= k_Epsilon);
            if (GUI.Button(r, s_styles.stopContent))
            {
                Stop();
            }
            EditorGUI.EndDisabledGroup();

            GUI.enabled = wasEnabled;

            if (IsPlaying())
            {
                ProcedureObjectInspector.RepaintFull();
            }
        }

        protected virtual void Play()
        {
            m_isPlaying = true;
            //m_direction = 1;
            EditorApplication.update -= PlayUpdate;
            EditorApplication.update += PlayUpdate;
        }

        protected virtual void Stop()
        {
            EditorApplication.update -= PlayUpdate;
            SoftStop();
        }

        private void SoftStop()
        {
            m_isPlaying = false;
            m_time = 0;
            if (m_action)
            {
                AnimateBlocks();
                foreach (var block in m_action.AnimationBlocks)
                {
                    block.Reset();
                }
            }
            m_sweepTime = -1;
        }

        protected virtual void Pause()
        {
            m_isPlaying = false;
            EditorApplication.update -= PlayUpdate;
        }

        protected virtual bool IsPlaying()
        {
            return m_isPlaying;
        }

        private void PlayUpdate()
        {
            var now = DateTime.Now;
            float deltaTime = Mathf.Min((float)((now - m_lastFrameTime).TotalSeconds), 0.1f);
            m_lastFrameTime = now;
            //float deltaTime = Mathf.Min((float)(EditorApplication.timeSinceStartup - m_lastTimeSinceStartup), Time.fixedDeltaTime);
            //m_lastTimeSinceStartup = EditorApplication.timeSinceStartup;
            m_time += deltaTime * m_action.Speed * m_direction;
            if (m_time > m_duration)
            {
                if (m_action.Speed < 0)
                {
                    m_direction = 1;
                    m_time = m_duration;
                }
                else if (m_action.IsAlternating)
                {
                    m_direction = -1;
                    m_time = m_duration;
                }
                else
                {
                    m_direction = 1;
                    m_time = 0;
                }
            }
            else if (m_time < 0)
            {
                if (m_action.Speed > 0)
                {
                    m_direction = 1;
                    m_time = 0;
                }
                else if (m_action.IsAlternating)
                {
                    m_direction = -1;
                    m_time = 0;
                }
                else
                {
                    m_direction = 1;
                    m_time = m_duration;
                }
            }
            AnimateBlocks();
        }

        private void AnimateBlocks()
        {
            //if (m_sweepTime)
            //{
            //    m_sweepTime = false;
            //    var ordered = m_action.AnimationBlocks.OrderBy(b => b.StartTime).ToArray();
            //    for (int i = 0; i < ordered.Length; i++)
            //    {
            //        float startTime = ordered[i].StartTime;
            //        for (int j = 0; j < i; j++)
            //        {
            //            try
            //            {
            //                ordered[j].Animate(startTime);
            //            }
            //            catch(Exception e)
            //            {
            //                Debug.LogError(e);
            //            }
            //        }

            //        try
            //        {
            //            ordered[i].OnStart();
            //        }
            //        catch(Exception e)
            //        {
            //            Debug.LogError(e);
            //        }
            //    }
            //}

            if (m_time > m_sweepTime)
            {
                var blocks = m_action.AnimationBlocks;
                for (int i = 0; i < blocks.Count; i++)
                {
                    float startTime = blocks[i].StartTime;
                    if (m_time < startTime)
                    {
                        break;
                    }
                    else if (m_sweepTime > startTime)
                    {
                        continue;
                    }
                    for (int j = 0; j < i; j++)
                    {
                        if (blocks[j].StartTime <= startTime)
                        {
                            try
                            {
                                blocks[j].Animate(startTime);
                            }
                            catch (Exception e)
                            {
                                blocks[i].ErrorMessage = $"[Animation {j}].Animate: {e.Message}";
                            }
                        }
                    }
                    try
                    {
                        blocks[i].OnStart();
                    }
                    catch (Exception e)
                    {
                        blocks[i].ErrorMessage = $"[Animation {i}].OnStart: {e.Message}";
                    }
                }
            }
            m_sweepTime = Mathf.Max(m_time, m_sweepTime);

            if (m_time > m_lastTime)
            {
                foreach (var block in m_action.AnimationBlocks)
                {
                    try
                    {
                        block.Animate(m_time);
                    }
                    catch(Exception e)
                    {
                        Debug.LogError(e);
                    }
                }
            }
            else
            {
                for (int i = m_action.AnimationBlocks.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        m_action.AnimationBlocks[i].Animate(m_time);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                    }
                }
            }
            m_lastTime = m_time;
        }

        #region [  POSE HANDLING  ]

        protected virtual void OnSceneGUI(SceneView sceneView)
        {
            if (!m_viewInScene) { return; }
            bool canGetPose = m_updatePreviewRoots && !IsPlaying();

            m_updatePreviewRoots = false;

            Dictionary<Vector3, string> labels = new Dictionary<Vector3, string>();
            for (int i = 0; i < m_previewRoots.Count; i++)
            {
                var root = m_previewRoots[i];
                if (canGetPose)
                {
                    root.Update();
                }
                root.DrawPreview(labels, m_trackColors, CanShowTools);
            }

            foreach (var pair in labels)
            {
                Handles.Label(pair.Key, pair.Value, s_styles.blockIdSceneLabel);
            }
        }

        private void UndoPerformed()
        {
            if (!m_viewInScene) { return; }
            for (int i = 0; i < m_previewRoots.Count; i++)
            {
                m_previewRoots[i]?.MarkDirty();
            }
        }


        private bool CanShowTools(BaseAnimationBlock block)
        {
            return !m_collapsed || (Get(block) as AnimationBlockEditor).IsExpanded;
        }
        
        #endregion

        #region [  PREVIEW TREES  ]

        protected class PreviewRoot : PreviewNode
        {
            public GameObject rootGo { get; private set; }

            private List<SerializedObject> m_serObjs;

            private bool m_requestedAnUpdate;
            private Vector3 m_startPoint;
            private Quaternion m_startRotation;
            public override Vector3 StartPosition => m_startPoint;
            protected override Quaternion StartRotation => m_startRotation;

            protected override GameObject parentGameObject => rootGo;
            

            public PreviewRoot(BaseAnimationBlock block) : base(null, block)
            {
                rootGo = (block as ITargetingObject).Target.GetGameObject();
                m_startPoint = rootGo ? rootGo.transform.position : Vector3.zero;
                root = this;
                m_serObjs = new List<SerializedObject>();
                foreach(var material in rootGo.GetComponentsInChildren<Renderer>().SelectMany(r => r.sharedMaterials))
                {
                    var serObj = new SerializedObject(material);
                    serObj.Update();
                    m_serObjs.Add(serObj);
                }
                foreach (var component in rootGo.GetComponents<Component>())
                {
                    var serObj = new SerializedObject(component);
                    serObj.Update();
                    m_serObjs.Add(serObj);
                }
            }

            public void MarkDirty() => m_requestedAnUpdate = true;

            public void Update()
            {
                if (rootGo.transform.hasChanged || m_requestedAnUpdate)
                {
                    m_requestedAnUpdate = false;
                    m_startPoint = rootGo.transform.position;
                    m_startRotation = rootGo.transform.rotation.normalized;
                    UpdatePreview(rootGo);
                    rootGo.transform.hasChanged = false;
                }
            }

            public void SaveCurrentState()
            {
                foreach (var serObj in m_serObjs)
                {
                    serObj.Update();
                }
            }

            public override void Dispose()
            {
                base.Dispose();
                
                // First restore common properties
                var currentSerObjs = rootGo.GetComponents<Component>().Select(c => new SerializedObject(c)).ToDictionary(s => s.targetObject);
                foreach (var serObj in m_serObjs)
                {
                    if (currentSerObjs.TryGetValue(serObj.targetObject, out SerializedObject curSerObj))
                    {
                        curSerObj.Update();
                        var iterator = serObj.GetIterator();
                        iterator.Next(true);
                        while (iterator.Next(iterator.propertyType == SerializedPropertyType.Generic))
                        {
                            curSerObj.CopyFromSerializedProperty(iterator);
                        }
                        curSerObj.ApplyModifiedProperties();
                    }
                    else
                    {
                        serObj.ApplyModifiedProperties();
                    }
                }

                // Then restore the materials
                var materialsObjs = rootGo.GetComponentsInChildren<Renderer>().SelectMany(r => r.sharedMaterials).Select(m => new SerializedObject(m)).ToDictionary(s => s.targetObject);
                foreach (var serObj in m_serObjs)
                {
                    if (materialsObjs.TryGetValue(serObj.targetObject, out SerializedObject curSerObj))
                    {
                        curSerObj.Update();
                        var iterator = serObj.GetIterator();
                        iterator.Next(true);
                        while (iterator.Next(iterator.propertyType == SerializedPropertyType.Generic))
                        {
                            curSerObj.CopyFromSerializedProperty(iterator);
                        }
                        curSerObj.ApplyModifiedProperties();
                    }
                    else
                    {
                        serObj.ApplyModifiedProperties();
                    }
                }
            }
        }
        
        protected class PreviewNode : IDisposable
        {
            protected class PropertiesPair
            {
                public SerializedProperty enableProperty;
                public SerializedProperty valueProperty;
            }


            public PreviewRoot root { get; protected set; }

            private PreviewNode m_parent;
            public PreviewNode parent
            {
                get => m_parent;
                private set
                {
                    if (m_parent != value)
                    {
                        if (m_parent != null)
                        {
                            m_parent.children.Remove(this);
                        }
                        root = null;
                        m_parent = value;
                        if (m_parent != null)
                        {
                            root = m_parent.root;
                            m_parent.children.Add(this);
                        }
                    }
                }
            }

            public bool IsVisible => previewGO?.IsVisible ?? false;

            public List<PreviewNode> children { get; private set; }
            public IPreviewAnimation animation { get; private set; }
            public BaseAnimationBlock block { get; private set; }
            public TempGameObject previewGO { get; private set; }

            protected virtual GameObject parentGameObject => m_parent?.previewGO.GameObject;
            
            private SerializedObject m_serObj;
            private Dictionary<UseHandleAttribute, PropertiesPair> m_properties;

            public virtual Vector3 StartPosition => parent?.previewGO?.GameObject.transform.position ?? Vector3.zero;
            protected virtual Quaternion StartRotation => m_parent?.previewGO?.GameObject.transform.rotation ?? Quaternion.identity;
            public virtual Vector3 EndPosition => previewGO?.GameObject.transform.position ?? Vector3.zero;
            protected virtual Quaternion EndRotation => previewGO?.GameObject.transform.rotation ?? Quaternion.identity;


            private bool m_isPreviewing;

            
            protected PreviewNode(PreviewNode parent, BaseAnimationBlock block)
            {
                this.parent = parent;
                children = new List<PreviewNode>();
                animation = block as IPreviewAnimation;
                this.block = block;

                ScanForPropertiesToHandle();
            }

            private void ScanForPropertiesToHandle()
            {
                m_properties = new Dictionary<UseHandleAttribute, PropertiesPair>();
                m_serObj = new SerializedObject(block);
                m_serObj.Update();
                var iterator = m_serObj.FindProperty(nameof(BaseAnimationBlock.separator));
                bool enterChildren = true;
                while(iterator.NextVisible(enterChildren && iterator.propertyType == SerializedPropertyType.Generic))
                {
                    enterChildren = true;
                    var attr = iterator.GetAttribute<UseHandleAttribute>();
                    if(attr != null)
                    {
                        var pair = new PropertiesPair();
                        if (iterator.type.ToLower().StartsWith("optional"))
                        {
                            // It is an optional
                            pair.enableProperty = iterator.FindPropertyRelative(nameof(Optional.enabled));
                            pair.valueProperty = iterator.FindPropertyRelative("value");
                            enterChildren = false;
                        }
                        else
                        {
                            pair.valueProperty = iterator.Copy();
                        }
                        m_properties[attr] = pair;
                    }
                }

                if(m_properties.Count == 0)
                {
                    m_serObj.Dispose();
                    m_serObj = null;
                    m_properties = null;
                }
            }

            public void PropagateVisibility(bool visible)
            {
                if(previewGO != null)
                {
                    previewGO.IsVisible = visible;
                }
                foreach(var child in children)
                {
                    child.PropagateVisibility(visible);
                }
            }

            private void AddChild(PreviewNode node)
            {
                node.parent = this;
            }

            private void RemoveChild(PreviewNode node)
            {
                if (node.parent == this)
                {
                    node.parent = null;
                }
            }

            public void UpdatePreview(GameObject source)
            {
                if (!source && parent != null)
                {
                    source = parentGameObject;
                    if (source == null)
                    {
                        return;
                    }
                }

                previewGO?.Dispose();
                previewGO = new TempGameObject(source);
                previewGO.GameObject.transform.SetParent(source.transform.parent, false);

                if (animation.CanPreview())
                {
                    animation.ApplyPreview(previewGO.GameObject);
                }
                
                foreach (var child in children)
                {
                    child.UpdatePreview(previewGO.GameObject);
                }
            }

            private void ApplyPreview(GameObject source, ITargetingObject targetingObj)
            {
                m_isPreviewing = true;
                var prevTarget = targetingObj.Target;
                try
                {
                    AnimateTopDown(block.StartTime);
                    previewGO = new TempGameObject(source);
                    previewGO.GameObject.transform.SetParent(source.transform.parent, false);
                    targetingObj.Target = previewGO.GameObject;
                    block.OnStart();
                    block.Animate(1);

                    foreach (var child in children)
                    {
                        child.UpdatePreview(previewGO.GameObject);
                    }

                    block.Animate(1);
                    //animation.ApplyPreview(previewGO.GameObject);
                }
                catch
                {
                    previewGO = new TempGameObject(source);
                }
                targetingObj.Target = prevTarget;
                m_isPreviewing = false;
            }

            private void AnimateTopDown(float startTime)
            {
                if (parent != null && parent.m_isPreviewing)
                {
                    parent.AnimateTopDown(startTime);
                    parent.block.Animate(startTime);
                }
            }

            public void UpdatePreview(BaseAnimationBlock block)
            {
                if (this.block != block)
                {
                    foreach (var child in children)
                    {
                        child.UpdatePreview(block);
                    }
                }
                else if (parent == null)
                {
                    if (block is ITargetingObject tObj && tObj.Target)
                    {
                        UpdatePreview(tObj.Target.GetGameObject());
                    }
                }
                else
                {
                    UpdatePreview(parentGameObject);
                }
            }

            public virtual void DrawPreview(Dictionary<Vector3, string> labels, Color[] trackColors, Func<BaseAnimationBlock, bool> canShowToolsCallback)
            {
                Handles.color = trackColors[block.Track];
                Vector3 endPoint = EndPosition;
                if (previewGO != null)
                {
                    Handles.DrawLine(StartPosition, endPoint);
                    if(m_serObj != null && m_serObj.targetObject && canShowToolsCallback != null && canShowToolsCallback(block))
                    {
                        m_serObj.Update();
                        DrawHandles(trackColors[block.Track]);
                        if (m_serObj.ApplyModifiedProperties())
                        {
                            block.Modified();
                        }
                    }
                }
                if (labels.TryGetValue(endPoint, out string label))
                {
                    if(parent != null && parent.previewGO != null)
                    {
                        parent.previewGO.IsVisible = false;
                    }
                    label += ", " + block.Index;
                }
                else
                {
                    label = block.Index.ToString();
                }

                labels[endPoint] = label;
                previewGO.IsVisible = true;

                foreach (var child in children)
                {
                    child.DrawPreview(labels, trackColors, canShowToolsCallback);
                }
            }

            protected virtual void DrawHandles(Color color)
            {
                var handlesMatrix = Handles.matrix;
                foreach(var pair in m_properties)
                {
                    if(pair.Value.enableProperty != null && !pair.Value.enableProperty.boolValue)
                    {
                        continue;
                    }

                    var property = pair.Value.valueProperty;
                    var handle = pair.Key;
                    bool isLocal = handle.Space == HandleSpace.Local || (handle.Space == HandleSpace.Auto && UTools.pivotRotation == PivotRotation.Local);
                    //Handles.matrix = parentGameObject.transform.localToWorldMatrix;
                    //Handles.matrix = Matrix4x4.TRS(parentGameObject.transform.position, Quaternion.identity, Vector3.one);
                    switch (handle.Type)
                    {
                        case HandleType.Position:
                            Handles.matrix = Matrix4x4.TRS(StartPosition, Quaternion.identity, Vector3.one);
                            switch (property.propertyType)
                            {
                                case SerializedPropertyType.Vector3:
                                    property.vector3Value = HandlePosition(property.vector3Value, handle);
                                    break;
                                case SerializedPropertyType.Vector2:
                                    property.vector2Value = HandlePosition(property.vector2Value, handle);
                                    break;
                                //case SerializedPropertyType.Vector2Int:

                                //    break;
                                //case SerializedPropertyType.Vector3Int:

                                //    break;
                                default:
                                    Debug.LogError($"[Handle Draw]: Property {property.propertyPath} is not suitable for handle {handle.Type}");
                                    break;
                            }
                            break;
                        case HandleType.Rotation:
                            if (property.propertyType == SerializedPropertyType.Quaternion)
                            {
                                var q = property.quaternionValue;
                                if(q.x == 0 && q.y == 0 && q.z == 0 && q.w == 0)
                                {
                                    property.quaternionValue = Quaternion.identity;
                                }
                                Handles.matrix = Matrix4x4.TRS(EndPosition, StartRotation, Vector3.one);
                                property.quaternionValue = Handles.RotationHandle(property.quaternionValue, Vector3.zero);
                                //property.quaternionValue = Handles.FreeRotateHandle(property.quaternionValue, Vector3.zero, HandleUtility.GetHandleSize(Vector3.zero));
                                //Debug.Log($"Rotation: {property.quaternionValue.eulerAngles}");
                                //property.quaternionValue = lastRotation * Quaternion.Inverse(parentGameObject.transform.rotation.normalized);
                            }
                            else if(property.propertyType == SerializedPropertyType.Vector3)
                            {
                                using (new Handles.DrawingScope())
                                {
                                    var vector = property.vector3Value;
                                    Handles.matrix = Matrix4x4.TRS(EndPosition, Quaternion.identity, Vector3.one);
                                    var startEuler = StartRotation.eulerAngles;
                                    var zRotation = Quaternion.Euler(previewGO.GameObject.transform.forward);
                                    var deltaZ = Handles.Disc(zRotation, Vector3.zero, previewGO.GameObject.transform.forward, HandleUtility.GetHandleSize(Vector3.zero), false, 0) * Quaternion.Inverse(zRotation);
                                    // TODO: Finish Handles.Discs logic for every rotation axis in this order: Z, X and Y
                                    var euler = new Vector3(property.vector3Value.x % 360f, property.vector3Value.y % 360f, property.vector3Value.z % 360f);
                                    if (euler.x >= 180 && euler.x <= 360) { euler.x -= 360; }
                                    //if(Mathf.Abs(euler.x) > 30) { euler.x = 0; }
                                    else if (euler.x <= -180 && euler.x >= -360) { euler.x += 360; }
                                    if (euler.y >= 180 && euler.y <= 360) { euler.y -= 360; }
                                    //if(Mathf.Abs(euler.y) > 30) { euler.y = 0; }
                                    else if (euler.y <= -180 && euler.y >= -360) { euler.y += 360; }
                                    if (euler.z >= 180 && euler.z <= 360) { euler.z -= 360; }
                                    //if(Mathf.Abs(euler.z) > 30) { euler.z = 0; }
                                    else if (euler.z <= -180 && euler.z >= -360) { euler.z += 360; }
                                    euler = Quaternion.Euler(euler).eulerAngles;
                                    var newEuler = (Handles.RotationHandle(Quaternion.Euler(euler) * StartRotation, Vector3.zero) * Quaternion.Inverse(StartRotation)).eulerAngles;
                                    if (euler != newEuler)
                                    {
                                        //Debug.Log($"Euler: {newEuler} - {euler} = {newEuler - euler}");

                                        euler = newEuler - euler;

                                        if (euler.x >= 180 && euler.x <= 360) { euler.x -= 360; }
                                        //if(Mathf.Abs(euler.x) > 30) { euler.x = 0; }
                                        else if (euler.x <= -180 && euler.x >= -360) { euler.x += 360; }
                                        if (euler.y >= 180 && euler.y <= 360) { euler.y -= 360; }
                                        //if(Mathf.Abs(euler.y) > 30) { euler.y = 0; }
                                        else if (euler.y <= -180 && euler.y >= -360) { euler.y += 360; }
                                        if (euler.z >= 180 && euler.z <= 360) { euler.z -= 360; }
                                        //if(Mathf.Abs(euler.z) > 30) { euler.z = 0; }
                                        else if (euler.z <= -180 && euler.z >= -360) { euler.z += 360; }

                                        property.vector3Value += euler;
                                    }
                                }
                            }
                            else
                            {
                                Debug.LogError($"[Handle Draw]: Property {property.propertyPath} is not suitable for handle {handle.Type}");
                            }
                            break;
                        case HandleType.Scale:
                            Handles.matrix = Matrix4x4.TRS(EndPosition, EndRotation, Vector3.one);
                            if (property.propertyType == SerializedPropertyType.Vector3)
                            {
                                property.vector3Value = Handles.ScaleHandle(property.vector3Value,
                                    Vector3.zero,
                                    Quaternion.identity,
                                    HandleUtility.GetHandleSize(Vector3.zero) * 0.65f);
                            }
                            else
                            {
                                Debug.LogError($"[Handle Draw]: Property {property.propertyPath} is not suitable for handle {handle.Type}");
                            }
                            break;
                        case HandleType.Slider:
                            if(property.propertyType == SerializedPropertyType.Float)
                            {
                                property.floatValue = Handles.ScaleSlider(property.floatValue,
                                    Vector3.zero,
                                    Vector3.right, Quaternion.identity,
                                    HandleUtility.GetHandleSize(parentGameObject.transform.position), 
                                    Handles.SnapValue(property.floatValue, 0.01f));
                            }
                            else if(property.propertyType == SerializedPropertyType.Integer)
                            {
                                property.intValue = (int)Handles.ScaleSlider(property.intValue,
                                    Vector3.zero,
                                    Vector3.right, Quaternion.identity,
                                    HandleUtility.GetHandleSize(parentGameObject.transform.position),
                                    Handles.SnapValue(property.intValue, 1f));
                            }
                            else if(property.propertyType == SerializedPropertyType.Boolean)
                            {
                                property.boolValue = Handles.ScaleSlider(property.boolValue ? 1 : 0,
                                    Vector3.zero,
                                    Vector3.right, Quaternion.identity,
                                    HandleUtility.GetHandleSize(parentGameObject.transform.position),
                                    Handles.SnapValue(property.floatValue, 1f)) > 0.5f;
                            }
                            else
                            {
                                Debug.LogError($"[Handle Draw]: Property {property.propertyPath} is not suitable for handle {handle.Type}");
                            }
                            break;
                        case HandleType.Slider2D:
                            if(property.propertyType == SerializedPropertyType.Vector2)
                            {
                                //property.vector2Value = Handles.sli
                                throw new NotImplementedException();
                            }
                            else
                            {
                                Debug.LogError($"[Handle Draw]: Property {property.propertyPath} is not suitable for handle {handle.Type}");
                            }
                            break;
                    }
                }

                Handles.matrix = handlesMatrix;
            }

            private Vector3 HandlePosition(Vector3 vector3, UseHandleAttribute handle)
            {
                if (handle.Space == HandleSpace.Local || (handle.Space == HandleSpace.Auto && UTools.pivotRotation == PivotRotation.Local))
                {
                    return Handles.PositionHandle(vector3, previewGO.GameObject.transform.rotation);
                }
                else
                {
                    return Handles.PositionHandle(vector3, Quaternion.identity);
                }
            }

            public virtual void Dispose()
            {
                previewGO?.Dispose();
                foreach (var child in children)
                {
                    child.Dispose();
                }
            }

            public static List<PreviewRoot> BuildTrees(List<BaseAnimationBlock> blocks)
            {
                List<PreviewRoot> roots = new List<PreviewRoot>();
                foreach (var group in blocks.Where(b => b is ITargetingObject tbj && b is IPreviewAnimation && tbj.Target)
                                            .OrderBy(b => b.StartTime).GroupBy(b => (b as ITargetingObject).Target.GetGameObject()))
                {
                    BaseAnimationBlock rootBlock = group.First();
                    PreviewNode previewNode = new PreviewRoot(rootBlock);
                    roots.Add(previewNode as PreviewRoot);
                    BaseAnimationBlock[] lastInTrack = new BaseAnimationBlock[AnimationComposer.k_MaxTrackId + 1];
                    lastInTrack[rootBlock.Track] = rootBlock;
                    Dictionary<BaseAnimationBlock, PreviewNode> previewNodes = new Dictionary<BaseAnimationBlock, PreviewNode>();
                    previewNodes[rootBlock] = previewNode;
                    foreach (var block in group)
                    {
                        if (block == rootBlock) { continue; }
                        var sameTrackLast = lastInTrack[block.Track];
                        if (sameTrackLast && previewNode.block.EndTime < sameTrackLast.EndTime)
                        {
                            previewNode = new PreviewNode(previewNodes[sameTrackLast], block);
                        }
                        else
                        {
                            previewNode = new PreviewNode(previewNode, block);
                        }
                        lastInTrack[block.Track] = block;
                        previewNodes[block] = previewNode;
                    }
                }

                return roots;
            }
        }

        #endregion
    }
}
