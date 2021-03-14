
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Networking
{
    [DefaultExecutionOrder(-27000)]
    [RequireComponent(typeof(Photon.Pun.PhotonView))]
    [AddComponentMenu("WEAVR/Network/Network Voice Recorder")]
    public class NetworkVoiceRecorder : Photon.Pun.MonoBehaviourPun
    {

        private Photon.Voice.Unity.Recorder m_recorder;

        private void Awake()
        {
            m_recorder = GetComponent<Photon.Voice.Unity.Recorder>();
            if (m_recorder)
            {
                if (photonView.IsMine)
                {
                    Photon.Voice.PUN.PhotonVoiceNetwork.Instance.PrimaryRecorder = m_recorder;
                    m_recorder.TransmitEnabled = true;
                }
                else
                {
                    Destroy(m_recorder);
                    m_recorder = null;
                }
            }
        }

        private void Start()
        {
            if (m_recorder)
            {
                m_recorder.TransmitEnabled = true;
            }
        }
    }
}
