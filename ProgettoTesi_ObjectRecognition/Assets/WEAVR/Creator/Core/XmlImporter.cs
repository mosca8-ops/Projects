using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TXT.WEAVR.Common;
using TXT.WEAVR.Core;
using TXT.WEAVR.Localization;
using TXT.WEAVR.Xml;
using UnityEditor;
using UnityEngine;


namespace TXT.WEAVR.Procedure
{

    using ConditionDictionary = Dictionary<string, KeyValuePair<int, FlowCondition>>;
    using IndexPair = KeyValuePair<int, FlowCondition>;
    using Object = UnityEngine.Object;

    public class XmlImporter
    {
        private const string k_Error = "[Error]: ";
        private const string k_Process = "[Process]: ";
        private const string k_Info = "[Info]: ";
        private const float repositionWidth = 300;
        private const float repositionHeight = 300;

        public static XmlProcedure Import(string xmlFilepath, Procedure procedure, List<string> messages)
        {
            var xmlProcedure = XmlProcedure.CreateFromXml(xmlFilepath);
            procedure.Guid = xmlProcedure.Guid.ToString();
            
            LocalizationTable table = LocalizationTable.Create(xmlProcedure.AvailableLanguages.Select(l => Language.Create(l)));
            var prevTable = LocalizationManager.Current.Table;
            LocalizationManager.Current.Table = table;
            Import(xmlProcedure, procedure.Graph, messages);
            LocalizationManager.Current.Table = prevTable;

            Object.DestroyImmediate(table);

            return xmlProcedure;
        }

        public static void Import(XmlProcedure xmlProcedure, BaseGraph graph, List<string> messages)
        {
            messages.Add(k_Process + $"------------- [  IMPORTING REFERENCES  ] ------------------");
            // Get the global objects and save them to a dictionary
            ObjectRetriever.Current.Clear();
            Dictionary<string, object> globalObjects = new Dictionary<string, object>();
            foreach (var dataBundleObject in xmlProcedure.DataObjects)
            {
                if (dataBundleObject is AssetObject assetObject)
                {
                    var type = Type.GetType(assetObject.Type);
                    var escapedPath = Path.ChangeExtension(assetObject.RelativePath, "");
                    var asset = Resources.Load(escapedPath.Substring(0, escapedPath.Length - 1), type);
                    globalObjects[assetObject.Id] = asset;
                }
                else if (dataBundleObject is SceneObject sceneObject)
                {
                    var go = ObjectRetriever.GetGameObject(sceneObject.UniqueId, sceneObject.HierarchyPath);
                    if (go)
                    {
                        globalObjects[sceneObject.Id] = go;
                        if (!string.IsNullOrEmpty(sceneObject.ComponentType))
                        {
                            var type = Type.GetType(sceneObject.ComponentType);
                            if (type != null)
                            {
                                globalObjects[sceneObject.Id] = go.GetComponent(type);
                            }
                        }
                    }
                    else
                    {
                        messages.Add(k_Error + $"Unable to find object [{sceneObject.UniqueId} at {sceneObject.HierarchyPath}]");
                    }
                }
            }

            // Create the nodes
            Dictionary<string, GenericNode> stepsBridge = new Dictionary<string, GenericNode>();

            // Prepare the conditions dictionary
            ConditionDictionary conditionsDictionary = new ConditionDictionary();
            GenericNode step = null;

            messages.Add(k_Process + $"------------- [  IMPORTING STEPS  ] ------------------");
            foreach (var xmlStep in xmlProcedure.Steps)
            {
                try
                {
                    step = ProcedureObject.Create<GenericNode>(graph.Procedure);
                    step.Title = xmlStep.Name;
                    step.Number = xmlStep.ID;
                    step.IsMandatory = xmlStep.IsMandatory;
                    step.ShouldPreCheckConditions = xmlStep.IsAr;

                    graph.Add(step);

                    messages.Add(k_Info + $"Created step [{step.Number} - {step.Title}]");
                }
                catch (Exception e)
                {
                    messages.Add(k_Error + $"Unable to create step [{xmlStep?.ID} - {xmlStep?.Name}] -> {e.Message}");
                }

                messages.Add(k_Info + $"------------- STEP [{step.Number} - {step.Title}] -------------");
                // Set the enter actions
                BuildActions(globalObjects, xmlStep, xmlStep.Actions, step, messages);
                // Set conditions
                BuildConditions(globalObjects, conditionsDictionary, xmlStep, step, messages);
                // Set the exit actions
                BuildActions(globalObjects, xmlStep, xmlStep.ExitActions, step, messages);

                stepsBridge[xmlStep.ID] = step;
            }

            // Link all steps
            try
            {
                var startStep = stepsBridge[xmlProcedure.Navigation.FirstStep.ID];
                graph.SetAsOnlyStartNode(startStep);
                messages.Add(k_Info + $"Set start step to [{step.Number} - {step.Title}]");
            }
            catch (Exception e)
            {
                messages.Add(k_Error + $"Unable to set start step [{xmlProcedure.Navigation.FirstStep?.ID} - {xmlProcedure.Navigation.FirstStep?.Name}] -> {e.Message}");
            }

            messages.Add(k_Process + $"------------- [  IMPORTING NAVIGATION  ] ------------------");
            foreach (var link in xmlProcedure.Navigation.StepLinks)
            {
                GenericNode currentStep = null;
                GenericNode nextStep = null;

                try
                {
                    currentStep = stepsBridge[link.CurrentStep.ID];
                    nextStep = stepsBridge[link.NextStep.ID];
                    var condition = conditionsDictionary[link.Condition.ConditionId];
                    var transition = ProcedureObject.Create<LocalTransition>(graph.Procedure);
                    transition.NodeA = currentStep;
                    transition.NodeB = nextStep;
                    condition.Value.Transition = transition;
                    graph.Add(transition);
                    string description = null;
                    try { description = condition.Value.GetDescription(); }
                    catch { }
                    messages.Add(k_Info + $"Linked [{currentStep.Number} - {currentStep.Title}] to [{nextStep.Number} - {nextStep.Title}] with {description}");
                }
                catch (Exception e)
                {
                    messages.Add(k_Error + $"Linking [{link?.CurrentStep?.ID} - {link?.CurrentStep?.Name}] to [{link?.NextStep?.ID} - {link?.NextStep?.Name}] -> {e.Message}");
                }
            }

            try
            {
                // Reposition all steps
                RepositionNodes(new Vector2(500, 500), graph);
            }
            catch(Exception e)
            {
                messages.Add(k_Error + $"Nodes repositioning failed -> {e.Message}");
            }

            messages.Add(k_Process + $"------------- [  IMPORT FINISHED  ] ------------------");
        }

