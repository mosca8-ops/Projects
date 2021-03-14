using Reprise;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.License
{
    public class RLMLicenserEditor : ILicenserEditor
    {
        #region [ CONST PART ]

        public const string RLM_DLL_NAME = "rlm1212.dll";

        public const string RLM_PRODUCT_NAME = "plweavreditor";

        public const string RLM_PRODUCT_VERSION = "1.2";

        #endregion [ CONST PART ]

        #region [ PROPERTIES ]

        private IntPtr _license;
        private IntPtr GetLicense(string productName, string productVersion)
        {
            if (_license == IntPtr.Zero)
            {
                _license = RLM.rlm_checkout(GetRlmHandle(), productName, productVersion, 1);
            }
            return _license;
        }

        private IntPtr _rlmHandle;
        private IntPtr GetRlmHandle()
        {
            if (_rlmHandle == IntPtr.Zero)
            {
                _rlmHandle = RLM.rlm_init(GetRlmPathLicense(), null, null);
            }
            return _rlmHandle;
        }

        private string _rlmPathLicense;
        protected string GetRlmPathLicense()
        {
            if (_rlmPathLicense == null)
            {
                _rlmPathLicense = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Pacelab", "WEAVR", "plweavreditor.lic");
            }
            return _rlmPathLicense;
        }

        #endregion [ PROPERTIES ]

        public RLMLicenserEditor() : base()
        {
        }

        public bool IsValid()
        {
            if (GetRlmHandle() == IntPtr.Zero)
            {
                return false;
            }

            if (GetLicense(RLM_PRODUCT_NAME, RLM_PRODUCT_VERSION) == IntPtr.Zero)
            {
                return false;
            }

            int stat = RLM.rlm_license_stat(GetLicense(RLM_PRODUCT_NAME, RLM_PRODUCT_VERSION));
            if (stat != 0)
            {
                Debug.LogError("Checkout of " + RLM_PRODUCT_NAME + " failed: " + RLM.marshalToString(RLM.rlm_errstring(GetLicense(RLM_PRODUCT_NAME, RLM_PRODUCT_VERSION), GetRlmHandle(), new byte[RLM.RLM_ERRSTRING_MAX])));
                return false;
            }

            return true;
        }

        public void RemoveLicense()
        {
            if (File.Exists(GetRlmPathLicense()))
            {
                var fileName = Path.GetFileName(GetRlmPathLicense());
                var directoryOld = Path.Combine(Path.GetDirectoryName(GetRlmPathLicense()), "old");

                if (!Directory.Exists(directoryOld))
                {
                    Directory.CreateDirectory(directoryOld);
                }

                FileUtil.MoveFileOrDirectory(GetRlmPathLicense(), Path.Combine(directoryOld, $"{DateTime.Now.ToString("yyyyMMddHHmmss")}_{fileName}"));
            }

            ResetLicense();
        }

        public void LoadLicense(string filePath)
        {
            var directory = Path.GetDirectoryName(GetRlmPathLicense());
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.Copy(filePath, GetRlmPathLicense());

            GetRlmHandle();
            GetLicense(RLM_PRODUCT_NAME, RLM_PRODUCT_VERSION);
        }

        public void RefreshLicense()
        {
            ResetLicense();

            GetRlmHandle();
            GetLicense(RLM_PRODUCT_NAME, RLM_PRODUCT_VERSION);
        }

        public IEnumerable<string> GetDetails()
        {
            var returnValue = new List<string>();

            if (IsValid())
            {
                var expiration = RLM.marshalToString(RLM.rlm_license_exp(GetLicense(RLM_PRODUCT_NAME, RLM_PRODUCT_VERSION)));
                returnValue.Add($"Expiration: {expiration}");
                if (expiration != "permanent")
                {
                    returnValue.Add($"Days until expiration: {RLM.rlm_license_exp_days(GetLicense(RLM_PRODUCT_NAME, RLM_PRODUCT_VERSION))}");
                }
            }
            else
            {
                int stat = RLM.rlm_license_stat(GetLicense(RLM_PRODUCT_NAME, RLM_PRODUCT_VERSION));
                returnValue.Add(RLM.marshalToString(RLM.rlm_errstring(GetLicense(RLM_PRODUCT_NAME, RLM_PRODUCT_VERSION), GetRlmHandle(), new byte[RLM.RLM_ERRSTRING_MAX])));

                /*
                if (File.Exists(GetRlmPathLicense()))
                {
                    int stat = RLM.rlm_license_stat(GetLicense(RLM_PRODUCT_NAME, RLM_PRODUCT_VERSION));
                    returnValue.Add(RLM.marshalToString(RLM.rlm_errstring(GetLicense(RLM_PRODUCT_NAME, RLM_PRODUCT_VERSION), GetRlmHandle(), new byte[RLM.RLM_ERRSTRING_MAX])));
                }
                else
                {
                    returnValue.Add("No License Found");
                }
                */
            }

            return returnValue;
        }

        private void ResetLicense()
        {
            if (_rlmHandle != IntPtr.Zero)
            {
                if (_license != IntPtr.Zero)
                {
                    RLM.rlm_checkin(_license);
                    _license = IntPtr.Zero;
                }
                RLM.rlm_close(_rlmHandle);
                _rlmHandle = IntPtr.Zero;
            }
        }
    }

}