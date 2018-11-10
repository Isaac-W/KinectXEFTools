using System;
using System.Collections.Generic;
using System.Text;

namespace KinectXEFTools
{
    public interface IEventReader : IDisposable
    {
        XEFEvent GetNextEvent();
        XEFEvent GetNextEvent(Guid streamDataType);
        XEFEvent GetNextEvent(ICollection<Guid> streamDataTypes);

        IEnumerable<XEFEvent> GetAllEvents();
        IEnumerable<XEFEvent> GetAllEvents(Guid streamDataType);
        IEnumerable<XEFEvent> GetAllEvents(ICollection<Guid> streamDataTypes);
    }
}
