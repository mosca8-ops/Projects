using System;

namespace TXT.WEAVR.Communication.Entities
{
    public class ProcedureMedia : BaseContent
    {
        public Guid ProcedureId { get; set; }

        public string Src { get; set; }

        public int Width { get; set; }  // in pixels
        public int Height { get; set; } // in pixels
        public long Size { get; set; }  // in bytes
        public byte[] MD5 { get; set; }
        public string Description { get; set; }
        public string FileExtension { get; set; }

        // ---------------
    }
}
