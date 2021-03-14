using Reprise;
using System;
using System.IO;
using UnityEngine;

namespace TXT.WEAVR.License
{
    public class RLMLicenserPlayer : ILicenserPlayer
    {
        #region [ CONST PART ]

        private readonly string[] LIC_FILE_FOLDERS = new string[] {
            Path.Combine(Application.streamingAssetsPath, "License"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Pacelab", "WEAVR"),
        };
        private readonly string[] LIC_FILE_NAMES = new string[] { "plweavr.lic", "plweavrplayer.lic", "plweavreditor.lic", "plweavrruntime.lic", "pacegmbh.lic" };

        protected const string RLM_PRODUCT_NAME = "plweavrruntime";
        protected const string RLM_PRODUCT_VERSION = "1.2";

        #endregion [ CONST PART ]

        #region [ PROPERTIES ]

        private IntPtr _rlmHandle;
        private IntPtr GetRlmHandle()
        {
            if (_rlmHandle == IntPtr.Zero)
            {
                _rlmHandle = RLM.rlm_init(GetRlmPathLicense(), null, null);
            }
            return _rlmHandle;
        }

        private IntPtr _license;
        private IntPtr GetLicense(string productName, string productVersion)
        {
            if (_license == IntPtr.Zero)
            {
                _license = RLM.rlm_checkout(GetRlmHandle(), productName, productVersion, 1);
            }
            return _license;
        }

        private string _rlmPathLicense;
        protected string GetRlmPathLicense()
        {
            if (_rlmPathLicense == null)
            {
                foreach (var folder in LIC_FILE_FOLDERS)
                {
                    foreach (var file in LIC_FILE_NAMES)
                    {
                        var path = Path.Combine(folder, file);
                        if (File.Exists(path))
                        {
                            _rlmPathLicense = path;

                            return _rlmPathLicense;
                        }
                    }
                }

                _rlmPathLicense = Path.Combine(LIC_FILE_FOLDERS[0], LIC_FILE_NAMES[0]);

                /*
                if (Application.platform == RuntimePlatform.WindowsPlayer)
                {
                }
                else if (Application.platform == RuntimePlatform.OSXPlayer)
                {

                }
                else if (Application.platform == RuntimePlatform.LinuxPlayer)
                {

                }

                else if (Application.platform == RuntimePlatform.Android)
                {
                    _rlmPathLicense = Path.Combine(Application.streamingAssetsPath, "License", LIC_FILE_NAME);
                }
                else if (Application.platform == RuntimePlatform.IPhonePlayer)
                {
                    _rlmPathLicense = Path.Combine(Application.streamingAssetsPath, "License", LIC_FILE_NAME);
                }
                else if (Application.platform == RuntimePlatform.WebGLPlayer)
                {
                    _rlmPathLicense = Path.Combine(Application.streamingAssetsPath, "License", LIC_FILE_NAME);
                }

                else if (Application.platform == RuntimePlatform.WSAPlayerX86)
                {
                    _rlmPathLicense = Path.Combine(Application.streamingAssetsPath, "License", LIC_FILE_NAME);
                }
                else if (Application.platform == RuntimePlatform.WSAPlayerX64)
                {
                    _rlmPathLicense = Path.Combine(Application.streamingAssetsPath, "License", LIC_FILE_NAME);
                }
                else if (Application.platform == RuntimePlatform.WSAPlayerARM)
                {
                    _rlmPathLicense = Path.Combine(Application.streamingAssetsPath, "License", LIC_FILE_NAME);
                }


                else if (Application.platform == RuntimePlatform.LinuxEditor)
                {
                    _rlmPathLicense = Path.Combine(Application.streamingAssetsPath, "License", LIC_FILE_NAME);
                }
                else if (Application.platform == RuntimePlatform.OSXEditor)
                {
                    _rlmPathLicense = Path.Combine(Application.streamingAssetsPath, "License", LIC_FILE_NAME);
                }
                else if (Application.platform == RuntimePlatform.WindowsEditor)
                {
                    _rlmPathLicense = Path.Combine(Application.streamingAssetsPath, "License", LIC_FILE_NAME);
                }
                */
            }
            return _rlmPathLicense;
        }

        #endregion [ PROPERTIES ]

        public RLMLicenserPlayer()
        {
            if (File.Exists(GetRlmPathLicense()))
            {
                GetRlmHandle();
            }
        }



        public bool IsValid()
        {
            // Check out a license
            //IntPtr license = RLM.rlm_checkout(_rlmHandle, RLM_PRODUCT_NAME, RLM_PRODUCT_VERSION, 1);
            //int stat = RLM.rlm_license_stat(license);
            //if (stat != 0)
            //{
            //    Debug.LogError("checkout of " + RLM_PRODUCT_NAME + " failed: " + RLM.marshalToString(RLM.rlm_errstring(license, _rlmHandle, new byte[RLM.RLM_ERRSTRING_MAX])));
            //    return false;
            //}
            //else
            //{
            //    Debug.LogError("checkout of " + RLM_PRODUCT_NAME + " PERFECT");
            //}

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

    }
}