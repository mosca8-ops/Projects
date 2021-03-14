namespace TXT.WEAVR.Communication.Entities.Xapi
{

    using System;
    using System.Collections.Generic;

    [Serializable]
    public class XApiObject
    {

        public Guid Id { get; set; }

        public string ObjectType { get; set; }
               
        public string ActionType { get; set; }

        public XApiExtensions Extensions { get; set; }
    }

    [Serializable]
    public class XApiExtensions
    {

        public Guid? IdProcedure { get; set; }

        public Guid? IdProcedureVersion { get; set; }

        public Guid? IdProcedureVersionPlatform { get; set; }

        public Guid? IdProcedureStep { get; set; }

        public Guid? IdProcedureVersionStep { get; set; }


        public Guid? IdExecutionProcedureVersion { get; set; }

        public Guid? IdExecutionProcedureVersionStep { get; set; }

        public DateTime? TimeStamp { get; set; }

        public Dictionary<string, object> Extensions { get; set; }
    }
}