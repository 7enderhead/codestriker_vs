using Microsoft.TeamFoundation.VersionControl.Client;
using System;

namespace CodestrikerPlugin.Util
{
    public class PendingSetWrapper
    {
        public PendingSet Set { get; set; }
        public DateTime CreationDate { get; set; }
        public PendingSetWrapper(PendingSet set, DateTime creationDate)
        {
            Set = set;
            CreationDate = creationDate;
        }       
    }
}
