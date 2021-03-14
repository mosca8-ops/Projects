using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TXT.WEAVR.Common;

namespace TXT.WEAVR.Procedure
{
    [Serializable]
    public class BillboardAndOutlineState
    {
        public bool hasBillboard;
        public List<string> billboardTexts;
        public bool isOutlined;
        public string outlineColor;

        [NonSerialized]
        private GameObject m_targetGameObject;

        #region [ SNAPSHOT ]
        public void Snaphot(GameObject _gameObject)
        {
            Clear();
            BillboardManager.Instance.OnBillboardHide.AddListener(HandleOnBillboardHide);
            ProcedureRunner.Current.StepFinished += HandleStepFinished;
            m_targetGameObject = _gameObject;

            SnaphotBillboard(m_targetGameObject);
            SnaphotOutline(m_targetGameObject);
        }

        private void SnaphotBillboard(GameObject _gameObject)
        {
            billboardTexts = new List<string>();
            if (BillboardManager.Instance.HasBillboards(_gameObject, out List<Billboard> billboards))
            {
                hasBillboard = true;
                billboardTexts = billboards.Select(b => b.Text).ToList();
            }
        }

        private void SnaphotOutline(GameObject _gameObject)
        {
            if (Outliner.TryGetOutlineColor(_gameObject, out Color? color))
            {
                isOutlined = true;
                outlineColor = ValueSerialization.Serialize(color.Value);
            }
        }

        private void HandleOnBillboardHide(GameObject _gameObject)
        {
            if (m_targetGameObject == _gameObject)
            {
                hasBillboard = false;
                billboardTexts = new List<string>();
                Clear();
            }
        }

        private void HandleStepFinished(IProcedureStep _step)
        {
            Clear();
        }

        private void Clear()
        {
            BillboardManager.Instance.OnBillboardHide.RemoveListener(HandleOnBillboardHide);
            ProcedureRunner.Current.StepFinished -= HandleStepFinished;
            m_targetGameObject = null;
        }
        #endregion

        #region [ RESTORE ]
        public void Restore(GameObject _gameObject)
        {
            RestoreBillboard(_gameObject);
            RestoreOutline(_gameObject);
        }

        private void RestoreBillboard(GameObject _gameObject)
        {
            if (hasBillboard)
            {
                for (int i = 0; i < billboardTexts.Count; i++)
                {
                    if (!BillboardManager.Instance.HasBillboardWithText(_gameObject, billboardTexts[i]))
                        BillboardManager.Instance.ShowBillboardOn(_gameObject, billboardTexts[i]);
                }
            }
            else
            {
                BillboardManager.Instance.HideBillboardOn(_gameObject);
            }
        }

        private void RestoreOutline(GameObject _gameObject)
        {
            if (isOutlined)
            {
                if (!Outliner.HasOutline(_gameObject))
                {
                    var color = (Color)ValueSerialization.Deserialize(outlineColor, typeof(Color));
                    Outliner.Outline(_gameObject, color);
                }
            }
            else
            {
                if (Outliner.HasOutline(_gameObject))
                    Outliner.RemoveOutline(_gameObject);             
            }
        }
        #endregion
    }
}