using UnityEditor;

namespace TXT.WEAVR.Editor
{

    public abstract class BaseStyles
    {
        private bool m_needsInitialization = true;
        private bool m_isProSkin = false;

        public BaseStyles() {
            m_needsInitialization = true;
        }

        public void Invalidate() {
            m_needsInitialization = true;
        }

        public bool Refresh() {
            if (m_needsInitialization || (m_isProSkin != EditorGUIUtility.isProSkin)) {
                m_needsInitialization = false;
                m_isProSkin = EditorGUIUtility.isProSkin;
                InitializeStyles(m_isProSkin);
                return true;
            }
            return false;
        }

        protected abstract void InitializeStyles(bool isProSkin);
    }
}
