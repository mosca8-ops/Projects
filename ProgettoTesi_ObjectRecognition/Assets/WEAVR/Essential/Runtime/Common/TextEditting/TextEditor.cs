using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TXT.WEAVR.TextEditting;
using UnityEngine;
using UnityEngine.UI;

namespace TXT.WEAVR.Common
{
    [AddComponentMenu("WEAVR/Text Editing/Text Editor")]
    public class TextEditor : SelectableText
    {
        public enum SizeType { Variable, Fixed }

        [Header("Text Editor")]
        [SerializeField]
        [Draggable]
        private Font m_font;
        [SerializeField]
        [Draggable]
        private Text m_cursorComponent;

        [SerializeField]
        private SizeType m_sizeType;
        [SerializeField]
        private string m_fixedFormat;
        [SerializeField]
        private string m_variableFormat;
        [SerializeField]
        private char m_cursorChar = '_';
        [SerializeField]
        private char m_defaultFormatChar = '#';
        [SerializeField]
        private char m_defaultPlaceholder = ' ';
        [SerializeField]
        private FormatChar[] m_formatChars = { new FormatChar('@', "0123456789") };
        [SerializeField]
        private bool m_cursorIsVisible;
        [SerializeField]
        private bool m_autoAdvanceCursor = true;
        [SerializeField]
        private bool m_circularCursor = false;
        [SerializeField]
        [Tooltip("Whether to show the wrong character or not")]
        private bool m_allowInvalidChar = false;
        [SerializeField]
        [Tooltip("Whether to allow the cursor to overflow the overall length of the format")]
        private bool m_allowCursorOverflow = false;
        [SerializeField]
        [Tooltip("Whether to block the cursor to the invalid character position")]
        private bool m_blockCursorOnInvalidChar = false;
        [SerializeField]
        [Tooltip("Whether to show the full format always or not")]
        private bool m_showFullFormat = true;

        [SerializeField]
        private Color[] m_charsColors;

        [Header("Events")]
        [SerializeField]
        private CharacterEvents m_characterEvents;
        [SerializeField]
        private CursorEvents m_cursorEvents;

        [Header("Debug")]
        [SerializeField]
        [ShowAsReadOnly]
        private string m_pureValue;
        //private string m_pureValue;
        //private string m_pureFormattedValue;
        //private int m_cursorPosition;
        //private int m_fixedFormatCursorPosition;
        //private int m_fixedFormatInputSize;
        //private int m_startCursorPoint;
        //private string m_variablePureFormat;

        //private string[] m_variableFormatPieces;

        private StringBuilder m_stringBuilder;
        //private StringBuilder m_pureValueSB;
        //private StringBuilder m_pureFormattedValueSB;

        //private (int pureIndex, FormatChar formatSymbol)[] m_formatToPureMap;
        //private (int formatIndex, FormatChar formatSymbol)[] m_pureToFormatMap;

        private List<FormatChar> m_formatters = new List<FormatChar>();

        private ITextFormatter m_activeFormatter;

        private ITextFormatter ActiveFormatter
        {
            get => m_activeFormatter;
            set
            {
                if(m_activeFormatter != value)
                {
                    UnregisterEvents();
                    m_activeFormatter = value;
                    RegisterEvents();
                }
            }
        }

        public bool IsEditEnabled => m_foreground && m_foreground.gameObject.activeInHierarchy;

        private void RegisterEvents()
        {
            if (m_activeFormatter != null)
            {
                // Set Events
                m_activeFormatter.OnCharInserted -= m_characterEvents.onCharInserted.Invoke;
                m_activeFormatter.OnCharInserted += m_characterEvents.onCharInserted.Invoke;

                m_activeFormatter.OnInvalidChar -= m_characterEvents.onInvalidChar.Invoke;
                m_activeFormatter.OnInvalidChar += m_characterEvents.onInvalidChar.Invoke;

                m_activeFormatter.OnCursorMoved -= CursorUpdated;
                m_activeFormatter.OnCursorMoved += CursorUpdated;

                m_activeFormatter.OnCursorInvalidMove -= m_cursorEvents.onInvalidCursorMove.Invoke;
                m_activeFormatter.OnCursorInvalidMove += m_cursorEvents.onInvalidCursorMove.Invoke;

                m_activeFormatter.OnTextChanged -= SetText;
                m_activeFormatter.OnTextChanged += SetText;
            }
        }

        private void UnregisterEvents()
        {
            if (m_activeFormatter != null)
            {
                // Remove Events
                m_activeFormatter.OnCharInserted -= m_characterEvents.onCharInserted.Invoke;
                m_activeFormatter.OnInvalidChar -= m_characterEvents.onInvalidChar.Invoke;
                m_activeFormatter.OnCursorMoved -= CursorUpdated;
                m_activeFormatter.OnCursorInvalidMove -= m_cursorEvents.onInvalidCursorMove.Invoke;
                m_activeFormatter.OnTextChanged -= SetText;
            }
        }

