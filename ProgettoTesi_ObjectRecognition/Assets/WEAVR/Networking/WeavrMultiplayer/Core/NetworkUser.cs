using System;
using TXT.WEAVR.Communication.Entities;

namespace TXT.WEAVR.Networking
{

    public class NetworkUser
    {
        private Photon.Realtime.Room m_currentRoom;
        private User m_user;
        private string m_userId;
        private bool m_isRoomMaster;

        public event Action<NetworkUser, Photon.Realtime.Room> ChangedRoom;
        public event Action<NetworkUser> UserIdChanged;
        public event Action<NetworkUser> UsernameChanged;

        public Photon.Realtime.Room Room {
            get => m_currentRoom;
            set {
                if (m_currentRoom != value)
                {
                    m_currentRoom = value;
                    if (m_currentRoom == null)
                    {
                        m_isRoomMaster = false;
                    }
                    ChangedRoom?.Invoke(this, value);
                }
            }
        }

        public string UserId {
            get => m_userId;
            set {
                if (m_userId != value)
                {
                    m_userId = value;
                    UserIdChanged?.Invoke(this);
                }
            }
        }

        public string FirstName
        {
            get => m_user?.FirstName;
            set
            {
                if(m_user != null && m_user.FirstName != value)
                {
                    m_user.FirstName = value;
                    UsernameChanged?.Invoke(this);
                }
            }
        }

        public string LastName
        {
            get => m_user?.LastName;
            set
            {
                if(m_user != null && m_user.LastName != value)
                {
                    UsernameChanged?.Invoke(this);
                }
            }
        }
        public string Email => m_user?.Email;

        public bool IsRoomMaster {
            get => m_isRoomMaster;
            set {
                if (m_isRoomMaster != value)
                {
                    m_isRoomMaster = value;
                }
            }
        }

        public NetworkUser(User user)
        {
            m_user = user; 
            UserId = user.Id.ToString();
        }

        public static bool AreSame(NetworkUser a, NetworkUser b)
        {
            return a != null && b != null && a.UserId == b.UserId && a.FirstName == b.FirstName && a.LastName == b.LastName && a.Email == b.Email;
        }

        public NetworkUser(string userId, string firstName, string lastName)
        {
            m_userId = userId;
            m_user = new User()
            {
                FirstName = firstName,
                LastName = lastName
            };
        }
    }
}
