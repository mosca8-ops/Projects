namespace TXT.WEAVR.Communication.Entities.Xapi
{
    using System;

    [Serializable]
    public class XApiStatement
    {

        public XApiActor Actor { get; set; }

        public string Verb { get; set; }

        public XApiObject Object { get; set; }
    }
}