        private static void RepositionNodes(Vector2 startPosition, BaseGraph graph)
        {
            // Arrange as matrix
            int rowSize = (int)Mathf.Floor(Mathf.Sqrt(graph.Nodes.Count));
            float xMax = startPosition.x + repositionWidth * rowSize;
            var nextPosition = startPosition;
            foreach (var node in FlattenNodesBreadthFirst(graph.StartingNodes[0], graph.Nodes, graph.Transitions))
            {
                node.UI_Position = nextPosition;
                nextPosition.x += repositionWidth;
                if (nextPosition.x > xMax)
                {
                    nextPosition.x = startPosition.x;
                    nextPosition.y += repositionHeight;
                }
            }
        }

        public static ICollection<BaseNode> FlattenNodesBreadthFirst(BaseNode startNode, IEnumerable<BaseNode> nodes, IEnumerable<BaseTransition> transitions)
        {
            List<BaseNode> flattenNodes = new List<BaseNode>();
            List<BaseNode> remainingNodes = new List<BaseNode>(nodes);

            remainingNodes.Remove(startNode);
            remainingNodes.Insert(0, startNode);

            int nextIndex = 0;
            while (remainingNodes.Count > 0)
            {
                flattenNodes.Add(remainingNodes[0]);
                remainingNodes.RemoveAt(0);
                while (nextIndex < flattenNodes.Count)
                {
                    foreach (var connection in transitions.Where(t => t.From == flattenNodes[nextIndex]))
                    {
                        var nodeB = connection.To;
                        if (nodeB is BaseNode n && !flattenNodes.Contains(nodeB))
                        {
                            flattenNodes.Add(n);
                            remainingNodes.Remove(n);
                        }
                    }
                    nextIndex++;
                }
            }

            return flattenNodes;
        }