        public string FullText
        {
            get => m_foreground.text;
            set
            {
                if(m_foreground.text != value)
                {
                    m_foreground.text = value;
                    //m_pureValue = string.Empty;
                    //m_pureFormattedValue = string.Empty;
                }
            }
        }

        public string Text
        {
            get => m_foreground.text;
            set
            {
                if (IsEditEnabled)
                {
                    SetTextAndCursor(value);
                }
            }
        }

        private void SetTextAndCursor(string value, bool cursorAtTheBeginning = false)
        {
            ActiveFormatter.Text = value;
            m_foreground.text = ActiveFormatter.Text;
            if (cursorAtTheBeginning)
            {
                CursorInTheBeginning = true;
            }
            else
            {
                CursorInTheEnd = true;
            }
        }

        public SizeType Size
        {
            get => m_sizeType;
            set
            {
                if(m_sizeType != value)
                {
                    m_sizeType = value;
                    CreateFormatter();
                }
            }
        }

        public bool ClearText
        {
            get => string.IsNullOrEmpty(ActiveFormatter.PureText);
            set
            {
                if (value && IsEditEnabled)
                {
                    ActiveFormatter.Clear();
                }
            }
        }

        public bool TextIsValid => ActiveFormatter.TextIsValid;

        public Text TextComponent => m_foreground;
        public Text CursorComponent => m_cursorComponent;

        private void SetText(string text)
        {
            FullText = text;
            m_pureValue = ActiveFormatter.PureText;
        }

        private void CursorUpdated(int index)
        {
            ApplyCursorPosition();
            m_cursorEvents.onCursorMove.Invoke(index);
        }

        protected override void Reset()
        {
            base.Reset();
            var cursor = transform.Find("Cursor");
            if (!cursor)
            {
                cursor = new GameObject("Cursor").AddComponent<RectTransform>();
                SetAndStretchToParentSize(cursor as RectTransform, transform as RectTransform);
                cursor.localPosition = new Vector3(0, -2, 0);
                cursor.localRotation = Quaternion.identity;
            }

            m_cursorComponent = cursor.GetComponent<Text>();
            if (!m_cursorComponent)
            {
                m_cursorComponent = cursor.gameObject.AddComponent<Text>();
                m_cursorComponent.color = Color.black;
            }

            OnValidate();
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            if (!m_foreground)
            {
                m_foreground = GetComponent<Text>();
            }
            if (m_foreground)
            {
                if (m_font)
                {
                    m_foreground.font = m_font;
                }
                else
                {
                    m_font = m_foreground.font;
                }
            }
            if (m_cursorComponent && m_cursorComponent.gameObject.name == "Cursor")
            {
                m_cursorComponent.font = m_foreground.font;
                m_cursorComponent.supportRichText = true;
            }
            if (Application.isPlaying)
            {
                CursorIsVisible = m_cursorIsVisible;
                CreateFormatter();
            }
        }

        private void CreateFormatter()
        {
            switch (m_sizeType)
            {
                case SizeType.Fixed:
                    if (ActiveFormatter is FixedLengthFormatter fixedFormatter)
                    {
                        fixedFormatter.ShowFullFormat = m_showFullFormat;
                        fixedFormatter.Formatters = new List<FormatChar>() { new FormatChar(m_defaultFormatChar, m_defaultPlaceholder) }.Union(m_formatChars);
                        fixedFormatter.FormatString = m_fixedFormat;
                        fixedFormatter.AllowCursorOverflow = m_allowCursorOverflow;
                        fixedFormatter.AllowInvalidCharInsertion = m_allowInvalidChar;
                        fixedFormatter.BlockCursorOnInvalidChar = m_blockCursorOnInvalidChar;
                        fixedFormatter.DefaultReplaceChar = m_defaultPlaceholder;
                    }
                    else
                    {
                        ActiveFormatter = new FixedLengthFormatter(new List<FormatChar>() { new FormatChar(m_defaultFormatChar, m_defaultPlaceholder) }.Union(m_formatChars), m_charsColors)
                        {
                            ShowFullFormat = m_showFullFormat,
                            FormatString = m_fixedFormat,
                            AllowCursorOverflow = m_allowCursorOverflow,
                            AllowInvalidCharInsertion = m_allowInvalidChar,
                            BlockCursorOnInvalidChar = m_blockCursorOnInvalidChar,
                            DefaultReplaceChar = m_defaultPlaceholder,
                        };
                    }
                    break;
                case SizeType.Variable:
                    ActiveFormatter = new VariableLengthFormatter()
                    {
                        FormatString = m_variableFormat,
                    };
                    break;
            }
            ActiveFormatter.CircularCursor = m_circularCursor;
            ActiveFormatter.AutoAdvanceCursor = m_autoAdvanceCursor;
            if (m_showFullFormat)
            {
                m_foreground.text = ActiveFormatter.Text;
            }
        }

