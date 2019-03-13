using System;
using System.Collections.Generic;
using System.Text;

namespace Circuit
{
    public class Example : System.ComponentModel.INotifyPropertyChanged
    {
        string foo;
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        public string Foo
        {
            get => foo;
            set
            {
                var changed = !string.Equals(foo, value, StringComparison.Ordinal);
                foo = value;
                if (changed)
                    PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Foo)));
            }
        }

        public void Register<T>(Circuit<string, T> circuit, Action<T> output)
        {
            //circuit.Register(output, e => PropertyChanged += e);
            PropertyChanged += (o, e) => circuit.Run(Foo, output);
        }
    }
}
