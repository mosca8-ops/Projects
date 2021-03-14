
namespace TXT.WEAVR.Xml
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Xml;
    using System.Xml.Serialization;
    using TXT.WEAVR.Core;


    #region [  Common  ]

    /// <summary>
    /// An Xml version of vector
    /// </summary>
    public partial class XmlVector
    {
        public float x;
        public float y;
        public float z;

        public XmlVector(string vectorString)
        {
            x = y = z = 0;
            ConvertFrom(vectorString);
        }

        public XmlVector ConvertFrom(string s)
        {
            if (!string.IsNullOrEmpty(s))
            {
                var splits = s.Split(',', ';');
                float value = 0;
                if (splits.Length > 0 && PropertyConvert.TryParse(splits[0], out value))
                {
                    x = value;
                }
                if (splits.Length > 1 && PropertyConvert.TryParse(splits[1], out value))
                {
                    y = value;
                }
                if (splits.Length > 2 && PropertyConvert.TryParse(splits[2], out value))
                {
                    z = value;
                }
            }

            return this;
        }

        public static implicit operator XmlVector(String vectorString)
        {
            return new XmlVector(vectorString);
        }

        public override string ToString()
        {
            return x.ToString(CultureInfo.InvariantCulture) + ", "
                 + y.ToString(CultureInfo.InvariantCulture) + ", "
                 + z.ToString(CultureInfo.InvariantCulture);
        }
    }

    [XmlRoot(DataType = "transformType")]
    public partial class ObjectTransform
    {
        #region Non XML Properties

        [XmlIgnore]
        public XmlVector Position { get; set; }
        [XmlIgnore]
        public XmlVector Rotation { get; set; }
        [XmlIgnore]
        public XmlVector Scale { get; set; }

        #endregion

        [XmlAttribute("isRelative")]
        public bool IsRelative { get; set; }

        [XmlAttribute("relativeObjectId")]
        public string RelativeObjectId { get; set; }

        [XmlAttribute("position")]
        public string PositionString {
            get { return Position == null ? null : Position.ToString(); }
            set { Position = Position == null ? new XmlVector(value) : Position.ConvertFrom(value); }
        }

        [XmlAttribute("rotation")]
        public string RotationString {
            get { return Rotation == null ? null : Rotation.ToString(); }
            set { Rotation = Rotation == null ? new XmlVector(value) : Rotation.ConvertFrom(value); }
        }

        [XmlAttribute("scale")]
        public string ScaleString {
            get { return Scale == null ? null : Scale.ToString(); }
            set { Scale = Scale == null ? new XmlVector(value) : Scale.ConvertFrom(value); }
        }
    }

    [XmlRoot(DataType = "objectId")]
    public partial class ObjectID
    {
        [XmlAttribute(AttributeName = "id")]
        public string ID { get; set; }

        [XmlAttribute(AttributeName = "guid")]
        public Guid Guid { get; set; }

        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; }

        [XmlIgnore]
        public bool IsFrozen { get; private set; }

        private int _hashValue;

        public ObjectID()
        {
            _hashValue = GetHashCode();
        }

        /// <summary>
        /// Creates an identical copy.
        /// </summary>
        /// <remarks>Useful for serialization when only the id and name 
        /// are required from derived classes</remarks>
        /// <returns>A copy of <see cref="ObjectID"/> type with same Id and Name</returns>
        public ObjectID GetID()
        {
            return new ObjectID()
            {
                ID = ID,
                Name = Name,
                IsFrozen = IsFrozen,
                Guid = Guid,
                _hashValue = _hashValue
            };
        }

        /// <summary>
        /// Freezes the hash code of this object
        /// </summary>
        /// <returns>This instance to allow call chaining</returns>
        public virtual ObjectID Freeze()
        {
            if (!IsFrozen)
            {
                _hashValue = (Name + "___" + ID).GetHashCode();
                IsFrozen = true;
            }
            return this;
        }

        public override bool Equals(object obj)
        {
            return obj != null && obj.GetType() == GetType()
                && obj.GetHashCode() == GetHashCode();
        }

        public override int GetHashCode()
        {
            return _hashValue;
        }
    }


    #endregion

    #region [  Data Related  ]

    [XmlRoot(DataType = "dataBundle")]
    public partial class DataBundle : ObjectID
    {
        [XmlAttribute(AttributeName = "localPath")]
        public virtual string LocalPath { get; set; }

        [XmlAttribute(AttributeName = "version")]
        public virtual string Version { get; set; }
    }

    [XmlRoot(DataType = "dataBundleObject")]
    public abstract partial class DataBundleObject
    {
        [XmlAttribute("id")]
        public virtual string Id { get; set; }

        [XmlAttribute("dataBundleId")]
        public virtual string DataBundleId { get; set; }
    }

    [XmlRoot(DataType = "sceneObject")]
    public partial class SceneObject : DataBundleObject
    {
        [XmlAttribute("uniqueId")]
        public virtual string UniqueId { get; set; }

        [XmlAttribute("hierarchyPath")]
        public virtual string HierarchyPath { get; set; }

        [XmlAttribute("componentType")]
        public virtual string ComponentType { get; set; }

        [XmlElement(ElementName = "transform")]
        public virtual ObjectTransform Transform { get; set; }
    }

    [XmlRoot(DataType = "assetObject")]
    public partial class AssetObject : DataBundleObject
    {
        [XmlAttribute("name")]
        public virtual string Name { get; set; }

        [XmlAttribute("type")]
        public virtual string Type { get; set; }

        [XmlAttribute("relativePath")]
        public virtual string RelativePath { get; set; }
    }

    [XmlRoot(DataType = "property")]
    public partial class ObjectProperty
    {
        [XmlAttribute("id")]
        public virtual string Id { get; set; }

        [XmlAttribute("propertyPath")]
        public virtual string PropertyPath { get; set; }

        [XmlAttribute("value")]
        public virtual string Value { get; set; }
    }

    #endregion

    #region [  Conditions  ]

    [XmlType(IncludeInSchema = false)]
    public enum ConditionType
    {
        or,
        and,
        not,
        clause,
        alwaysTrue,
        existingCondition
    }

    [XmlRoot(ElementName = "condition")]
    public abstract partial class Condition
    {
        [XmlAttribute("conditionId")]
        public virtual string ConditionId { get; set; }

        [XmlIgnore]
        public abstract ConditionType Type { get; }
    }

    public enum ClauseOperation
    {
        NotEquals,
        Equals,
        Less,
        Greater,
        LessOrEqual,
        GreaterOrEqual,
        Same
    }

    [XmlRoot(ElementName = "clause")]
    public partial class Clause : Condition
    {
        [XmlElement("operandA")]
        public ObjectProperty OperandA { get; set; }

        [XmlElement("operandB")]
        public ObjectProperty OperandB { get; set; }

        [XmlAttribute("operation")]
        public string OperationAttribute {
            get { return ToCamelCase(Operation.ToString()); }
            set { Operation = (ClauseOperation)Enum.Parse(typeof(ClauseOperation), value, true); }
        }

        [XmlIgnore]
        public ClauseOperation Operation { get; set; }

        public override ConditionType Type { get { return ConditionType.clause; } }

        private static string ToCamelCase(string str)
        {
            return str.Substring(0, 1).ToLower() + str.Substring(1);
        }
    }

    public abstract partial class ConditionNode : Condition
    {
        [XmlChoiceIdentifier("ChildrenTypes")]
        [XmlElement("or", typeof(ConditionOr))]
        [XmlElement("and", typeof(ConditionAnd))]
        [XmlElement("not", typeof(ConditionNot))]
        [XmlElement("clause", typeof(Clause))]
        [XmlElement("alwaysTrue", typeof(ConditionAlwaysTrue))]
        [XmlElement("existingCondition", typeof(LinkCondition))]
        public Condition[] Children { get; set; }

        [XmlIgnore]
        public ConditionType[] ChildrenTypes { get; set; }
    }

    [XmlRoot(ElementName = "alwaysTrue")]
    public partial class ConditionAlwaysTrue : Condition
    {
        public override ConditionType Type { get { return ConditionType.alwaysTrue; } }

        /// <summary>
        /// Makes a copy of this condition with a new id
        /// </summary>
        /// <param name="idSuffix">The suffix to Id</param>
        /// <returns>The new <see cref="ConditionAlwaysTrue"/></returns>
        public ConditionAlwaysTrue MakeCopy(string idSuffix)
        {
            return new ConditionAlwaysTrue() { ConditionId = ConditionId + idSuffix };
        }
    }

    [XmlRoot(ElementName = "or")]
    public partial class ConditionOr : ConditionNode
    {
        public override ConditionType Type { get { return ConditionType.or; } }
    }

    [XmlRoot(ElementName = "and")]
    public partial class ConditionAnd : ConditionNode
    {
        public override ConditionType Type { get { return ConditionType.and; } }
    }

    [XmlRoot(ElementName = "not")]
    public partial class ConditionNot : Condition
    {
        [XmlChoiceIdentifier("ChildType")]
        [XmlElement("or", typeof(ConditionOr))]
        [XmlElement("and", typeof(ConditionAnd))]
        [XmlElement("not", typeof(ConditionNot))]
        [XmlElement("clause", typeof(Clause))]
        [XmlElement("alwaysTrue", typeof(ConditionAlwaysTrue))]
        [XmlElement("existingCondition", typeof(LinkCondition))]
        public Condition Child { get; set; }

        [XmlIgnore]
        public ConditionType ChildType { get; set; }

        public override ConditionType Type { get { return ConditionType.not; } }
    }

    [XmlRoot(ElementName = "existingCondition")]
    public partial class LinkCondition : Condition
    {
        public override ConditionType Type { get { return ConditionType.existingCondition; } }
    }

    #endregion

    #region [  Virtual Screen  ]

    [XmlType(IncludeInSchema = false)]
    public enum XmlVirtualScreenType
    {
        templateScreen,
        simpleScreen,
        videoScreen,
        screenReference
    }

    public abstract partial class XmlVirtualScreen : ObjectID
    {

        [XmlIgnore]
        public abstract XmlVirtualScreenType Type { get; }
    }

    public abstract partial class XmlSimpleScreenAbstract : XmlVirtualScreen
    {
        [XmlIgnore]
        public int? DisplayId { get; set; }

        [XmlAttribute(AttributeName = "displayId")]
        public string DisplayIdText {
            get { return DisplayId.HasValue ? DisplayId.ToString() : null; }
            set {
                int parsed;
                DisplayId = int.TryParse(value, out parsed) ? parsed : default(int?);
            }
        }

        [XmlAttribute(AttributeName = "title")]
        public virtual string Title { get; set; }
    }


    [XmlRoot(ElementName = "templateScreen")]
    public partial class TemplateScreen : XmlSimpleScreenAbstract
    {
        [XmlElement(ElementName = "template")]
        public virtual XmlTemplate Template { get; set; }

        [XmlIgnore]
        public override XmlVirtualScreenType Type { get { return XmlVirtualScreenType.templateScreen; } }
    }

    [XmlRoot(ElementName = "simpleScreen")]
    public partial class SimpleScreen : XmlSimpleScreenAbstract
    {
        [XmlElement(ElementName = "picture")]
        public virtual AssetObject[] Pictures { get; set; }

        [XmlElement(ElementName = "audio")]
        public virtual AssetObject Audio { get; set; }

        [XmlElement(ElementName = "text")]
        public virtual string[] Texts { get; set; }

        [XmlIgnore]
        public override XmlVirtualScreenType Type { get { return XmlVirtualScreenType.simpleScreen; } }
    }

    [XmlRoot(ElementName = "videoScreen")]
    public partial class VideoScreen : XmlSimpleScreenAbstract
    {
        [XmlElement(ElementName = "video")]
        public virtual AssetObject Video { get; set; }

        [XmlElement(ElementName = "audio")]
        public virtual AssetObject Audio { get; set; }

        [XmlElement(ElementName = "text")]
        public virtual string[] Texts { get; set; }

        [XmlIgnore]
        public override XmlVirtualScreenType Type { get { return XmlVirtualScreenType.videoScreen; } }
    }

    [XmlRoot(ElementName = "screenReference")]
    public partial class ScreenReference : XmlVirtualScreen
    {
        [XmlAttribute(AttributeName = "referenceId")]
        public virtual string ReferenceId { get; set; }

        [XmlIgnore]
        public override XmlVirtualScreenType Type { get { return XmlVirtualScreenType.screenReference; } }
    }

    [XmlRoot(DataType = "virtualScreenLink")]
    public partial class VirtualScreenLink
    {
        [XmlAttribute(AttributeName = "linkName")]
        public virtual string LinkName { get; set; }

        [XmlAttribute(AttributeName = "currentScreenId")]
        public virtual string CurrentScreenId { get; set; }

        [XmlAttribute(AttributeName = "nextScreenId")]
        public virtual string NextScreenId { get; set; }

        [XmlAttribute(AttributeName = "oneWay")]
        public virtual bool OneWay { get; set; }
    }

    [XmlRoot(DataType = "virtualScreensPack")]
    public partial class VirtualScreensPack
    {
        [XmlArray("screens")]
        [XmlArrayItem("templateScreen", typeof(TemplateScreen))]
        [XmlArrayItem("simpleScreen", typeof(SimpleScreen))]
        [XmlArrayItem("videoScreen", typeof(VideoScreen))]
        [XmlArrayItem("screenReference", typeof(ScreenReference))]
        public virtual XmlVirtualScreen[] Screens { get; set; }

        [XmlIgnore]
        public virtual XmlVirtualScreenType[] ScreenTypes { get; set; }


        [XmlArray(ElementName = "links")]
        [XmlArrayItem("link")]
        public virtual VirtualScreenLink[] Links { get; set; }
    }

    #endregion

    #region [  Help  ]

    [XmlRoot(ElementName = "help")]
    public partial class Help
    {
        [XmlArray("AR")]
        [XmlArrayItem("data")]
        public SceneObject[] DataAR { get; set; }

        [XmlElement("virtualScreens")]
        public VirtualScreensPack VirtualScreens { get; set; }
    }

    #endregion

    #region [  Step  ]

    [XmlRoot(DataType = "stepAction")]
    public partial class XmlStepAction
    {
        [XmlElement("data")]
        public virtual ObjectProperty[] Data { get; set; }

        [XmlAttribute(AttributeName = "type")]
        public virtual string Type { get; set; }

        [XmlAttribute(AttributeName = "executionMode")]
        public virtual string ExecutionModeString { get; set; }

        [XmlAttribute(AttributeName = "isAsync")]
        public virtual bool IsAsync { get; set; }
    }

    [XmlRoot(ElementName = "step")]
    public partial class XmlStep : ObjectID
    {
        [XmlAttribute(AttributeName = "isMandatory")]
        public virtual bool IsMandatory { get; set; }

        [XmlAttribute(AttributeName = "isAr")]
        public virtual bool IsAr { get; set; }

        [XmlElement("help")]
        public virtual Help Help { get; set; }

        [XmlArray("enterActions")]
        [XmlArrayItem("action")]
        public virtual XmlStepAction[] Actions { get; set; }

        [XmlArray("exitConditions")]
        [XmlArrayItem("or", typeof(ConditionOr))]
        [XmlArrayItem("and", typeof(ConditionAnd))]
        [XmlArrayItem("not", typeof(ConditionNot))]
        [XmlArrayItem("alwaysTrue", typeof(ConditionAlwaysTrue))]
        [XmlArrayItem("clause", typeof(Clause))]
        public virtual Condition[] ExitConditions { get; set; }

        [XmlIgnore]
        public virtual ConditionType[] ConditionTypes { get; set; }

        [XmlArray("exitActions")]
        [XmlArrayItem("action")]
        public virtual XmlStepAction[] ExitActions { get; set; }

        [XmlIgnore]
        public XmlSuperStep SuperStep { get; set; }

        public override string ToString()
        {
            return ID.ToString();
        }
    }

    #endregion

    #region [  SUPER STEP  ]

    [XmlRoot(ElementName = "superStep")]
    public partial class XmlSuperStep : ObjectID
    {
        [XmlAttribute(AttributeName = "description")]
        public virtual string Description { get; set; }

        [XmlArray("steps")]
        [XmlArrayItem("step")]
        public virtual XmlStep[] Steps { get; set; }

        public XmlSuperStep() : base() { }

        public XmlSuperStep(XmlStep step) : base()
        {
            Steps = new XmlStep[] { step };
            Name = step.Name;
            ID = step.ID;
            Guid = Guid;
        }

        public override ObjectID Freeze()
        {
            foreach (var step in Steps)
            {
                step.Freeze();
            }
            return base.Freeze();
        }

        public override string ToString()
        {
            return ID.ToString();
        }
    }

    #endregion

    #region [  Navigation  ]

    [XmlRoot(ElementName = "stepLink")]
    public partial class XmlStepLink
    {
        [XmlElement("currentStep")]
        public ObjectID CurrentStep { get; set; }

        [XmlElement("nextStep")]
        public ObjectID NextStep { get; set; }

        [XmlChoiceIdentifier("ConditionType")]
        [XmlElement("or", typeof(ConditionOr))]
        [XmlElement("and", typeof(ConditionAnd))]
        [XmlElement("not", typeof(ConditionNot))]
        [XmlElement("clause", typeof(Clause))]
        [XmlElement("alwaysTrue", typeof(ConditionAlwaysTrue))]
        [XmlElement("existingCondition", typeof(LinkCondition))]
        public Condition Condition { get; set; }

        [XmlIgnore]
        public ConditionType ConditionType { get; set; }
    }

    [XmlRoot(ElementName = "navigation")]
    public partial class Navigation
    {
        [XmlElement("firstStep")]
        public ObjectID FirstStep { get; set; }

        [XmlArray("stepLinks")]
        [XmlArrayItem("stepLink")]
        public XmlStepLink[] StepLinks { get; set; }
    }

    #endregion

    [XmlRoot(ElementName = "procedure")]
    public partial class XmlProcedure : ObjectID
    {

        public const int kCurrentXMLSchemaVersion = 1;

        [XmlAttribute(AttributeName = "sceneName")]
        public string SceneName { get; set; }

        [XmlAttribute(AttributeName = "sceneGuid")]
        public string SceneGuid { get; set; }

        [XmlAttribute(AttributeName = "availableLanguages")]
        public string[] AvailableLanguages { get; set; }

        [XmlAttribute(AttributeName = "fallbackLanguage")]
        public string FallbackLanguage { get; set; }

        [XmlAttribute(AttributeName = "versionGuid")]
        public Guid VersionGuid { get; set; }

        [XmlIgnore]
        public int SchemaVersion { get; set; }

        [XmlAttribute(AttributeName = "schemaVersion")]
        public string SchemaVersionString {
            get { return SchemaVersion.ToString(); }
            set { SchemaVersion = string.IsNullOrEmpty(value) ? 0 : int.Parse(value); }
        }

        [XmlAttribute(AttributeName = "text")]
        public virtual string Text { get; set; }

        [XmlArray("dataBundles")]
        [XmlArrayItem("data")]
        public virtual DataBundle[] Data { get; set; }

        [XmlArray("dataObjects")]
        [XmlArrayItem("object", typeof(SceneObject))]
        [XmlArrayItem("asset", typeof(AssetObject))]
        public virtual DataBundleObject[] DataObjects { get; set; }

        [XmlElement("virtualScreens")]
        public virtual VirtualScreensPack[] VirtualScreens { get; set; }

        [XmlArray("steps")]
        [XmlArrayItem("step")]
        // TODO: Remove this for next xml version
        //[Obsolete("Use SuperSteps instead", false)]
        public virtual XmlStep[] Steps { get; set; }

        [XmlArray("superSteps")]
        [XmlArrayItem("superStep")]
        public virtual XmlSuperStep[] SuperSteps { get; set; }

        [XmlElement("navigation")]
        public virtual Navigation Navigation { get; set; }

        partial void OnXmlLoaded();

        /// <summary>
        /// Freezes objects ids hash codes, this way the elements can be matched easily
        /// </summary>
        /// <returns>The same procedure objects, but with hash codes frozen</returns>
        public override ObjectID Freeze()
        {
            base.Freeze();
            if (Data != null)
            {
                foreach (var dataBundle in Data)
                {
                    dataBundle.Freeze();
                }
            }
            if (VirtualScreens != null)
            {
                foreach (var virtualScreenPack in VirtualScreens)
                {
                    foreach (var virtualScreen in virtualScreenPack.Screens)
                    {
                        virtualScreen.Freeze();
                    }
                }
            }
            if (Steps != null)
            {
                foreach (var step in Steps)
                {
                    step.Freeze();
                }
            }
            if (SuperSteps != null)
            {
                foreach (var superStep in SuperSteps)
                {
                    superStep.Freeze();
                }
            }
            Navigation.FirstStep.Freeze();
            foreach (var stepLink in Navigation.StepLinks)
            {
                stepLink.CurrentStep.Freeze();
                stepLink.NextStep.Freeze();
            }

            OnXmlLoaded();

            return this;
        }

        // It is quite slow, so use just one instance for all parsers 
        // (in unity it shouldn't have race issues since it is single threaded)
        private static XmlSerializer s_xmlSerializer;

        /// <summary>
        /// Write the xml representation of this Template into a string writer
        /// </summary>
        /// <param name="stream">[Optional] The stream writer where to write</param>
        /// <returns>The xml as string</returns>
        public string SerializeToXml(StringWriter stream = null)
        {
            if (s_xmlSerializer == null)
            {
                s_xmlSerializer = new XmlSerializer(typeof(XmlProcedure));
            }
            bool volatileStream = stream == null;
            if (volatileStream)
            {
                stream = new Utf8StringWriter();
            }
            try
            {
                SchemaVersion = kCurrentXMLSchemaVersion;
                s_xmlSerializer.Serialize(stream, this);
                return stream.ToString();
            }
            finally
            {
                if (volatileStream)
                {
                    //stream.Close();
                }
            }
        }

        /// <summary>
        /// Tries to create the template from an xml file
        /// </summary>
        /// <param name="filePath">The path to the xml file</param>
        /// <returns>The template representation from the xml</returns>
        /// <exception cref="FileNotFoundException"></exception>
        public static XmlProcedure CreateFromXml(string filePath)
        {
            using (var stream = new StringReader(File.ReadAllText(filePath)))
            {
                return CreateFromXml(stream);
            }
        }

        /// <summary>
        /// Tries to parse a procedure from an xml string
        /// </summary>
        /// <param name="xml">The string with xml</param>
        /// <returns>The procedure representation from the xml</returns>
        public static XmlProcedure CreateFromXmlString(string xml)
        {
            using (var stream = new StringReader(xml))
            {
                return CreateFromXml(stream);
            }
        }

        /// <summary>
        /// Tries to create a template from a string reader
        /// </summary>
        /// <param name="reader">The string reader to get the xml from</param>
        /// <returns>The template representation from xml string reader</returns>
        public static XmlProcedure CreateFromXml(StringReader reader)
        {
            if (s_xmlSerializer == null)
            {
                s_xmlSerializer = new XmlSerializer(typeof(XmlProcedure));
            }
            var result = s_xmlSerializer.Deserialize(reader) as XmlProcedure;
            return result == null ? null : result.Freeze() as XmlProcedure;
        }
    }

    /// <summary>
    /// Unicode (UTF8) version of StringWriter
    /// </summary>
    public class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding { get { return Encoding.UTF8; } }
    }
}