        private static void BuildConditions(Dictionary<string, object> globalObjects,
                                            ConditionDictionary conditionsDictionary,
                                            XmlStep xmlStep,
                                            GenericNode step,
                                            List<string> messages)
        {
            //for (int i = 0; i < xmlStep.ExitConditions.Length; i++) {
            //    step.AddExitCondition();
            //}
            int index = 0;
            FlowCondition exitCondition = null;
            var stepConditions = step.FlowElements.FirstOrDefault(e => e is FlowConditionsContainer) as FlowConditionsContainer;
            try
            {
                if (!stepConditions)
                {
                    stepConditions = ProcedureObject.Create<FlowConditionsContainer>(step.Procedure);
                    step.FlowElements.Add(stepConditions);
                }
            }
            catch (Exception e)
            {
                messages.Add(k_Error + $"Critical issue when adding conditions to [{step.Number} - {step.Title}] -> {e.Message}");
            }

            foreach (var xmlCondition in xmlStep.ExitConditions)
            {
                try
                {
                    exitCondition = ProcedureObject.Create<FlowCondition>(step.Procedure);
                    if (xmlCondition is Xml.ConditionNode conditionNode)
                    {
                        var cNode = ProcedureObject.Create<ConditionAnd>(step.Procedure);
                        foreach (var child in conditionNode.Children)
                        {
                            if (child is Clause clause)
                            {
                                try
                                {
                                    GenericCondition gn = ProcedureObject.Create<GenericCondition>(step.Procedure);
                                    var value = globalObjects[clause.OperandA.Id];
                                    gn.Target = value is Component c ? c.gameObject : value is GameObject go ? go : null;
                                    gn.PropertyPath = clause.OperandA.PropertyPath;
                                    var uObj = PropertyConvert.ToUnityObject(clause.OperandA.Value);
                                    gn.ExpectedValue = uObj ? uObj : Convert(GetTypeFromPath(gn.Target, gn.PropertyPath), clause.OperandA.Value);
                                    gn.Operator = ConvertFromXml(clause.Operation);

                                    cNode.Children.Add(gn);
                                    string gnDescr = null;
                                    try { gnDescr = gn.GetDescription(); } catch { }
                                    messages.Add(k_Info + $"Added clause [{gnDescr}] to condition {index}  of [{step.Number} - {step.Title}]");
                                }
                                catch (Exception e)
                                {
                                    if (exitCondition)
                                    {
                                        stepConditions.ErrorMessage += $"Import failed on condition {index} and clause [{clause.OperandA.PropertyPath} {clause.Operation} {clause.OperandA.Value}]\n";
                                    }
                                    messages.Add(k_Error + $"Unable to add clause [{clause.OperandA.PropertyPath} {clause.Operation} {clause.OperandA.Value}] to condition {index} of [{step.Number} - {step.Title}] -> {e.Message}");
                                }
                            }
                        }

                        exitCondition.Child = cNode;
                    }

                    stepConditions.Conditions.Add(exitCondition);
                    conditionsDictionary[xmlCondition.ConditionId] = new IndexPair(index, exitCondition);
                    string description = null;
                    try
                    {
                        description = exitCondition.GetDescription();
                    }
                    catch { }
                    messages.Add(k_Info + $"Added condition {index} [{description}] to [{step.Number} - {step.Title}]");
                    index++;
                }
                catch (Exception e)
                {
                    if (exitCondition)
                    {
                        stepConditions.ErrorMessage += $"Import failed on condition {index}\n";
                    }
                    messages.Add(k_Error + $"Unable to add exit condition {index} to [{step.Number} - {step.Title}] -> {e.Message}");
                    index++;
                }
            }
        }

        public static ComparisonOperator ConvertFromXml(ClauseOperation op)
        {
            switch (op)
            {
                case ClauseOperation.Equals:
                    return ComparisonOperator.Equals;
                case ClauseOperation.Greater:
                    return ComparisonOperator.GreaterThan;
                case ClauseOperation.GreaterOrEqual:
                    return ComparisonOperator.GreaterThanOrEquals;
                case ClauseOperation.Less:
                    return ComparisonOperator.LessThan;
                case ClauseOperation.LessOrEqual:
                    return ComparisonOperator.LessThanOrEquals;
                case ClauseOperation.NotEquals:
                    return ComparisonOperator.NotEquals;
            }
            return ComparisonOperator.Equals;
        }

