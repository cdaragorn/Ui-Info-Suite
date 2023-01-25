using System;

namespace UIInfoSuite
{
    public interface ILeEvents
    {
        event EventHandler OnXpChanged;
        void RaiseEvent();
    }
}
