using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR
{
    /// <summary>
    /// Class which wraps the <see cref="System.Guid"/> object to be serialized
    /// </summary>
    /// <remarks>It is a class instead of a struct because it hooks up to Unity serialization system and the latter requires a class to work</remarks>
    [Serializable]
    public class SerializedGuid : ISerializationCallbackReceiver, IComparable, IComparable<Guid>, IEquatable<Guid>, IFormattable
    {
        [SerializeField]
        [HideInInspector]
        private byte[] m_guid;

        public Guid Guid { get; private set; }

        public SerializedGuid(Guid guid)
        {
            Guid = guid;
        }

        public SerializedGuid(byte[] b) : this(new Guid(b)) { }
        public SerializedGuid(string g) : this(new Guid(g)) { }
        public SerializedGuid(int a, short b, short c, byte[] d) : this(new Guid(a, b, c, d)) { }
        public SerializedGuid(int a, short b, short c, byte d, byte e, byte f, byte g, byte h, byte i, byte j, byte k) : this(new Guid(a, b, c, d, e, f, g, h, i, j, k)) { }
        public SerializedGuid(uint a, ushort b, ushort c, byte d, byte e, byte f, byte g, byte h, byte i, byte j, byte k) : this(new Guid(a, b, c, d, e, f, g, h, i, j, k)) { }

        public int CompareTo(object obj) => obj is SerializedGuid sGuid ? Guid.CompareTo(sGuid.Guid) : Guid.CompareTo(obj);

        public int CompareTo(Guid other) => Guid.CompareTo(other);

        public bool Equals(Guid other) => Guid.Equals(other);

        public override bool Equals(object obj) => obj is SerializedGuid sGuid ? Guid.Equals(sGuid.Guid) : Guid.Equals(obj);

        public override int GetHashCode() => Guid.GetHashCode();

        public void OnAfterDeserialize()
        {
            if(m_guid?.Length == 16)
            {
                Guid = new Guid(m_guid);
            }
            else
            {
                Guid = Guid.NewGuid();
            }
        }

        public void OnBeforeSerialize()
        {
            if(Guid != Guid.Empty)
            {
                m_guid = Guid.ToByteArray();
            }
            else
            {
                m_guid = new byte[0];
            }
        }

        public override string ToString() => Guid.ToString();

        public string ToString(string format, IFormatProvider formatProvider) => Guid.ToString(format, formatProvider);

        public static implicit operator Guid(SerializedGuid sguid)
        {
            return sguid.Guid;
        }

        public static implicit operator SerializedGuid(Guid guid)
        {
            return new SerializedGuid(guid);
        }

        public static bool operator ==(SerializedGuid a, SerializedGuid b) => a.Guid == b.Guid;
        public static bool operator !=(SerializedGuid a, SerializedGuid b) => a.Guid != b.Guid;
    }
}