        private static void BuildActions(Dictionary<string, object> globalObjects,
                                         XmlStep xmlStep,
                                         IEnumerable<XmlStepAction> xmlActions,
                                         GenericNode step, List<string> messages)
        {
            int index = 0;
            Dictionary<WEAVR.Legacy.ExecutionMode, ExecutionMode> modes = PrepareExecutionModes(step);
            foreach (var xmlAction in xmlActions)
            {
                BaseAction action = null;
                try
                {
                    switch (xmlAction.Type)
                    {
                        case "AnimatorParameterAction":
                            {
                                var a = ProcedureObject.Create<SetAnimatorValueAction>(step.Procedure);
                                action = a;
                                a.Target = globalObjects[xmlAction.Data[0].Id] as Object;
                                a.ParameterName = xmlAction.Data[1].Value;

                                if (a.Target is Animator animator)
                                {
                                    var parameter = animator.parameters.FirstOrDefault(p => p.name == a.ParameterName);

                                    a.ParameterType = parameter.type;

                                    switch (parameter.type)
                                    {
                                        case AnimatorControllerParameterType.Bool:
                                            {
                                                if (bool.TryParse(xmlAction.Data[2].Value, out bool value))
                                                {
                                                    a.BoolValue = value;
                                                }
                                            }
                                            break;
                                        case AnimatorControllerParameterType.Float:
                                            {
                                                if (float.TryParse(xmlAction.Data[2].Value, out float value))
                                                {
                                                    a.FloatValue = value;
                                                }
                                            }
                                            break;
                                        case AnimatorControllerParameterType.Int:
                                            {
                                                if (int.TryParse(xmlAction.Data[2].Value, out int value))
                                                {
                                                    a.IntValue = value;
                                                }
                                            }
                                            break;
                                    }
                                }
                            }
                            break;
                        case "BillboardHideAction":
                            {
                                var a = ProcedureObject.Create<RemoveHighlightAction>(step.Procedure);
                                action = a;
                                a.Target = globalObjects[xmlAction.Data[0].Id] as Object;
                                a.RemoveBillboard = RemoveHighlightAction.BillboardRemoval.All;
                            }
                            break;
                        case "BillboardShowAction":
                            {
                                var a = ProcedureObject.Create<BillboardAction>(step.Procedure);
                                action = a;
                                a.Target = globalObjects[xmlAction.Data[0].Id] as Object;
                                a.ShowBillboard = true;
                                a.Text = LocalizedString.GetFrom(GetDictionaryFrom(xmlAction.Data[1].Value));
                            }
                            break;
                        case "CameraFollowAction":
                            {
                                var a = ProcedureObject.Create<CameraFollowAction>(step.Procedure);
                                action = a;
                                a.Camera = (globalObjects[xmlAction.Data[0].Id] as Object)?.GetGameObject()?.GetComponent<Camera>();
                                a.Target = globalObjects[xmlAction.Data[1].Id] as Object;
                                if(float.TryParse(xmlAction.Data[2].Value, out float floatValue))
                                {
                                    a.Duration = floatValue;
                                }
                                if(bool.TryParse(xmlAction.Data[3].Value, out bool boolValue))
                                {
                                    a.FixedPosition = boolValue;
                                }
                            }
                            break;
                        case "CameraMoveAction":
                            {
                                var a = ProcedureObject.Create<MoveCameraAction>(step.Procedure);
                                action = a;
                                a.Target = globalObjects[xmlAction.Data[0].Id] as Object;
                                a.To = (globalObjects[xmlAction.Data[2].Id] as Object)?.GetGameObject()?.GetComponent<VirtualCamera>();
                                if(!string.IsNullOrEmpty(xmlAction.Data[1].Id))
                                {
                                    a.From = (globalObjects[xmlAction.Data[1].Id] as Object)?.GetGameObject()?.GetComponent<VirtualCamera>();
                                }
                                {
                                    if (float.TryParse(xmlAction.Data[2].Value, out float floatValue))
                                    {
                                        a.Duration = floatValue;
                                    }
                                }
                            }
                            break;
                        case "AllInteractionEnablerAction":
                            {
                                var a = ProcedureObject.Create<GlobalToggleComponentAction>(step.Procedure);
                                action = a;
                                a.ComponentTypename = xmlAction.Data[1].Value;
                                {
                                    if (bool.TryParse(xmlAction.Data[2].Value, out bool boolValue))
                                    {
                                        a.IncludeInactive = boolValue;
                                    }
                                }
                                {
                                    if (bool.TryParse(xmlAction.Data[3].Value, out bool boolValue))
                                    {
                                        a.ShouldEnable = boolValue;
                                    }
                                }
                            }
                            break;
                        case "InteractionEnablerAction":
                            {
                                if (xmlAction.Data[1].Value == typeof(GameObject).Name)
                                {
                                    var a = ProcedureObject.Create<ShowHideObjectAction>(step.Procedure);
                                    action = a;
                                    a.Target = globalObjects[xmlAction.Data[0].Id] as Object;
                                    if (bool.TryParse(xmlAction.Data[2].Value, out bool boolValue))
                                    {
                                        a.Show = boolValue;
                                    }
                                }
                                else
                                {
                                    var a = ProcedureObject.Create<ToggleComponentAction>(step.Procedure);
                                    action = a;
                                    a.Target = globalObjects[xmlAction.Data[0].Id] as Object;
                                    a.Component = xmlAction.Data[1].Value;
                                    if (bool.TryParse(xmlAction.Data[2].Value, out bool boolValue))
                                    {
                                        a.ShouldEnable = boolValue;
                                    }
                                }
                            }
                            break;
                        case "FocalPointAction":
                            {
                                var a = ProcedureObject.Create<ShowHideObjectAction>(step.Procedure);
                                action = a;
                                a.Target = globalObjects[xmlAction.Data[1].Id] as Object;
                                a.Show = true;
                            }
                            break;
                        case "HideFocalPointAction":
                            {
                                var a = ProcedureObject.Create<ShowHideObjectAction>(step.Procedure);
                                action = a;
                                a.Target = globalObjects[xmlAction.Data[0].Id] as Object;
                                a.Show = false;
                            }
                            break;
                        case "ObjectShowAction":
                            {
                                var a = ProcedureObject.Create<ShowHideObjectAction>(step.Procedure);
                                action = a;
                                a.Target = globalObjects[xmlAction.Data[0].Id] as Object;
                                if (bool.TryParse(xmlAction.Data[1].Value, out bool boolValue))
                                {
                                    a.Show = boolValue;
                                }
                            }
                            break;
                        case "OutlineAction":
                            {
                                if (bool.TryParse(xmlAction.Data[1].Value, out bool shouldOutline) && shouldOutline){
                                    var a = ProcedureObject.Create<BillboardAction>(step.Procedure);
                                    action = a;
                                    a.Target = globalObjects[xmlAction.Data[0].Id] as Object;
                                    a.OutlineColor = PropertyConvert.ToColor(xmlAction.Data[2].Value);
                                }
                                else {
                                    var a = ProcedureObject.Create<RemoveHighlightAction>(step.Procedure);
                                    action = a;
                                    a.Target = globalObjects[xmlAction.Data[0].Id] as Object;
                                    a.RemoveBillboard = null;
                                    a.RemoveOutline = true;
                                }
                            }
                            break;
                        case "SoundPlayAction":
                            {
                                var a = ProcedureObject.Create<PlayAudioClipAction>(step.Procedure);
                                action = a;
                                a.Clip = globalObjects[xmlAction.Data[0].Id] as AudioClip;
                                if (float.TryParse(xmlAction.Data[1].Value, out float floatValue))
                                {
                                    a.Volume = floatValue;
                                }
                            }
                            break;
                        case "ReachTargetAction":
                            {
                                var a = ProcedureObject.Create<SetValueAction>(step.Procedure);
                                action = a;

                            }
                            break;
                        case "SetTextAction":
                            {
                                var a = ProcedureObject.Create<SetTextAction>(step.Procedure);
                                action = a;
                                a.Target = globalObjects[xmlAction.Data[0].Id] as Object;
                                a.LocalizedText = LocalizedString.GetFrom(GetDictionaryFrom(xmlAction.Data[1].Value));
                            }
                            break;
                        case "SetValueAction":
                            {
                                var a = ProcedureObject.Create<SetValueAction>(step.Procedure);
                                action = a;
                                a.Target = globalObjects[xmlAction.Data[0].Id] as Object;
                                a.PropertyPath = xmlAction.Data[0].PropertyPath;
                                a.Value = globalObjects.TryGetValue(xmlAction.Data[0].Value, out object uObj) ? uObj 
                                    : Convert(GetTypeFromPath(a.Target, a.PropertyPath), xmlAction.Data[0].Value);
                            }
                            break;
                        case "StopAllAsyncActions":
                            {
                                var a = ProcedureObject.Create<StopAsyncAction>(step.Procedure);
                                action = a;
                            }
                            break;
                        case "TextToSpeechAction":
                            {
                                var a = ProcedureObject.Create<TextToSpeechAction>(step.Procedure);
                                action = a;
                                if (float.TryParse(xmlAction.Data[1].Value, out float floatValue))
                                {
                                    a.Volume = floatValue;
                                }
                                a.Speech = LocalizedTTS.Merge(GetDictionaryFrom(xmlAction.Data[2].Value), GetDictionaryFrom(xmlAction.Data[3].Value));
                                var audioIds = GetAudioIDsDictionaryFrom(xmlAction.Data[0].Value);
                                var localizedClip = new LocalizedAudioClip();
                                localizedClip.UpdateLanguages();
                                foreach(var id in audioIds)
                                {
                                    if(globalObjects.TryGetValue(id.Value.ToString(), out object v) && v is AudioClip clip)
                                    {
                                        localizedClip.SetAudioClip(id.Key, clip);
                                    }
                                }
                                a.LocalizedClip = localizedClip;
                            }
                            break;
                        case "TransparencyAction":
                            {
                                var a = ProcedureObject.Create<RemoveHighlightAction>(step.Procedure);
                                action = a;

                            }
                            break;
                        case "WaitAllAsyncActions":
                            {
                                var a = ProcedureObject.Create<WaitAsyncAction>(step.Procedure);
                                action = a;
                            }
                            break;
                        case "WaitAction":
                            {
                                var a = ProcedureObject.Create<WaitTimeAction>(step.Procedure);
                                action = a;
                                if (float.TryParse(xmlAction.Data[0].Value, out float floatValue))
                                {
                                    a.WaitTime = floatValue;
                                }
                            }
                            break;


                        // ANIMATIONS
                        case "ObjectMoveAction":
                        case "LinearAnimationAction":
                        case "DeltaAnimationAction":
                        case "AlternateAnimationAction":
                        case "ScaleAnimationAction":
                        case "SpiralAnimationAction":
                            {
                                var a = ProcedureObject.Create<AnimationComposer>(step.Procedure);
                                action = a;
                                a.AsyncThread = xmlAction.IsAsync ? -1 : 0;
                                if(xmlAction.Type != "ObjectMoveAction")
                                {
                                    if (bool.TryParse(xmlAction.Data[3].Value, out bool boolValue))
                                    {
                                        a.Loop = boolValue;
                                    }
                                    if (int.TryParse(xmlAction.Data[4].Value, out int intValue))
                                    {
                                        a.LoopCount = intValue <= 0 ? int.MaxValue : intValue;
                                    }
                                }
                                BaseAnimationBlock block = null;
                                switch (xmlAction.Type)
                                {
                                    case "ObjectMoveAction":
                                        {
                                            var b = ProcedureObject.Create<MoveToPointBlock>(step.Procedure);
                                            block = b;
                                            b.Target = globalObjects[xmlAction.Data[0].Id] as Object;
                                            b.Destination = (globalObjects[xmlAction.Data[1].Id] as Object)?.GetGameObject()?.transform;
                                            if (float.TryParse(xmlAction.Data[2].Value, out float floatValue))
                                            {
                                                b.Duration = floatValue != 0 ? floatValue : 1;
                                            }
                                        }
                                        break;
                                    case "DeltaAnimationAction":
                                        {
                                            var b = ProcedureObject.Create<DeltaMoveBlock>(step.Procedure);
                                            block = b;
                                            b.Target = globalObjects[xmlAction.Data[0].Id] as Object;
                                            b.MoveBy = PropertyConvert.ToVector3(xmlAction.Data[5].Value);
                                            b.RotateBy = PropertyConvert.ToVector3(xmlAction.Data[6].Value);
                                            if (float.TryParse(xmlAction.Data[7].Value, out float floatValue))
                                            {
                                                b.Duration = floatValue != 0 ? b.MoveBy.Value.magnitude / floatValue : 1;
                                            }
                                        }
                                        break;
                                    case "LinearAnimationAction":
                                        {
                                            var b = ProcedureObject.Create<MoveToPointBlock>(step.Procedure);
                                            block = b;
                                            b.Duration = 1;
                                            b.Target = globalObjects[xmlAction.Data[0].Id] as Object;
                                            b.Destination = (globalObjects[xmlAction.Data[5].Id] as Object)?.GetGameObject()?.transform;
                                            if (bool.TryParse(xmlAction.Data[6].Value, out bool boolValue))
                                            {
                                                b.WithRotation = boolValue;
                                            }
                                        }
                                        break;
                                    case "AlternateAnimationAction":
                                        {
                                            var b = ProcedureObject.Create<DeltaMoveBlock>(step.Procedure);
                                            block = b;
                                            b.Target = globalObjects[xmlAction.Data[0].Id] as Object;
                                            b.MoveBy = PropertyConvert.ToVector3(xmlAction.Data[5].Value);
                                            if (float.TryParse(xmlAction.Data[6].Value, out float floatValue))
                                            {
                                                b.Duration = floatValue != 0 ? floatValue : 1;
                                            }
                                        }
                                        break;
                                    case "ScaleAnimationAction":
                                        {
                                            var b = ProcedureObject.Create<DeltaMoveBlock>(step.Procedure);
                                            block = b;
                                            b.Target = globalObjects[xmlAction.Data[0].Id] as Object;
                                            b.ScaleBy = PropertyConvert.ToVector3(xmlAction.Data[5].Value);
                                            if (float.TryParse(xmlAction.Data[6].Value, out float floatValue))
                                            {
                                                b.Duration = floatValue != 0 ? b.MoveBy.Value.magnitude / floatValue : 1;
                                            }
                                        }
                                        break;
                                    case "SpiralAnimationAction":
                                        {
                                            var b = ProcedureObject.Create<DeltaMoveBlock>(step.Procedure);
                                            block = b;
                                            b.Target = globalObjects[xmlAction.Data[0].Id] as Object;
                                            b.MoveBy = PropertyConvert.ToVector3(xmlAction.Data[6].Value);
                                            if (float.TryParse(xmlAction.Data[6].Value, out float stepSize))
                                            {
                                                b.RotateBy = b.MoveBy.Value.normalized * stepSize * 360f;
                                            }
                                            if (float.TryParse(xmlAction.Data[7].Value, out float floatValue))
                                            {
                                                b.Duration = floatValue != 0 ? b.MoveBy.Value.magnitude / floatValue : 1;
                                            }
                                        }
                                        break;
                                }

                                a.AnimationBlocks.Add(block);
                            }
                            break;
                    }

                    // General Meta Info
                    action.AsyncThread = xmlAction.IsAsync ? -1 : 0;
                    if (xmlAction.ExecutionMode.HasFlag(WEAVR.Legacy.ExecutionMode.Automatic))
                    {
                        if (modes.TryGetValue(WEAVR.Legacy.ExecutionMode.Automatic, out ExecutionMode mode))
                        {
                            action.ExecutionModes.Add(mode);
                        }
                    }
                    if (xmlAction.ExecutionMode.HasFlag(WEAVR.Legacy.ExecutionMode.Guided))
                    {
                        if (modes.TryGetValue(WEAVR.Legacy.ExecutionMode.Guided, out ExecutionMode mode))
                        {
                            action.ExecutionModes.Add(mode);
                        }
                    }
                    if (xmlAction.ExecutionMode.HasFlag(WEAVR.Legacy.ExecutionMode.Feedback))
                    {
                        if (modes.TryGetValue(WEAVR.Legacy.ExecutionMode.Feedback, out ExecutionMode mode))
                        {
                            action.ExecutionModes.Add(mode);
                        }
                    }

                    step.FlowElements.Add(action);
                    string description = null;
                    try
                    {
                        description = action.GetDescription();
                    }
                    catch { }
                    messages.Add(k_Info + $"Added action {index} [{xmlAction.Type} -> {action.GetType().Name}] [{description}] to [{step.Number} - {step.Title}]");
                    index++;
                }
                catch (Exception e)
                {
                    if (action)
                    {
                        action.ErrorMessage += $"Import failed with {xmlAction.Data.Length} data objects\n";
                    }
                    messages.Add(k_Error + $"Unable to add action {index} of type {xmlAction.Type} to [{step.Number} - {step.Title}] with data [ {DataToString(xmlAction.Data)} ] -> {e.Message}");
                    index++;
                }
            }
        }

