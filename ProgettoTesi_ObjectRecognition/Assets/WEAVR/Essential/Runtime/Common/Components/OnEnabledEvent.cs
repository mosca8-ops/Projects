using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace TXT.WEAVR.Common
{

    [AddComponentMenu("WEAVR/Components/On Enable Event")]
    public class OnEnabledEvent : MonoBehaviour
    {
        public OptionalFloat onEnableRaiseDelay;
        public OptionalFloat onDisableRaiseDelay;
        public UnityEventGameObject onEnabled;
        public UnityEventGameObject onDisabled;

        bool callEnabled;

        private async void OnEnable()
        {
            // onEnabled.Invoke(gameObject);
            if (onEnableRaiseDelay.enabled && onEnableRaiseDelay.value > 0)
            {
                await Task.Delay((int)(onEnableRaiseDelay.value * 1000));
                if (isActiveAndEnabled)
                {
                    onEnabled?.Invoke(gameObject);
                }
            }
            else
            {
                callEnabled = true;
            }
        }

        private async void OnDisable()
        {
            if (onDisableRaiseDelay.enabled && onDisableRaiseDelay.value > 0)
            {
                await Task.Delay((int)(onDisableRaiseDelay.value * 1000));
                if (!isActiveAndEnabled)
                {
                    onDisabled?.Invoke(gameObject);
                }
            }
            else
            {
                onDisabled.Invoke(gameObject);
            }
        }

        private void LateUpdate()
        {
            if (callEnabled)
            {
                onEnabled.Invoke(gameObject);
                callEnabled = false;
            }
        }
    }
}
