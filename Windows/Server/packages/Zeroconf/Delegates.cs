using System;
namespace Zeroconf
{
    public class Delegates
    {
        public class OnChangeEventArgs : EventArgs
        {
            public readonly string Type;
            public readonly string Name;
            public readonly ServiceStateChange StateChange;

            public OnChangeEventArgs(string type, string name,
                                     ServiceStateChange stateChange)
            {
                this.Type = type;
                this.Name = name;
                this.StateChange = stateChange;
            }
        }

        public delegate void HandlerDelegate(object sender, OnChangeEventArgs args);
    }
}
