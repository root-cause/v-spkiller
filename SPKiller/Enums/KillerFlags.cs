using System;

namespace SPKiller.Enums
{
    [Flags]
    public enum KillerFlags
    {
        None            = 0,
        FoundLimb       = 1 << 0,
        FoundWriting    = 1 << 1,
        FoundHandprint  = 1 << 2,
        FoundMachete    = 1 << 3,
        FoundVan        = 1 << 4,
        ReceivedText    = 1 << 5,
        KilledKiller    = 1 << 6
    }
}
