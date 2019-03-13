using System;
using System.Collections.Generic;
using System.Text;

namespace Circuit
{
    /// <summary>
    /// An event occurrence.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct Event<T> : IEquatable<Event<T>>
    {
        public Event(T value)
        {
            this.Value = value;
            this.HasValue = true;
        }
        /// <summary>
        /// The event's value.
        /// </summary>
        public T Value { get; private set; }

        /// <summary>
        /// True if the event has a value, false otherwise.
        /// </summary>
        public bool HasValue { get; private set; }

        public bool Equals(Event<T> other) =>
            EqualityComparer<T>.Default.Equals(Value, other.Value);

        public override bool Equals(object obj) =>
            obj is Event<T> e && Equals(e);

        public override int GetHashCode() =>
            typeof(Event<T>).GetHashCode() ^ Value.GetHashCode();
    }
}