        protected override void Start()
        {
            base.Start();
            m_stringBuilder = new StringBuilder();
            //m_pureValue = m_pureFormattedValue = string.Empty;
            //m_pureValueSB = new StringBuilder();
            //m_pureFormattedValueSB = new StringBuilder();
            if (ActiveFormatter == null)
            {
                CreateFormatter();
            }
            CursorIsVisible = m_cursorIsVisible;
            ApplyCursorPosition();
        }

        private void OnEnable()
        {
            if(ActiveFormatter == null)
            {
                CreateFormatter();
            }
            else
            {
                RegisterEvents();
            }
        }

        private void OnDisable()
        {
            //ActiveFormatter = null;
            UnregisterEvents();
        }

        public void Insert(char character)
        {
            if (IsEditEnabled)
            {
                ActiveFormatter.Insert(character);
            }
        }

        public void Delete()
        {
            if (IsEditEnabled)
            {
                ActiveFormatter.Delete();
            }
        }

        public void Delete(int index)
        {
            if (IsEditEnabled)
            {
                ActiveFormatter.Delete(index);
            }
        }

        public void Append(char character)
        {
            if (IsEditEnabled)
            {
                ActiveFormatter.Append(character);
            }
        }

        public void Append(string str)
        {
            if (IsEditEnabled)
            {
                ActiveFormatter.Append(str);
            }
        }

        public void Insert(string character)
        {
            if (IsEditEnabled)
            {
                ActiveFormatter.Insert(character);
            }
        }

        public Text CopyFormattedToText
        {
            get => m_foreground;
            set
            {
                CopyFormattedTo(value);
            }
        }

        public TextEditor CopyToOther
        {
            get => this;
            set
            {
                value.SetTextAndCursor(Text, true);
            }
        }

        public void CopyFrom(Text textComponent)
        {
            SetTextAndCursor(textComponent.text, true);
        }

        public void CopyFormattedTo(Text otherTextComponent)
        {
            if (otherTextComponent)
            {
                otherTextComponent.text = m_foreground.text;
            }
        }

        public void CopyFormattedTo(SelectableLabel otherTextComponent)
        {
            if (otherTextComponent)
            {
                otherTextComponent.Text = m_foreground.text;
            }
        }

        public void CopyFormattedTo(TextEditor otherTextComponent)
        {
            if (otherTextComponent)
            {
                otherTextComponent.SetTextAndCursor(ActiveFormatter.PureText, true);
            }
        }

        public void Clear()
        {
            if (IsEditEnabled)
            {
                ActiveFormatter.Clear();
            }
        }

        [Serializable]
        private struct CursorEvents
        {
            public UnityEventInt onCursorMove;
            public UnityEventInt onInvalidCursorMove;
        }

        [Serializable]
        private struct CharacterEvents
        {
            public UnityEventChar onCharInserted;
            public UnityEventChar onInvalidChar;
        }

        #region [  CURSOR MANAGEMENT  ]

        public bool CursorIsVisible
        {
            get => m_cursorComponent && m_cursorComponent.gameObject.activeInHierarchy;
            set
            {
                if (m_cursorComponent)
                {
                    m_cursorComponent.gameObject.SetActive(value);
                }
            }
        }

        public bool CursorInTheBeginning
        {
            get => CursorPosition == 0;
            set
            {
                if (value && IsEditEnabled)
                {
                    CursorPosition = 0;
                }
            }
        }

        public bool CursorInTheEnd
        {
            get => ActiveFormatter.CursorIsAtEnd;
            set
            {
                ActiveFormatter.CursorIsAtEnd = value && IsEditEnabled;
            }
        }

        public int CursorPosition
        {
            get => ActiveFormatter.CursorPosition;
            set
            {
                if (IsEditEnabled)
                {
                    ActiveFormatter.CursorPosition = value;
                    ApplyCursorPosition();
                }
            }
        }

        private void ApplyCursorPosition()
        {
            if (m_cursorComponent)
            {
                int emptyChars = ActiveFormatter.CursorInFormattedText;

                m_stringBuilder.Clear();
                for (int i = 0; i < emptyChars; i++)
                {
                    m_stringBuilder.Append("<color=#00000000>").Append(ActiveFormatter.Text[i]).Append("</color>");
                }
                m_stringBuilder.Append(m_cursorChar);
                m_cursorComponent.text = m_stringBuilder.ToString();
            }
        }

        public void IncrementCursor()
        {
            if (m_cursorComponent)
            {
                CursorPosition++;
            }
        }

        public void DecrementCursor()
        {
            if (m_cursorComponent)
            {
                CursorPosition--;
            }
        }

        #endregion
        
    }
}
