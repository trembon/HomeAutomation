using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HomeAutomation.Entities
{
    public interface IEntity
    {
        string UniqueID { get; }

        string ToSourceString();
    }
}