        private static DictionaryOfStringAndString GetDictionaryFrom(object data)
        {
            DictionaryOfStringAndString dictionary = null;
            if (data != null && (data as string) != "")
            {
                try
                {
                    dictionary = JsonConvert.DeserializeObject<DictionaryOfStringAndString>(data as string);
                }
                catch (Exception)
                {
                    dictionary = new DictionaryOfStringAndString
                    {
                        { LocalizationManager.Current.CurrentLanguage.Name, data as string }
                    };
                }
            }
            if (dictionary == null)
            {
                dictionary = new DictionaryOfStringAndString();
            }
            return dictionary;
        }

        private static DictionaryOfStringAndInt GetAudioIDsDictionaryFrom(object data)
        {
            DictionaryOfStringAndInt dictionary = null;
            if (data != null && (data as string) != "")
            {
                try
                {
                    dictionary = JsonConvert.DeserializeObject<DictionaryOfStringAndInt>(data as string);
                }
                catch (Exception)
                {
                    dictionary = new DictionaryOfStringAndInt
                    {
                        { LocalizationManager.Current.CurrentLanguage.Name, int.TryParse(data as string, out int val) ? val : data is int i ? i : 0 }
                    };
                }
            }
            if (dictionary == null)
            {
                dictionary = new DictionaryOfStringAndInt();
            }
            return dictionary;
        }

