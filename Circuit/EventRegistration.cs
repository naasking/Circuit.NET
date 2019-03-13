using System;
using System.Collections.Generic;
using System.Text;

namespace Circuit
{
    /// <summary>
    /// A disposable handle encapsulating an event registration.
    /// </summary>
    /// <typeparam name="TEvent"></typeparam>
    public sealed class EventRegistration<TEvent> : IDisposable
    {
        internal Action<TEvent> deregister;
        internal TEvent handler;

        public EventRegistration(TEvent handler, Action<TEvent> deregister)
        {
            this.deregister = deregister;
            this.handler = handler;
        }

        public void Dispose()
        {
            var x = System.Threading.Interlocked.Exchange(ref deregister, null);
            if (x != null)
            {
                x(handler);
                GC.SuppressFinalize(this);
            }
        }
        ~EventRegistration() => Dispose();
    }
}
