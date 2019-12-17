using System;

namespace DAQ
{
    public class ValueChangedEventArgs : EventArgs
    {
        public string KeyName { get; }

        public ValueChangedEventArgs(string keyName) => KeyName = keyName;
    }
}
