
namespace TXT.WEAVR.Xml
{
    using System.IO;
    using System.Text;
    using System.Xml;
    using System.Xml.Serialization;
    using TXT.WEAVR.Core;

    /// <summary>
    /// The base element for all xml elements under (and including) sections
    /// </summary>
    public abstract partial class XmlBaseElement
    {
        /// <summary>
        /// The Id of the element
        /// </summary>
        [XmlAttribute(AttributeName = "id")]
        public string ID { get; set; }

        /// <summary>
        /// The row position in the immediate parent
        /// </summary>
        #region Optional Attributes
        [XmlIgnore]
        public int? Row { get; set; }
        [XmlAttribute(AttributeName = "row")]
        public string RowText {
            get { return Row.HasValue ? Row.ToString() : null; }
            set {
                int parsed;
                Row = int.TryParse(value, out parsed) ? parsed : default(int?);
            }
        }

        /// <summary>
        /// The column position in the immediate parent
        /// </summary>
        [XmlIgnore]
        public int? Column { get; set; }
        [XmlAttribute(AttributeName = "column")]
        public string ColumnText {
            get { return Column.HasValue ? Column.ToString() : null; }
            set {
                int parsed;
                Column = int.TryParse(value, out parsed) ? parsed : default(int?);
            }
        }

        /// <summary>
        /// The element span in rows (Read: height in rows)
        /// </summary>
        [XmlIgnore]
        public int? RowSpan { get; set; }
        [XmlAttribute(AttributeName = "rowSpan")]
        public string RowSpanText {
            get { return RowSpan.HasValue ? RowSpan.ToString() : null; }
            set {
                int parsed;
                RowSpan = int.TryParse(value, out parsed) ? parsed : default(int?);
            }
        }

        /// <summary>
        /// The element span in columns (Read: width in columns)
        /// </summary>
        [XmlIgnore]
        public int? ColumnSpan { get; set; }
        [XmlAttribute(AttributeName = "columnSpan")]
        public string ColumnSpanText {
            get { return ColumnSpan.HasValue ? ColumnSpan.ToString() : null; }
            set {
                int parsed;
                ColumnSpan = int.TryParse(value, out parsed) ? parsed : default(int?);
            }
        }
        #endregion
    }

    [XmlType(IncludeInSchema = false)]
    public enum XmlElementType
    {
        none,
        text,
        image,
        button,
        input,
        section
    }

    [XmlType(IncludeInSchema = false)]
    public enum XmlTextType
    {
        simple,
        p,
        ol,
        ul,
        list
    }

    [XmlRoot(ElementName = "list")]
    public partial class XmlSimpleList : XmlTextElement
    {
        private string _finalText;
        private string _indent;
        private int _indentLevel;
        protected StringBuilder _fullTextBuilder;         // StringBuilder do not trigger GarbageCollection
        protected string _originalText;

        private void Indent(int level)
        {
            _indentLevel = level;
            _indent = "".PadLeft(_indentLevel * 4);     // Four spaces for now for the indent
            _finalText = null;                          // Trigger the text update
        }

        public XmlSimpleList()
        {
            _fullTextBuilder = new StringBuilder();
        }

        [XmlElement(ElementName = "li")]
        public XmlTextElement[] Elements { get; set; }

        [XmlText]
        public override string Text {
            get {
                return _originalText;
            }
            set {
                _originalText = value;
                _finalText = null;  // Reset the final text
            }
        }

        [XmlIgnore]
        public override string FullText {
            get {
                if (_fullTextBuilder.Length == 0 || _finalText == null)
                {
                    _finalText = UpdateText();
                }
                return _finalText;
            }
        }

        protected string UpdateText()
        {
            _fullTextBuilder.Length = 0;
            int elementIndex = 0;
            if (Elements != null)
            {
                foreach (var textElement in Elements)
                {
                    if (textElement is XmlSimpleList)
                    {
                        (textElement as XmlSimpleList).Indent(_indentLevel + 1);
                    }
                    _fullTextBuilder.Append(_indent)
                                  .Append(FormatText(_indentLevel, ++elementIndex, textElement.FullText))
                                  .Append("\n");
                }
            }
            return _fullTextBuilder.ToString();
        }

        protected virtual string FormatText(int indentLevel, int elementIndex, string elementText)
        {
            return elementText;
        }

        public override string ToString()
        {
            return FullText;
        }
    }

    [XmlRoot(ElementName = "ul")]
    public partial class XmlUnorderedList : XmlSimpleList
    {
        protected override string FormatText(int indentLevel, int elementIndex, string elementText)
        {
            return "• " + elementText;
        }
    }