        private static object Convert(Type type, string valueString)
        {
            if(type == null)
            {
                return valueString;
            }
            if (type == typeof(string))
            {
                return valueString;
            }
            else if (type == typeof(float))
            {
                return PropertyConvert.ToFloat(valueString);
            }
            else if (type == typeof(bool))
            {
                return PropertyConvert.ToBoolean(valueString);
            }
            else if (type == typeof(int))
            {
                return PropertyConvert.ToInt(valueString);
            }
            else if (type == typeof(Vector2))
            {
                return PropertyConvert.ToVector2(valueString);
            }
            else if (type == typeof(Vector3))
            {
                return PropertyConvert.ToVector3(valueString);
            }
            else if (type == typeof(Vector4))
            {
                return PropertyConvert.ToVector4(valueString);
            }
            else if (type == typeof(Color))
            {
                return PropertyConvert.ToColor(valueString);
            }
            else if (type.IsEnum())
            {
                return PropertyConvert.ToEnum(valueString, type);
            }

            return valueString;
        }

        private static Editor.PropertyPathField s_propertyPath;
        private static Editor.PropertyPathField PropertyPath
        {
            get
            {
                if(s_propertyPath == null)
                {
                    s_propertyPath = new Editor.PropertyPathField();
                }
                return s_propertyPath;
            }
        }

