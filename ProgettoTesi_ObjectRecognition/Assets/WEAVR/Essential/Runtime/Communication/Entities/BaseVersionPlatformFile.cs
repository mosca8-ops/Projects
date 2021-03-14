namespace TXT.WEAVR.Communication.Entities
{
    public abstract class BaseVersionPlatformFile : BaseEntity
    {

        public string Src { get; set; }

        public long Size { get; set; }

        public byte[] MD5 { get; set; }
    }
}