    [XmlRoot(ElementName = "ol")]
    public partial class XmlOrderedList : XmlSimpleList
    {
        protected override string FormatText(int indentLevel, int elementIndex, string elementText)
        {
            return elementIndex.ToString() + elementText;
        }
    }

    public partial class XmlTextElement
    {
        [XmlText]
        public virtual string Text { get; set; }

        [XmlIgnore]
        public virtual string FullText { get { return Text; } set { Text = value; } }

        public object OptionalData { get; set; }

        public override string ToString()
        {
            return Text;
        }

        #region Optional Attributes
        [XmlIgnore]
        public bool? IsBold { get; set; }
        [XmlAttribute(AttributeName = "bold")]
        public string IsBoldText {
            get { return IsBold.HasValue ? IsBold.ToString() : null; }
            set {
                bool parsed;
                IsBold = bool.TryParse(value, out parsed) ? parsed : default(bool?);
            }
        }

        [XmlIgnore]
        public bool? IsItalic { get; set; }
        [XmlAttribute(AttributeName = "italic")]
        public string IsItalicText {
            get { return IsItalic.HasValue ? IsItalic.ToString() : null; }
            set {
                bool parsed;
                IsItalic = bool.TryParse(value, out parsed) ? parsed : default(bool?);
            }
        }

        [XmlIgnore]
        public bool? IsUnderlined { get; set; }
        [XmlAttribute(AttributeName = "underline")]
        public string IsUnderlinedText {
            get { return IsUnderlined.HasValue ? IsUnderlined.ToString() : null; }
            set {
                bool parsed;
                IsUnderlined = bool.TryParse(value, out parsed) ? parsed : default(bool?);
            }
        }
        #endregion

        [XmlAttribute(AttributeName = "color")]
        public string Color { get; set; }
    }

    [XmlRoot(ElementName = "text")]
    public partial class XmlTextContainer : XmlBaseElement
    {
        protected StringBuilder _fullTextBuilder;         // StringBuilder do not trigger GarbageCollection
        protected string _originalText;
        protected string _finalText;

        public XmlTextContainer()
        {
            _fullTextBuilder = new StringBuilder();
        }

        [XmlText]
        public virtual string Text {
            get {
                return _originalText;
            }
            set {
                _originalText = value;
                _fullTextBuilder.Length = 0;
            }
        }

        [XmlIgnore]
        public virtual string FullText {
            get {
                if (_fullTextBuilder.Length == 0 || _finalText == null)
                {
                    UpdateTextBuilder(_originalText);
                    _finalText = _fullTextBuilder.ToString();
                }
                return _finalText;
            }
        }

        private void UpdateTextBuilder(string value)
        {
            _fullTextBuilder.Length = 0;
            _fullTextBuilder.Append(value);
            if (Elements != null)
            {
                foreach (var elem in Elements)
                {
                    _fullTextBuilder.Append("\n").Append(elem.FullText);
                }
                if (_fullTextBuilder[0] == '\n') { _fullTextBuilder.Remove(0, 1); }
            }
        }

        [XmlChoiceIdentifier("ChildTypes")]
        [XmlElement("p", typeof(XmlTextElement))]
        [XmlElement("list", typeof(XmlSimpleList))]
        [XmlElement("ul", typeof(XmlUnorderedList))]
        [XmlElement("ol", typeof(XmlOrderedList))]
        public XmlTextElement[] Elements { get; set; }

        [XmlIgnore]
        public XmlTextType[] ChildTypes { get; set; }

        public override string ToString()
        {
            return FullText;
        }
    }

    [XmlRoot(ElementName = "image")]
    public partial class XmlImage : XmlBaseElement
    {
        [XmlAttribute(AttributeName = "imageId")]
        public string ImageId { get; set; }

        #region Optional Attributes
        [XmlIgnore]
        public float? Width { get; set; }
        [XmlAttribute(AttributeName = "width")]
        public string WidthText {
            get { return Width.HasValue ? Width.ToString() : null; }
            set {
                float parsed;
                Width = PropertyConvert.TryParse(value, out parsed) ? parsed : default(float?);
            }
        }

        [XmlIgnore]
        public float? Height { get; set; }
        [XmlAttribute(AttributeName = "height")]
        public string HeightText {
            get { return Height.HasValue ? Height.ToString() : null; }
            set {
                float parsed;
                Height = PropertyConvert.TryParse(value, out parsed) ? parsed : default(float?);
            }
        }
        #endregion