        private static Type GetTypeFromPath(Object target, string propertyPath)
        {
            PropertyPath.SetPropertyPath(target, propertyPath);
            return s_propertyPath.SelectedProperty?.type;
        }

        private static Dictionary<WEAVR.Legacy.ExecutionMode, ExecutionMode> PrepareExecutionModes(GenericNode step)
        {
            Dictionary<WEAVR.Legacy.ExecutionMode, ExecutionMode> modes = new Dictionary<WEAVR.Legacy.ExecutionMode, ExecutionMode>();
            var execMode = step.Procedure.ExecutionModes.FirstOrDefault(m => m.ModeName.ToLower().StartsWith("automatic"));
            if (execMode)
            {
                modes[WEAVR.Legacy.ExecutionMode.Automatic] = execMode;
            }
            execMode = step.Procedure.ExecutionModes.FirstOrDefault(m => m.ModeName.ToLower().StartsWith("guided"));
            if (execMode)
            {
                modes[WEAVR.Legacy.ExecutionMode.Guided] = execMode;
            }
            execMode = step.Procedure.ExecutionModes.FirstOrDefault(m => m.ModeName.ToLower().StartsWith("feedback"));
            if (execMode)
            {
                modes[WEAVR.Legacy.ExecutionMode.Feedback] = execMode;
            }

            return modes;
        }

