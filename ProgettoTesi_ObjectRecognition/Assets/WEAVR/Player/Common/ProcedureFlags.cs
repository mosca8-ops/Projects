using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TXT.WEAVR.Player
{
    [Flags]
    public enum ProcedureFlags
    {
        Undefined = 0,
        New = 1 << 0,
        Preview = 1 << 1,
        Sync = 1 << 2,
        Ready = 1 << 3,
        Syncing = 1 << 4,
        CanBeRemoved = 1 << 5,
    }
}
