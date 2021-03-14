using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace TXT.WEAVR.Common
{
    [AddComponentMenu("WEAVR/Text Editing/Display List")]
    public class DisplayList : MonoBehaviour
    {
        [Serializable]
        public class TextObject
        {
            public string Id;
            public string Value;
        }

        [Serializable]
        public class ListElement
        {
            [Draggable]
            public List<TextObject> TextObjects;
        }

        [Serializable]
        public class ElementsList
        {
            public List<ListElement> List;
        }

        public string ContentFilePath;
        public List<string> SecondaryContentFilePathList = new List<string>();
        [Draggable]
        public GameObject SampleElement;
        [Draggable]
        public Transform ElementsParent;
        public Color HighlightColor;
        public Color StandardColor;

        private List<DisplayListElement> m_displayListElements = new List<DisplayListElement>();
        private List<ElementsList> m_secondarySearchElements = new List<ElementsList>();
        private int m_highlightItemIndex;
        private int m_index = 0;
        private int m_pageLength = 6;
        private int m_selectedItemIndex = 0;
        private int m_elementSearchResult = -1;
        private int m_listSearchResult = -1;
        private bool m_isHighlightActive = true;

        public int Index
        {
            get => m_index;
            set
            {
                int oldIndex = m_index;
                m_index += value;
                if (m_index > m_displayListElements.Count - 1)
                {
                    m_index = m_displayListElements.Count - 1;
                    if (m_displayListElements[m_index].Empty == true)
                    {
                        m_index = oldIndex;
                    }
                }
                if (m_index < 0)
                {
                    m_index = 0;
                }
                if (m_displayListElements[m_index].Empty == true)
                {
                    m_index = oldIndex;
                }
                UpdatePageContent();
            }
        }

        public int SelectedItemIndex { get => m_selectedItemIndex; set => OnSelectorButtonClick(value); }

        public DisplayTextObject CopyToOther
        {
            get => null;
            set
            {
                if (SelectedItemIndex >= 0 && SelectedItemIndex < m_displayListElements.Count)
                {
                    if (!m_displayListElements[SelectedItemIndex].Empty)
                    {
                        value.SetContent(GetElementValueById(value.Id, SelectedItemIndex));
                    }
                }
            }
        }

        public DisplayTextObject CopyToOtherAfterSearch
        {
            get => null;
            set
            {
                if (ElementSearchResult >= 0)
                {
                    value.SetContent(GetElementValueById(value.Id, ElementSearchResult, ListSearchResult));
                }
            }
        }

        public DisplayListElement CopyAllToOther
        {
            get => null;
            set
            {
                if (SelectedItemIndex >= 0 && SelectedItemIndex < m_displayListElements.Count)
                {
                    if (!m_displayListElements[SelectedItemIndex].Empty)
                    {
                        foreach (var textObj in value.DisplayTextObjects)
                        {
                            textObj.SetContent(GetElementValueById(textObj.Id, SelectedItemIndex));
                        }
                    }
                }
            }
        }

        public DisplayListElement CopyAllToOtherAfterSearch
        {
            get => null;
            set
            {
                if (ElementSearchResult >= 0)
                {
                    foreach (var textObj in value.DisplayTextObjects)
                    {
                        textObj.SetContent(GetElementValueById(textObj.Id, ElementSearchResult, ListSearchResult));
                    }
                }
            }
        }

        public DisplayTextObject SearchById
        {
            get => null;
            set
            {
                SearchForElement(value);
                HighlightItemIndex = m_elementSearchResult;
            }
        }

        public int HighlightItemIndex
        {
            get => m_highlightItemIndex;
            set
            {
                ApplyHighlightOnElements(value);
                m_highlightItemIndex = value;
            }
        }

        public int ElementSearchResult { get => m_elementSearchResult; set => m_elementSearchResult = value; }
        public bool IsHighlightActive { get => m_isHighlightActive; set => m_isHighlightActive = value; }
        public int ListSearchResult { get => m_listSearchResult; set => m_listSearchResult = value; }

        private void Start()
        {
            //CreateJson();
            LoadContentFromJson();
            LoadSecondarySearchContentFromJson();
            HighlightItemIndex = -1;
        }

        private static bool CanUseFileIO() => Application.isEditor || (!Application.isMobilePlatform && !Application.isConsolePlatform);

        public void LoadContentFromJson()
        {
            if (CanUseFileIO() && !Directory.Exists(Application.streamingAssetsPath))
            {
                Directory.CreateDirectory(Application.streamingAssetsPath);
            }
            string filepath = Path.Combine(Application.streamingAssetsPath, ContentFilePath);

            if (Application.platform == RuntimePlatform.Android)
            {
                var fileReader = UnityWebRequest.Get(filepath);
                fileReader.SendWebRequest().completed += ops => InputSystem_AndroidReadContentCompleted(fileReader, ops);
            }
            else if (CanUseFileIO())
            {
                var TextList = JsonUtility.FromJson<ElementsList>(File.ReadAllText(filepath));
                FillElementContent(TextList);
            }
        }

        private void LoadSecondarySearchContentFromJson()
        {
            foreach (var path in SecondaryContentFilePathList)
            {
                if (CanUseFileIO() && !Directory.Exists(Application.streamingAssetsPath))
                {
                    Directory.CreateDirectory(Application.streamingAssetsPath);
                }
                string filepath = Path.Combine(Application.streamingAssetsPath, path);

                if (Application.platform == RuntimePlatform.Android)
                {
                    var fileReader = UnityWebRequest.Get(filepath);
                    fileReader.SendWebRequest().completed += ops => InputSystem_AndroidReadSecondarySearchElementsCompleted(fileReader, ops);
                }
                else if (CanUseFileIO())
                {
                    var TextList = JsonUtility.FromJson<ElementsList>(File.ReadAllText(filepath));
                    CreateSecondarySearchList(TextList);
                }
            }
        }

        private void InputSystem_AndroidReadSecondarySearchElementsCompleted(UnityWebRequest request, AsyncOperation operation)
        {
            if (request.isNetworkError || request.isHttpError)
            {
                Debug.Log("Error reading Android streamming asset");
                return;
            }
            CreateSecondarySearchList(JsonUtility.FromJson<ElementsList>(request.downloadHandler.text));
        }

        private void CreateSecondarySearchList(ElementsList elementsList)
        {
            m_secondarySearchElements.Add(elementsList);
            //foreach (var element in elementsList.List)
            //{
            //    var displayElement = new DisplayListElement();
            //    displayElement.DisplayTextObjects = new List<DisplayTextObject>();
            //    displayElement.Empty = false;

            //    foreach (var text in element.TextObjects)
            //    {
            //        var tempTextObject = new DisplayTextObject();
            //        tempTextObject.Id = text.Id;
            //        tempTextObject.Value = text.Value;
            //        displayElement.DisplayTextObjects.Add(tempTextObject);
            //    }

            //    m_secondarySearchElements[m_secondarySearchElements.Count - 1].Add(displayElement);
            //}
        }

        private void InputSystem_AndroidReadContentCompleted(UnityWebRequest request, AsyncOperation operation)
        {
            if (request.isNetworkError || request.isHttpError)
            {
                Debug.Log("Error reading Android streamming asset");
                return;
            }
            FillElementContent(JsonUtility.FromJson<ElementsList>(request.downloadHandler.text));
        }

        private void FillElementContent(ElementsList elementsList)
        {
            foreach (var element in elementsList.List)
            {
                GameObject go = Instantiate(SampleElement, ElementsParent != null ? ElementsParent : transform);
                var displayElement = go.GetComponent<DisplayListElement>();
                bool active = true;
                if (displayElement != null)
                {
                    displayElement.Empty = false;
                    foreach (var text in displayElement.DisplayTextObjects)
                    {
                        var t = element.TextObjects.SingleOrDefault<TextObject>(s => s.Id == text.Id);
                        if (t != null)
                        {
                            text.SetContent(t.Value);
                            text.Text.color = StandardColor;
                        }
                        else
                        {
                            active = false;
                            Debug.Log("Display list content: id in json not found");
                        }
                    }
                    if (active)
                    {
                        m_displayListElements.Add(displayElement);
                    }
                }
                else
                {
                    active = false;
                }

                go.SetActive(active);
            }
            int counter = 0;

            //add last empty element to the list
            GameObject ego = Instantiate(SampleElement, ElementsParent != null ? ElementsParent : transform);
            var emptyDisplayElement = ego.GetComponent<DisplayListElement>();
            if (emptyDisplayElement != null)
            {
                emptyDisplayElement.Empty = true;
                foreach (var text in emptyDisplayElement.DisplayTextObjects)
                {
                    text.Text.color = StandardColor;
                    text.SetContent("...");
                }
            }
            ego.SetActive(true);
            m_displayListElements.Add(emptyDisplayElement);
            counter++;

            UpdatePageContent();
        }

        public void NextPage()
        {
            Index += m_pageLength;
        }

        public void NextElement()
        {
            Index++;
        }

        public void PreviousPage()
        {
            Index -= m_pageLength;
        }

        public void PreviousElement()
        {
            Index--;
        }

        public void UpdatePageContent()
        {
            int FirstElement = Index;
            int LastElement = Index + m_pageLength;

            for (int i = 0; i < m_displayListElements.Count; i++)
            {
                if (i >= FirstElement && i < LastElement)
                {
                    m_displayListElements[i].gameObject.SetActive(true);
                }
                else
                {
                    m_displayListElements[i].gameObject.SetActive(false);
                }
            }
        }

        public void OnSelectorButtonClick(int buttonIndex)
        {
            if (m_index + buttonIndex < m_displayListElements.Count - 2)
            {
                m_selectedItemIndex = m_index + buttonIndex;
                HighlightItemIndex = m_selectedItemIndex;
            }
        }

        public string GetElementValueById(string id, int index, int secondaryList = -1)
        {
            string resultValue = "";
            DisplayTextObject displayTextObejct = null;
            if (secondaryList == -1)
            {
                if (index < m_displayListElements.Count)
                {
                    displayTextObejct = m_displayListElements[index].DisplayTextObjects.SingleOrDefault<DisplayTextObject>(s => s.Id == id);
                    if (displayTextObejct)
                    {
                        resultValue = displayTextObejct.Value;
                    }
                }
            }
            else
            {
                if (secondaryList < m_secondarySearchElements.Count && index < m_secondarySearchElements[secondaryList].List.Count)
                {
                    var textObject = m_secondarySearchElements[secondaryList].List[index].TextObjects.SingleOrDefault<TextObject>(s => s.Id == id);
                    if (textObject != null)
                    {
                        resultValue = textObject.Value;
                    }
                }
            }



            return resultValue;
        }

        public void ApplyHighlightOnElements(int newHighlightIndex)
        {
            //new index negative only disables the old higlight
            if (IsHighlightActive)
            {
                if (newHighlightIndex >= 0 && m_listSearchResult == -1)
                {
                    foreach (var textObj in m_displayListElements[newHighlightIndex].DisplayTextObjects)
                    {
                        textObj.Text.color = HighlightColor;
                    }
                }
            }
            if (m_highlightItemIndex >= 0)
            {
                foreach (var textObj in m_displayListElements[m_highlightItemIndex].DisplayTextObjects)
                {
                    textObj.Text.color = StandardColor;
                }
            }
        }

        public void SearchForElement(DisplayTextObject textObject)
        {
            ElementSearchResult = -1;
            ListSearchResult = -1;
            for (int i = 0; i < m_displayListElements.Count; i++)
            {
                if (!m_displayListElements[i].Empty)
                {
                    var result = m_displayListElements[i].DisplayTextObjects.SingleOrDefault<DisplayTextObject>(s => (s.Id == textObject.Id) && (s.Value == textObject.Text.text.Replace("@", "")));
                    if (result != null)
                    {
                        ElementSearchResult = i;
                        return;
                    }
                }
            }

            for (int j = 0; j < m_secondarySearchElements.Count; j++)
            {
                for (int i = 0; i < m_secondarySearchElements[j].List.Count; i++)
                {
                    var result = m_secondarySearchElements[j].List[i].TextObjects.SingleOrDefault<TextObject>(s => (s.Id == textObject.Id) && (s.Value == textObject.Text.text.Replace("@", "")));
                    if (result != null)
                    {
                        ListSearchResult = j;
                        ElementSearchResult = i;
                        return;
                    }
                }
            }
        }


        private void CreateJson()
        {
            //List<string> valuesListText3 = new List<string>();
            //valuesListText3.Add("001X");
            //valuesListText3.Add("001Y");
            //valuesListText3.Add("002X");
            //valuesListText3.Add("002Y");
            //valuesListText3.Add("003X");
            //valuesListText3.Add("003Y");
            //valuesListText3.Add("004X");
            //valuesListText3.Add("004Y");
            //valuesListText3.Add("005X");
            //valuesListText3.Add("005Y");
            //valuesListText3.Add("006X");
            //valuesListText3.Add("006Y");
            //valuesListText3.Add("007X");
            //valuesListText3.Add("007Y");
            //valuesListText3.Add("008X");
            //valuesListText3.Add("008Y");
            //valuesListText3.Add("009X");
            //valuesListText3.Add("009Y");
            //valuesListText3.Add("010X");
            //valuesListText3.Add("010Y");
            //valuesListText3.Add("011X");
            //valuesListText3.Add("011Y");
            //valuesListText3.Add("012X");
            //valuesListText3.Add("012Y");
            //valuesListText3.Add("013X");
            //valuesListText3.Add("013Y");
            //valuesListText3.Add("014X");
            //valuesListText3.Add("014Y");
            //valuesListText3.Add("015X");
            //valuesListText3.Add("015Y");
            //valuesListText3.Add("016X");
            //valuesListText3.Add("016Y");
            //valuesListText3.Add("017X");
            //valuesListText3.Add("017Y");
            //valuesListText3.Add("017Z");
            //valuesListText3.Add("018X");
            //valuesListText3.Add("018W");
            //valuesListText3.Add("018Y");
            //valuesListText3.Add("018Z");
            //valuesListText3.Add("019X");
            //valuesListText3.Add("019Y");
            //valuesListText3.Add("019Z");
            //valuesListText3.Add("020X");
            //valuesListText3.Add("020W");
            //valuesListText3.Add("020Y");
            //valuesListText3.Add("020Z");
            //valuesListText3.Add("021X");
            //valuesListText3.Add("021Y");
            //valuesListText3.Add("021Z");
            //valuesListText3.Add("022X");
            //valuesListText3.Add("022W");
            //valuesListText3.Add("022Y");
            //valuesListText3.Add("022Z");
            //valuesListText3.Add("023X");
            //valuesListText3.Add("023Y");
            //valuesListText3.Add("023Z");
            //valuesListText3.Add("024X");
            //valuesListText3.Add("024W");
            //valuesListText3.Add("024Y");
            //valuesListText3.Add("024Z");
            //valuesListText3.Add("025X");
            //valuesListText3.Add("025Y");
            //valuesListText3.Add("025Z");
            //valuesListText3.Add("026X");
            //valuesListText3.Add("026W");
            //valuesListText3.Add("026Y");
            //valuesListText3.Add("026Z");
            //valuesListText3.Add("027X");
            //valuesListText3.Add("027Y");
            //valuesListText3.Add("027Z");
            //valuesListText3.Add("028X");
            //valuesListText3.Add("028W");
            //valuesListText3.Add("028Y");
            //valuesListText3.Add("028Z");
            //valuesListText3.Add("029X");
            //valuesListText3.Add("029Y");
            //valuesListText3.Add("029Z");
            //valuesListText3.Add("030X");
            //valuesListText3.Add("030W");
            //valuesListText3.Add("030Y");
            //valuesListText3.Add("030Z");
            //valuesListText3.Add("031X");
            //valuesListText3.Add("031Y");
            //valuesListText3.Add("031Z");
            //valuesListText3.Add("032X");
            //valuesListText3.Add("032W");
            //valuesListText3.Add("032Y");
            //valuesListText3.Add("032Z");
            //valuesListText3.Add("033X");
            //valuesListText3.Add("033Y");
            //valuesListText3.Add("033Z");
            //valuesListText3.Add("034X");
            //valuesListText3.Add("034W");
            //valuesListText3.Add("034Y");
            //valuesListText3.Add("034Z");
            //valuesListText3.Add("035X");
            //valuesListText3.Add("035Y");
            //valuesListText3.Add("035Z");
            //valuesListText3.Add("036X");
            //valuesListText3.Add("036W");
            //valuesListText3.Add("036Y");
            //valuesListText3.Add("036Z");
            //valuesListText3.Add("037X");
            //valuesListText3.Add("037Y");
            //valuesListText3.Add("037Z");
            //valuesListText3.Add("038X");
            //valuesListText3.Add("038W");
            //valuesListText3.Add("038Y");
            //valuesListText3.Add("038Z");
            //valuesListText3.Add("039X");
            //valuesListText3.Add("039Y");
            //valuesListText3.Add("039Z");
            //valuesListText3.Add("040X");
            //valuesListText3.Add("040W");
            //valuesListText3.Add("040Y");
            //valuesListText3.Add("040Z");
            //valuesListText3.Add("041X");
            //valuesListText3.Add("041Y");
            //valuesListText3.Add("041Z");
            //valuesListText3.Add("042X");
            //valuesListText3.Add("042W");
            //valuesListText3.Add("042Y");
            //valuesListText3.Add("042Z");
            //valuesListText3.Add("043X");
            //valuesListText3.Add("043Y");
            //valuesListText3.Add("043Z");
            //valuesListText3.Add("044X");
            //valuesListText3.Add("044W");
            //valuesListText3.Add("044Y");
            //valuesListText3.Add("044Z");
            //valuesListText3.Add("045X");
            //valuesListText3.Add("045Y");
            //valuesListText3.Add("045Z");
            //valuesListText3.Add("046X");
            //valuesListText3.Add("046W");
            //valuesListText3.Add("046Y");
            //valuesListText3.Add("046Z");
            //valuesListText3.Add("047X");
            //valuesListText3.Add("047Y");
            //valuesListText3.Add("047Z");
            //valuesListText3.Add("048X");
            //valuesListText3.Add("048W");
            //valuesListText3.Add("048Y");
            //valuesListText3.Add("048Z");
            //valuesListText3.Add("049X");
            //valuesListText3.Add("049Y");
            //valuesListText3.Add("049Z");
            //valuesListText3.Add("050X");
            //valuesListText3.Add("050W");
            //valuesListText3.Add("050Y");
            //valuesListText3.Add("050Z");
            //valuesListText3.Add("051X");
            //valuesListText3.Add("051Y");
            //valuesListText3.Add("051Z");
            //valuesListText3.Add("052X");
            //valuesListText3.Add("052W");
            //valuesListText3.Add("052Y");
            //valuesListText3.Add("052Z");
            //valuesListText3.Add("053X");
            //valuesListText3.Add("053Y");
            //valuesListText3.Add("053Z");
            //valuesListText3.Add("054X");
            //valuesListText3.Add("054W");
            //valuesListText3.Add("054Y");
            //valuesListText3.Add("054Z");
            //valuesListText3.Add("055X");
            //valuesListText3.Add("055Y");
            //valuesListText3.Add("055Z");
            //valuesListText3.Add("056X");
            //valuesListText3.Add("056W");
            //valuesListText3.Add("056Y");
            //valuesListText3.Add("056Z");
            //valuesListText3.Add("057X");
            //valuesListText3.Add("057Y");
            //valuesListText3.Add("058X");
            //valuesListText3.Add("058Y");
            //valuesListText3.Add("059X");
            //valuesListText3.Add("059Y");
            //valuesListText3.Add("060X");
            //valuesListText3.Add("060Y");
            //valuesListText3.Add("061X");
            //valuesListText3.Add("061Y");
            //valuesListText3.Add("062X");
            //valuesListText3.Add("062Y");
            //valuesListText3.Add("063X");
            //valuesListText3.Add("063Y");
            //valuesListText3.Add("064X");
            //valuesListText3.Add("064Y");
            //valuesListText3.Add("065X");
            //valuesListText3.Add("065Y");
            //valuesListText3.Add("066X");
            //valuesListText3.Add("066Y");
            //valuesListText3.Add("067X");
            //valuesListText3.Add("067Y");
            //valuesListText3.Add("068X");
            //valuesListText3.Add("068Y");
            //valuesListText3.Add("069X");
            //valuesListText3.Add("069Y");
            //valuesListText3.Add("070X");
            //valuesListText3.Add("070Y");
            //valuesListText3.Add("071X");
            //valuesListText3.Add("071Y");
            //valuesListText3.Add("072X");
            //valuesListText3.Add("072Y");
            //valuesListText3.Add("073X");
            //valuesListText3.Add("073Y");
            //valuesListText3.Add("074X");
            //valuesListText3.Add("074Y");
            //valuesListText3.Add("075X");
            //valuesListText3.Add("075Y");
            //valuesListText3.Add("076X");
            //valuesListText3.Add("076Y");
            //valuesListText3.Add("077X");
            //valuesListText3.Add("077Y");
            //valuesListText3.Add("078X");
            //valuesListText3.Add("078Y");
            //valuesListText3.Add("079X");
            //valuesListText3.Add("079Y");
            //valuesListText3.Add("080X");
            //valuesListText3.Add("080Y");
            //valuesListText3.Add("080Z");
            //valuesListText3.Add("081X");
            //valuesListText3.Add("081Y");
            //valuesListText3.Add("081Z");
            //valuesListText3.Add("082X");
            //valuesListText3.Add("082Y");
            //valuesListText3.Add("082Z");
            //valuesListText3.Add("083X");
            //valuesListText3.Add("083Y");
            //valuesListText3.Add("083Z");
            //valuesListText3.Add("084X");
            //valuesListText3.Add("084Y");
            //valuesListText3.Add("084Z");
            //valuesListText3.Add("085X");
            //valuesListText3.Add("085Y");
            //valuesListText3.Add("085Z");
            //valuesListText3.Add("086X");
            //valuesListText3.Add("086Y");
            //valuesListText3.Add("086Z");
            //valuesListText3.Add("087X");
            //valuesListText3.Add("087Y");
            //valuesListText3.Add("087Z");
            //valuesListText3.Add("088X");
            //valuesListText3.Add("088Y");
            //valuesListText3.Add("088Z");
            //valuesListText3.Add("089X");
            //valuesListText3.Add("089Y");
            //valuesListText3.Add("089Z");
            //valuesListText3.Add("090X");
            //valuesListText3.Add("090Y");
            //valuesListText3.Add("090Z");
            //valuesListText3.Add("091X");
            //valuesListText3.Add("091Y");
            //valuesListText3.Add("091Z");
            //valuesListText3.Add("092X");
            //valuesListText3.Add("092Y");
            //valuesListText3.Add("092Z");
            //valuesListText3.Add("093X");
            //valuesListText3.Add("093Y");
            //valuesListText3.Add("093Z");
            //valuesListText3.Add("094X");
            //valuesListText3.Add("094Y");
            //valuesListText3.Add("094Z");
            //valuesListText3.Add("095X");
            //valuesListText3.Add("095Y");
            //valuesListText3.Add("095Z");
            //valuesListText3.Add("096X");
            //valuesListText3.Add("096Y");
            //valuesListText3.Add("096Z");
            //valuesListText3.Add("097X");
            //valuesListText3.Add("097Y");
            //valuesListText3.Add("097Z");
            //valuesListText3.Add("098X");
            //valuesListText3.Add("098Y");
            //valuesListText3.Add("098Z");
            //valuesListText3.Add("099X");
            //valuesListText3.Add("099Y");
            //valuesListText3.Add("099Z");
            //valuesListText3.Add("100X");
            //valuesListText3.Add("100Y");
            //valuesListText3.Add("100Z");
            //valuesListText3.Add("101X");
            //valuesListText3.Add("101Y");
            //valuesListText3.Add("101Z");
            //valuesListText3.Add("102X");
            //valuesListText3.Add("102Y");
            //valuesListText3.Add("102Z");
            //valuesListText3.Add("103X");
            //valuesListText3.Add("103Y");
            //valuesListText3.Add("103Z");
            //valuesListText3.Add("104X");
            //valuesListText3.Add("104Y");
            //valuesListText3.Add("104Z");
            //valuesListText3.Add("105X");
            //valuesListText3.Add("105Y");
            //valuesListText3.Add("105Z");
            //valuesListText3.Add("106X");
            //valuesListText3.Add("106Y");
            //valuesListText3.Add("106Z");
            //valuesListText3.Add("107X");
            //valuesListText3.Add("107Y");
            //valuesListText3.Add("107Z");
            //valuesListText3.Add("108X");
            //valuesListText3.Add("108Y");
            //valuesListText3.Add("108Z");
            //valuesListText3.Add("109X");
            //valuesListText3.Add("109Y");
            //valuesListText3.Add("109Z");
            //valuesListText3.Add("110X");
            //valuesListText3.Add("110Y");
            //valuesListText3.Add("110Z");
            //valuesListText3.Add("111X");
            //valuesListText3.Add("111Y");
            //valuesListText3.Add("111Z");
            //valuesListText3.Add("112X");
            //valuesListText3.Add("112Y");
            //valuesListText3.Add("112Z");
            //valuesListText3.Add("113X");
            //valuesListText3.Add("113Y");
            //valuesListText3.Add("113Z");
            //valuesListText3.Add("114X");
            //valuesListText3.Add("114Y");
            //valuesListText3.Add("114Z");
            //valuesListText3.Add("115X");
            //valuesListText3.Add("115Y");
            //valuesListText3.Add("115Z");
            //valuesListText3.Add("116X");
            //valuesListText3.Add("116Y");
            //valuesListText3.Add("116Z");
            //valuesListText3.Add("117X");
            //valuesListText3.Add("117Y");
            //valuesListText3.Add("117Z");
            //valuesListText3.Add("118X");
            //valuesListText3.Add("118Y");
            //valuesListText3.Add("118Z");
            //valuesListText3.Add("119X");
            //valuesListText3.Add("119Y");
            //valuesListText3.Add("119Z");
            //valuesListText3.Add("120X");
            //valuesListText3.Add("120Y");
            //valuesListText3.Add("121X");
            //valuesListText3.Add("121Y");
            //valuesListText3.Add("122X");
            //valuesListText3.Add("122Y");
            //valuesListText3.Add("123X");
            //valuesListText3.Add("123Y");
            //valuesListText3.Add("124X");
            //valuesListText3.Add("124Y");
            //valuesListText3.Add("125X");
            //valuesListText3.Add("125Y");
            //valuesListText3.Add("126X");
            //valuesListText3.Add("126Y");

            //List<string> valuesListText4 = new List<string>();
            //valuesListText4.Add("-");
            //valuesListText4.Add("-");
            //valuesListText4.Add("-");
            //valuesListText4.Add("-");
            //valuesListText4.Add("-");
            //valuesListText4.Add("-");
            //valuesListText4.Add("-");
            //valuesListText4.Add("-");
            //valuesListText4.Add("-");
            //valuesListText4.Add("-");
            //valuesListText4.Add("-");
            //valuesListText4.Add("-");
            //valuesListText4.Add("-");
            //valuesListText4.Add("-");
            //valuesListText4.Add("-");
            //valuesListText4.Add("-");
            //valuesListText4.Add("-");
            //valuesListText4.Add("-");
            //valuesListText4.Add("-");
            //valuesListText4.Add("-");
            //valuesListText4.Add("-");
            //valuesListText4.Add("-");
            //valuesListText4.Add("-");
            //valuesListText4.Add("-");
            //valuesListText4.Add("-");
            //valuesListText4.Add("-");
            //valuesListText4.Add("-");
            //valuesListText4.Add("-");
            //valuesListText4.Add("-");
            //valuesListText4.Add("-");
            //valuesListText4.Add("-");
            //valuesListText4.Add("-");
            //valuesListText4.Add("108.00");
            //valuesListText4.Add("108.05");
            //valuesListText4.Add("-");
            //valuesListText4.Add("108.10");
            //valuesListText4.Add("-");
            //valuesListText4.Add("108.15");
            //valuesListText4.Add("-");
            //valuesListText4.Add("108.20");
            //valuesListText4.Add("108.25");
            //valuesListText4.Add("-");
            //valuesListText4.Add("108.30");
            //valuesListText4.Add("-");
            //valuesListText4.Add("108.35");
            //valuesListText4.Add("-");
            //valuesListText4.Add("108.40");
            //valuesListText4.Add("108.45");
            //valuesListText4.Add("-");
            //valuesListText4.Add("108.50");
            //valuesListText4.Add("-");
            //valuesListText4.Add("108.55");
            //valuesListText4.Add("-");
            //valuesListText4.Add("108.60");
            //valuesListText4.Add("108.65");
            //valuesListText4.Add("-");
            //valuesListText4.Add("108.70");
            //valuesListText4.Add("-");
            //valuesListText4.Add("108.75");
            //valuesListText4.Add("-");
            //valuesListText4.Add("108.80");
            //valuesListText4.Add("108.85");
            //valuesListText4.Add("-");
            //valuesListText4.Add("108.90");
            //valuesListText4.Add("-");
            //valuesListText4.Add("108.95");
            //valuesListText4.Add("-");
            //valuesListText4.Add("109.00");
            //valuesListText4.Add("109.05");
            //valuesListText4.Add("-");
            //valuesListText4.Add("109.10");
            //valuesListText4.Add("-");
            //valuesListText4.Add("109.15");
            //valuesListText4.Add("-");
            //valuesListText4.Add("109.20");
            //valuesListText4.Add("109.25");
            //valuesListText4.Add("-");
            //valuesListText4.Add("109.30");
            //valuesListText4.Add("-");
            //valuesListText4.Add("109.35");
            //valuesListText4.Add("-");
            //valuesListText4.Add("109.40");
            //valuesListText4.Add("109.45");
            //valuesListText4.Add("-");
            //valuesListText4.Add("109.50");
            //valuesListText4.Add("-");
            //valuesListText4.Add("109.55");
            //valuesListText4.Add("-");
            //valuesListText4.Add("109.60");
            //valuesListText4.Add("109.65");
            //valuesListText4.Add("-");
            //valuesListText4.Add("109.70");
            //valuesListText4.Add("-");
            //valuesListText4.Add("109.75");
            //valuesListText4.Add("-");
            //valuesListText4.Add("109.80");
            //valuesListText4.Add("109.85");
            //valuesListText4.Add("-");
            //valuesListText4.Add("109.90");
            //valuesListText4.Add("-");
            //valuesListText4.Add("109.95");
            //valuesListText4.Add("-");
            //valuesListText4.Add("110.00");
            //valuesListText4.Add("110.05");
            //valuesListText4.Add("-");
            //valuesListText4.Add("110.10");
            //valuesListText4.Add("-");
            //valuesListText4.Add("110.15");
            //valuesListText4.Add("-");
            //valuesListText4.Add("110.20");
            //valuesListText4.Add("110.25");
            //valuesListText4.Add("-");
            //valuesListText4.Add("110.30");
            //valuesListText4.Add("-");
            //valuesListText4.Add("110.35");
            //valuesListText4.Add("-");
            //valuesListText4.Add("110.40");
            //valuesListText4.Add("110.45");
            //valuesListText4.Add("-");
            //valuesListText4.Add("110.50");
            //valuesListText4.Add("-");
            //valuesListText4.Add("110.55");
            //valuesListText4.Add("-");
            //valuesListText4.Add("110.60");
            //valuesListText4.Add("110.65");
            //valuesListText4.Add("-");
            //valuesListText4.Add("110.70");
            //valuesListText4.Add("-");
            //valuesListText4.Add("110.75");
            //valuesListText4.Add("-");
            //valuesListText4.Add("110.80");
            //valuesListText4.Add("110.85");
            //valuesListText4.Add("-");
            //valuesListText4.Add("110.90");
            //valuesListText4.Add("-");
            //valuesListText4.Add("110.95");
            //valuesListText4.Add("-");
            //valuesListText4.Add("111.00");
            //valuesListText4.Add("111.05");
            //valuesListText4.Add("-");
            //valuesListText4.Add("111.10");
            //valuesListText4.Add("-");
            //valuesListText4.Add("111.15");
            //valuesListText4.Add("-");
            //valuesListText4.Add("111.20");
            //valuesListText4.Add("111.25");
            //valuesListText4.Add("-");
            //valuesListText4.Add("111.30");
            //valuesListText4.Add("-");
            //valuesListText4.Add("111.35");
            //valuesListText4.Add("-");
            //valuesListText4.Add("111.40");
            //valuesListText4.Add("111.45");
            //valuesListText4.Add("-");
            //valuesListText4.Add("111.50");
            //valuesListText4.Add("-");
            //valuesListText4.Add("111.55");
            //valuesListText4.Add("-");
            //valuesListText4.Add("111.60");
            //valuesListText4.Add("111.65");
            //valuesListText4.Add("-");
            //valuesListText4.Add("111.70");
            //valuesListText4.Add("-");
            //valuesListText4.Add("111.75");
            //valuesListText4.Add("-");
            //valuesListText4.Add("111.80");
            //valuesListText4.Add("111.85");
            //valuesListText4.Add("-");
            //valuesListText4.Add("111.90");
            //valuesListText4.Add("-");
            //valuesListText4.Add("111.95");
            //valuesListText4.Add("-");
            //valuesListText4.Add("112.00");
            //valuesListText4.Add("112.05");
            //valuesListText4.Add("112.10");
            //valuesListText4.Add("112.15");
            //valuesListText4.Add("112.20");
            //valuesListText4.Add("112.25");
            //valuesListText4.Add("-");
            //valuesListText4.Add("-");
            //valuesListText4.Add("-");
            //valuesListText4.Add("-");
            //valuesListText4.Add("-");
            //valuesListText4.Add("-");
            //valuesListText4.Add("-");
            //valuesListText4.Add("-");
            //valuesListText4.Add("-");
            //valuesListText4.Add("-");
            //valuesListText4.Add("-");
            //valuesListText4.Add("-");
            //valuesListText4.Add("-");
            //valuesListText4.Add("-");
            //valuesListText4.Add("-");
            //valuesListText4.Add("-");
            //valuesListText4.Add("-");
            //valuesListText4.Add("-");
            //valuesListText4.Add("-");
            //valuesListText4.Add("-");
            //valuesListText4.Add("112.30");
            //valuesListText4.Add("112.35");
            //valuesListText4.Add("112.40");
            //valuesListText4.Add("112.45");
            //valuesListText4.Add("112.50");
            //valuesListText4.Add("112.55");
            //valuesListText4.Add("112.60");
            //valuesListText4.Add("112.65");
            //valuesListText4.Add("112.70");
            //valuesListText4.Add("112.75");
            //valuesListText4.Add("112.80");
            //valuesListText4.Add("112.85");
            //valuesListText4.Add("112.90");
            //valuesListText4.Add("112.95");
            //valuesListText4.Add("113.00");
            //valuesListText4.Add("113.05");
            //valuesListText4.Add("113.10");
            //valuesListText4.Add("113.15");
            //valuesListText4.Add("113.20");
            //valuesListText4.Add("113.25");
            //valuesListText4.Add("113.30");
            //valuesListText4.Add("113.35");
            //valuesListText4.Add("-");
            //valuesListText4.Add("113.40");
            //valuesListText4.Add("113.45");
            //valuesListText4.Add("-");
            //valuesListText4.Add("113.50");
            //valuesListText4.Add("113.55");
            //valuesListText4.Add("-");
            //valuesListText4.Add("113.60");
            //valuesListText4.Add("113.65");
            //valuesListText4.Add("-");
            //valuesListText4.Add("113.70");
            //valuesListText4.Add("113.75");
            //valuesListText4.Add("-");
            //valuesListText4.Add("113.80");
            //valuesListText4.Add("113.85");
            //valuesListText4.Add("-");
            //valuesListText4.Add("113.90");
            //valuesListText4.Add("113.95");
            //valuesListText4.Add("-");
            //valuesListText4.Add("114.00");
            //valuesListText4.Add("114.05");
            //valuesListText4.Add("-");
            //valuesListText4.Add("114.10");
            //valuesListText4.Add("114.15");
            //valuesListText4.Add("-");
            //valuesListText4.Add("114.20");
            //valuesListText4.Add("114.25");
            //valuesListText4.Add("-");
            //valuesListText4.Add("114.30");
            //valuesListText4.Add("114.35");
            //valuesListText4.Add("-");
            //valuesListText4.Add("114.40");
            //valuesListText4.Add("114.45");
            //valuesListText4.Add("-");
            //valuesListText4.Add("114.50");
            //valuesListText4.Add("114.55");
            //valuesListText4.Add("-");
            //valuesListText4.Add("114.60");
            //valuesListText4.Add("114.65");
            //valuesListText4.Add("-");
            //valuesListText4.Add("114.70");
            //valuesListText4.Add("114.75");
            //valuesListText4.Add("-");
            //valuesListText4.Add("114.80");
            //valuesListText4.Add("114.85");
            //valuesListText4.Add("-");
            //valuesListText4.Add("114.90");
            //valuesListText4.Add("114.95");
            //valuesListText4.Add("-");
            //valuesListText4.Add("115.00");
            //valuesListText4.Add("115.05");
            //valuesListText4.Add("-");
            //valuesListText4.Add("115.10");
            //valuesListText4.Add("115.15");
            //valuesListText4.Add("-");
            //valuesListText4.Add("115.20");
            //valuesListText4.Add("115.25");
            //valuesListText4.Add("-");
            //valuesListText4.Add("115.30");
            //valuesListText4.Add("115.35");
            //valuesListText4.Add("-");
            //valuesListText4.Add("115.40");
            //valuesListText4.Add("115.45");
            //valuesListText4.Add("-");
            //valuesListText4.Add("115.50");
            //valuesListText4.Add("115.55");
            //valuesListText4.Add("-");
            //valuesListText4.Add("115.60");
            //valuesListText4.Add("115.65");
            //valuesListText4.Add("-");
            //valuesListText4.Add("115.70");
            //valuesListText4.Add("115.75");
            //valuesListText4.Add("-");
            //valuesListText4.Add("115.80");
            //valuesListText4.Add("115.85");
            //valuesListText4.Add("-");
            //valuesListText4.Add("115.90");
            //valuesListText4.Add("115.95");
            //valuesListText4.Add("-");
            //valuesListText4.Add("116.00");
            //valuesListText4.Add("116.05");
            //valuesListText4.Add("-");
            //valuesListText4.Add("116.10");
            //valuesListText4.Add("116.15");
            //valuesListText4.Add("-");
            //valuesListText4.Add("116.20");
            //valuesListText4.Add("116.25");
            //valuesListText4.Add("-");
            //valuesListText4.Add("116.30");
            //valuesListText4.Add("116.35");
            //valuesListText4.Add("-");
            //valuesListText4.Add("116.40");
            //valuesListText4.Add("116.45");
            //valuesListText4.Add("-");
            //valuesListText4.Add("116.50");
            //valuesListText4.Add("116.55");
            //valuesListText4.Add("-");
            //valuesListText4.Add("116.60");
            //valuesListText4.Add("116.65");
            //valuesListText4.Add("-");
            //valuesListText4.Add("116.70");
            //valuesListText4.Add("116.75");
            //valuesListText4.Add("-");
            //valuesListText4.Add("116.80");
            //valuesListText4.Add("116.85");
            //valuesListText4.Add("-");
            //valuesListText4.Add("116.90");
            //valuesListText4.Add("116.95");
            //valuesListText4.Add("-");
            //valuesListText4.Add("117.00");
            //valuesListText4.Add("117.05");
            //valuesListText4.Add("-");
            //valuesListText4.Add("117.10");
            //valuesListText4.Add("117.15");
            //valuesListText4.Add("-");
            //valuesListText4.Add("117.20");
            //valuesListText4.Add("117.25");
            //valuesListText4.Add("-");
            //valuesListText4.Add("117.30");
            //valuesListText4.Add("117.35");
            //valuesListText4.Add("117.40");
            //valuesListText4.Add("117.45");
            //valuesListText4.Add("117.50");
            //valuesListText4.Add("117.55");
            //valuesListText4.Add("117.60");
            //valuesListText4.Add("117.65");
            //valuesListText4.Add("117.70");
            //valuesListText4.Add("117.75");
            //valuesListText4.Add("117.80");
            //valuesListText4.Add("117.85");
            //valuesListText4.Add("117.90");
            //valuesListText4.Add("117.95");

            //ListElement le = new ListElement();
            //TextObject to = new TextObject();
            //List<TextObject> TOL = new List<TextObject>();
            //List<ListElement> listelme = new List<ListElement>();
            //ElementsList el = new ElementsList();

            //for (int i = 0; i < valuesListText3.Count; i++)
            //{
            //    le = new ListElement();
            //    to = new TextObject();
            //    TOL = new List<TextObject>();

            //    to.Id = "text1";
            //    to.Value = "...";
            //    TOL.Add(to);
            //    to = new TextObject();
            //    to.Id = "text2";
            //    to.Value = "...";
            //    TOL.Add(to);
            //    to = new TextObject();
            //    to.Id = "text3";
            //    to.Value = valuesListText3[i];
            //    TOL.Add(to);
            //    to = new TextObject();
            //    to.Id = "text4";
            //    to.Value = valuesListText4[i];
            //    TOL.Add(to);
            //    le.TextObjects = TOL;
            //    listelme.Add(le);

            //    el.List = listelme;

            //}

            //string a = JsonUtility.ToJson(el, true);
            //File.WriteAllText(InternationalContentFilePath, a);
        }
    }
}