        private static string DataToString(ObjectProperty[] data)
        {
            string s = "";
            foreach(var d in data)
            {
                s += $" | {d.Id} - {d.PropertyPath} - {d.Value}";
            }
            return s.Substring(2);
        }

        //private static object[] BuildData(Dictionary<string, object> globalObjects, XmlStepAction xmlAction, List<string> messages)
        //{
        //    if (xmlAction.Data == null) { return new object[0]; }
        //    object[] data = new object[xmlAction.Data.Length];
        //    for (int i = 0; i < data.Length; i++)
        //    {
        //        // Check if object wrapper, or if property or if simple value
        //        var xmlData = xmlAction.Data[i];
        //        bool hasId = !string.IsNullOrEmpty(xmlData.Id);
        //        bool hasProperty = !string.IsNullOrEmpty(xmlData.PropertyPath);
        //        bool hasValue = !string.IsNullOrEmpty(xmlData.Value);

        //        // Get the correct data objects
        //        var propertyWrapper = PropertyValueWrapper.CreateInstance<PropertyValueWrapper>();
        //        Undo.RegisterCreatedObjectUndo(propertyWrapper, "Create property wrapper");

        //        if (hasId && hasProperty)
        //        {
        //            data[i] = propertyWrapper.BuildFrom(globalObjects[xmlData.Id], xmlData.PropertyPath, xmlData.Value) ?
        //                      (object)propertyWrapper : xmlData.PropertyPath;
        //        }
        //        else if (hasId)
        //        {
        //            data[i] = globalObjects[xmlData.Id];
        //        }
        //        else if (hasValue)
        //        {
        //            data[i] = xmlData.Value;
        //        }
        //    }

        //    return data;
        //}
    }
}