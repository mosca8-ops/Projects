namespace TXT.WEAVR.Communication.Entities.Xapi
{

    using System;

    [Serializable]
    public class XApiActor
    {

        public Guid Id { get; set; }

        public String ObjectType { get; set; }
    }
}