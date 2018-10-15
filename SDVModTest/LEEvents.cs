using System;

namespace UIInfoSuite {
    public interface LEEvents
    {
        event EventHandler OnXPChanged;
        void raiseEvent();
    }
}
