using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.EventSystems;

namespace TXT.WEAVR.UI
{
    public class KeyboardPlayer : MonoBehaviour
    {
        public GameObject keyboard;
        public Button[] keyboardButtons;

        private TMP_InputField m_inputField;
        private string m_textInput;
        private GameObject m_nextInput;
        private GameObject m_currentFocus;
        private int m_frameToHideKeyboard;


        private void Reset()
        {
            keyboardButtons = GetComponentsInChildren<Button>(true);
        }

        private void OnValidate()
        {
            keyboardButtons = GetComponentsInChildren<Button>(true);
        }

        private void Start()
        {
            for (int i = 0; i < keyboardButtons.Length; i++)
            {
                if (!keyboardButtons[i].gameObject.GetComponent<SpecialButtonKeyboard>())
                {
                    if (keyboardButtons[i].gameObject.GetComponentInChildren<TextMeshProUGUI>())
                    {
                        string textButton = keyboardButtons[i].gameObject.GetComponentInChildren<TextMeshProUGUI>().text;
                        keyboardButtons[i].onClick.AddListener(() => OnButtonSelected(textButton));
                    }
                }
                else
                {
                    if (!keyboardButtons[i].GetComponent<SpecialButtonKeyboard>().doOverride)
                    {
                        keyboardButtons[i].onClick.AddListener(() => OnSpecialButtonSelected());
                    }
                }
            }
        }

        private void Update()
        {
            m_frameToHideKeyboard--;
            if (EventSystem.current.currentSelectedGameObject != m_currentFocus && EventSystem.current.currentSelectedGameObject != null)
            {
                m_currentFocus = EventSystem.current.currentSelectedGameObject;
                if (m_currentFocus.GetComponent<TMP_InputField>())
                {
                    if (m_currentFocus.GetComponent<TMP_InputField>() != m_inputField)
                    {
                        CancelKeyboardHiding();
                        ShowKeyboard(EventSystem.current.currentSelectedGameObject);
                    }
                }
                else
                {
                    HideKeyboard();
                }
            }
            else if (m_currentFocus != null && EventSystem.current.currentSelectedGameObject == null)
            {
                m_currentFocus = EventSystem.current.currentSelectedGameObject;
                m_frameToHideKeyboard = 2;
            }
            if (m_frameToHideKeyboard == 0)
            {
                HideKeyboard();
            }
        }

        

        public void ShowKeyboard(GameObject textField)
        {
            keyboard.SetActive(true);
            m_inputField = textField.GetComponent<TMP_InputField>();
            m_textInput = "";
        }

        public void SetCurrentFocus(GameObject focus)
        {
            if (m_inputField)
            {
                focus = m_inputField.gameObject;
                StartCoroutine(ActivateInputFieldWithoutSelection(m_inputField));

            }
            m_currentFocus = focus;
            EventSystem.current.SetSelectedGameObject(focus);
        }



        public void CancelKeyboardHiding()
        {
            m_frameToHideKeyboard = int.MaxValue;
        }

        public void Backspace()
        {
            if (m_textInput.Length != 0)
            {
                m_textInput = m_textInput.Substring(0, m_textInput.Length - 1);                
            }
            if (m_inputField)
            {
                m_inputField.text = m_textInput;
            }
        }

        public void HideKeyboard()
        {
            keyboard.SetActive(false);
            StartCoroutine(ResetInputField());
        }

        IEnumerator ResetInputField()
        {
            yield return null;
            m_inputField = null;
            EventSystem.current.SetSelectedGameObject(null);
        }

        public void EnterPressed()
        {
            Next();
            if (m_nextInput)
            {
                m_nextInput.GetComponent<TMP_InputField>().Select();
            }
            else
            {
                HideKeyboard();
            }
        }

        private void Next()
        {
            if (m_inputField.FindSelectableOnRight() && m_inputField.FindSelectableOnRight().gameObject.GetComponent<TMP_InputField>())
            {
                m_nextInput = m_inputField.FindSelectableOnRight().gameObject;
            }
            else if (m_inputField.FindSelectableOnDown() && m_inputField.FindSelectableOnDown().gameObject.GetComponent<TMP_InputField>())
            {
                m_nextInput = m_inputField.FindSelectableOnDown().gameObject;
                if (m_nextInput.GetComponent<TMP_InputField>().FindSelectableOnLeft() && m_nextInput.GetComponent<TMP_InputField>().FindSelectableOnLeft().gameObject.GetComponent<TMP_InputField>())
                {
                    m_nextInput = m_nextInput.GetComponent<TMP_InputField>().FindSelectableOnLeft().gameObject;
                }
            }
            else
            {
                m_nextInput = null;
            }
        }

        private void OnButtonSelected(string m_textButton)
        {
            CancelKeyboardHiding();
            if (m_inputField)
            {
                m_textInput = m_textInput + m_textButton;
                m_inputField.text = m_textInput;
                StartCoroutine(ActivateInputFieldWithoutSelection(m_inputField));
            }
        }

        private void OnSpecialButtonSelected()
        {
            if (m_inputField)
            {
                StartCoroutine(ActivateInputFieldWithoutSelection(m_inputField));
            }
        }

        IEnumerator ActivateInputFieldWithoutSelection(TMP_InputField inputField)
        {
            inputField.ActivateInputField();
            yield return new WaitForEndOfFrame();
            if (EventSystem.current.currentSelectedGameObject == inputField.gameObject)
            {
                inputField.MoveTextEnd(false);                
            }
        }


    }
}


