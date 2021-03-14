using UnityEngine;

namespace TXT.WEAVR.Common
{
    [AddComponentMenu("WEAVR/Audio/Audio Listener Muter")]
    public class AudioListenerMuter : MonoBehaviour
    {

        public bool Mute {
            get { return AudioListener.volume == 0; }
            set {
                if (value)
                {
                    AudioListener.volume = 0;
                }
                else
                {
                    AudioListener.volume = 1;
                }
            }
        }
    }
}