        public override string ToString()
        {
            if (Width.HasValue && Height.HasValue)
            {
                return "[Img='" + ImageId + "' (" + Width + " x " + Height + ")]";
            }
            return "[Img='" + ImageId + "']";
        }
    }

    [XmlRoot(ElementName = "section")]
    public partial class XmlSection : XmlBaseElement
    {
        #region Optional Attributes
        [XmlIgnore]
        public int? Rows { get; set; }
        [XmlAttribute(AttributeName = "rows")]
        public string RowsText {
            get { return Rows.HasValue ? Rows.ToString() : null; }
            set {
                int parsed;
                Rows = int.TryParse(value, out parsed) ? parsed : default(int?);
            }
        }

        [XmlIgnore]
        public int? Columns { get; set; }
        [XmlAttribute(AttributeName = "columns")]
        public string ColumnsText {
            get { return Columns.HasValue ? Columns.ToString() : null; }
            set {
                int parsed;
                Columns = int.TryParse(value, out parsed) ? parsed : default(int?);
            }
        }
        #endregion

        [XmlChoiceIdentifier("ElementTypes")]
        [XmlElement("text", typeof(XmlTextContainer))]
        [XmlElement("image", typeof(XmlImage))]
        [XmlElement("button", typeof(XmlButton))]
        [XmlElement("section", typeof(XmlSection))]
        [XmlElement("input", typeof(XmlInput))]
        public XmlBaseElement[] Elements { get; set; }

        [XmlIgnore]
        public XmlElementType[] ElementTypes { get; set; }

        public override string ToString()
        {
            return ToString("");
        }

        private string ToString(string indent)
        {
            StringBuilder builder = new StringBuilder();
            foreach (var elem in Elements)
            {
                builder.Append(indent);
                if (elem is XmlSection)
                {
                    builder.Append((elem as XmlSection).ToString(indent + "  "));
                }
                else
                {
                    builder.Append(elem.ToString());
                }
                builder.Append("\n");
            }
            return builder.ToString();
        }
    }

    [XmlRoot(ElementName = "button")]
    public partial class XmlButton : XmlBaseElement
    {
        [XmlAttribute(AttributeName = "imageId")]
        public string ImageId { get; set; }

        [XmlText]
        public string Text { get; set; }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(ImageId))
            {
                return Text;
            }
            return "[Img=" + ImageId + "] " + Text;
        }
    }

    [XmlRoot(ElementName = "input")]
    public partial class XmlInput : XmlBaseElement
    {
        [XmlText]
        public string Text { get; set; }

        public override string ToString()
        {
            return Text;
        }
    }

    /// <summary>
    /// The Template class for Weavr-P screen
    /// </summary>
    [XmlRoot(ElementName = "template")]
    public partial class XmlTemplate
    {
        [XmlElement(ElementName = "section")]
        public XmlSection[] Sections { get; set; }

        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "title")]
        public string Title { get; set; }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            foreach (var section in Sections)
            {
                builder.Append(section.ToString());
            }
            return builder.ToString();
        }

        // It is quite slow, so use just one instance for all parsers 
        // (in unity it shouldn't have race issues since it is single threaded)
        private static XmlSerializer _xmlSerializer;

        /// <summary>
        /// Write the xml representation of this Template into a string writer
        /// </summary>
        /// <param name="stream">[Optional] The stream writer where to write</param>
        /// <returns>The xml as string</returns>
        public string SerializeToXml(StringWriter stream = null)
        {
            if (_xmlSerializer == null)
            {
                _xmlSerializer = new XmlSerializer(typeof(XmlTemplate));
            }
            bool volatileStream = stream == null;
            if (volatileStream)
            {
                stream = new StringWriter();
            }
            try
            {
                _xmlSerializer.Serialize(stream, this);
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
        public static XmlTemplate CreateFromXml(string filePath)
        {
            using (var stream = new StringReader(File.ReadAllText(filePath)))
            {
                return CreateFromXml(stream);
            }
        }

        /// <summary>
        /// Tries to create the template from an xml string
        /// </summary>
        /// <param name="xml">The string with xml</param>
        /// <returns>The template representation from the xml</returns>
        public static XmlTemplate CreateFromXmlString(string xml)
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
        public static XmlTemplate CreateFromXml(StringReader reader)
        {
            if (_xmlSerializer == null)
            {
                _xmlSerializer = new XmlSerializer(typeof(XmlTemplate));
            }
            return _xmlSerializer.Deserialize(reader) as XmlTemplate;
        }
    }
}
