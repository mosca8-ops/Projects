using System;
using System.Collections.Generic;
using TXT.WEAVR.Interaction;
using TXT.WEAVR.Procedure;

namespace TXT.WEAVR.Player.Analytics
{
    public delegate void XAPIEventDelegate(string verb, string @object, Guid id);

    public interface IXApiProvider
    {
        bool Active { get; }
        void Prepare(Procedure.Procedure procedure, ExecutionMode mode, IEnumerable<AbstractInteractiveBehaviour> behaviours, XAPIEventDelegate callbackToRaise);
        void Cleanup();
    }
}
