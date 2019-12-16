using System;

namespace DAQ
{
    public interface IConfigureFile
    {
        event EventHandler<ValueChangedEventArgs> ValueChanged;

        bool Contains(string key);

        T GetValue<T>(string key);

        IConfigureFile SetValue<T>(string key, T value);

        IConfigureFile Load(string filePath = null);

        IConfigureFile Clear();

        void Delete();
    }
}