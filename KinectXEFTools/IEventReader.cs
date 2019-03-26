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

        IReadOnlyList<XEFEvent> GetAllEvents();
        IReadOnlyList<XEFEvent> GetAllEvents(Guid streamDataType);
        IReadOnlyList<XEFEvent> GetAllEvents(ICollection<Guid> streamDataTypes);

        void Close();
    }
}
