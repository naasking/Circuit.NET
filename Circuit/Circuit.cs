using System;
using System.Collections.Generic;

namespace Circuit
{
    /// <summary>
    /// A circuit type.
    /// </summary>
    /// <typeparam name="T0">The circuit input.</typeparam>
    /// <typeparam name="T1">The circuit output.</typeparam>
    public struct Circuit<T0, T1>
    {
        // a circuit is delegate network in CPS form
        internal Action<T0, Action<T1>> k;

        internal Circuit(Action<T0, Action<T1>> k) =>
            this.k = k;

        /// <summary>
        /// Connect two circuits.
        /// </summary>
        /// <typeparam name="T2"></typeparam>
        /// <param name="next"></param>
        /// <returns></returns>
        public Circuit<T0, T2> Connect<T2>(Circuit<T1, T2> next)
        {
            var k = this.k;
            return new Circuit<T0, T2>((x0, k2) => k(x0, x1 => next.k(x1, x2 => k2(x2))));
        }

        /// <summary>
        /// Transform the circuit output.
        /// </summary>
        /// <typeparam name="T2"></typeparam>
        /// <param name="next"></param>
        /// <returns></returns>
        public Circuit<T0, T2> Select<T2>(Func<T1, T2> next)
        {
            var k = this.k;
            return new Circuit<T0, T2>((x0, k2) => k(x0, x1 => k2(next(x1))));
        }

        /// <summary>
        /// Merge two circuit outputs.
        /// </summary>
        /// <typeparam name="T2"></typeparam>
        /// <param name="next"></param>
        /// <returns></returns>
        public Circuit<T0, (T1, T2)> Merge<T2>(Circuit<T0, T2> next)
        {
            var k = this.k;
            return new Circuit<T0, (T1, T2)>((x0, k12) => next.k(x0, x2 => k(x0, x1 => k12((x1, x2)))));
        }

        /// <summary>
        /// Merge two circuit outputs.
        /// </summary>
        /// <typeparam name="T2"></typeparam>
        /// <param name="next"></param>
        /// <returns></returns>
        public Circuit<T0, T3> Merge<T2, T3>(Circuit<T0, T2> next, Func<T1, T2, T3> select)
        {
            //FIXME: if 'this' depends upon 'next' somehow, then next.circuit is called twice for every update.
            //This only happens if user supplies a delegate that explicitly invokes Circuit.Run.
            var k = this.k;
            return new Circuit<T0, T3>((x0, k12) => next.k(x0, x2 => k(x0, x1 => k12(select(x1, x2)))));
        }

        /// <summary>
        /// Delays the output by one cycle.
        /// </summary>
        /// <returns>A signal with a delayed output.</returns>
        public Circuit<T0, T1> Delay()
        {
            Action<T0, Action<T1>> next = k;
            T0 xo0 = default(T0);
            Action<T1> ko1 = null;
            return new Circuit<T0, T1>((x0, k1) =>
            {
                if (ko1 != null)
                    next.Invoke(xo0, ko1);
                xo0 = x0;
                ko1 = k1;
            });
        }

        /// <summary>
        /// Take the first output of the circuit.
        /// </summary>
        /// <typeparam name="T2"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public Circuit<(T0, T2), (T1, T2)> First<T2>()
        {
            var k = this.k;
            return new Circuit<(T0, T2), (T1, T2)>((x02, k12) => k(x02.Item1, x1 => k12((x1, x02.Item2))));
        }

        /// <summary>
        /// Run a circuit.
        /// </summary>
        /// <returns></returns>
        public void Run(T0 input, Action<T1> output)
        {
            this.k?.Invoke(input, output);
        }

        /// <summary>
        /// Register a circuit to handle an event.
        /// </summary>
        /// <typeparam name="T0"></typeparam>
        /// <typeparam name="T1"></typeparam>
        /// <param name="output"></param>
        /// <param name="register"></param>
        /// <param name="deregister"></param>
        /// <param name="handle"></param>
        /// <returns></returns>
        /// <remarks>
        /// The event registration is permanent, there is no way to unregister the handler.
        /// </remarks>
        public void Register(Action<T1> output, Action<EventHandler<T0>> register)
        {
            var k = this.k;
            register((o, e) => k(e, output));
        }

        /// <summary>
        /// Register a circuit to handle an event.
        /// </summary>
        /// <typeparam name="T0"></typeparam>
        /// <typeparam name="T1"></typeparam>
        /// <param name="output"></param>
        /// <param name="register"></param>
        /// <param name="deregister"></param>
        /// <param name="handle"></param>
        /// <returns></returns>
        /// <remarks>
        /// Disposing of the handle unregisters the circuit from the event.
        /// </remarks>
        public static IDisposable Register(Action<T1> output, Action<EventHandler<T0>> register, Action<EventHandler<T0>> deregister)
        {
            var circuit = new Circuit<T0, T1>();
            var disposable = new EventRegistration<EventHandler<T0>>(
                (o, e) => circuit.Run(e, output), deregister);
            register(disposable.handler);
            return disposable;
        }
    }

    /// <summary>
    /// Extensions on circuits.
    /// </summary>
    public static class Circuit
    {
        /// <summary>
        /// Create a circuit from a function.
        /// </summary>
        /// <typeparam name="T0"></typeparam>
        /// <typeparam name="T1"></typeparam>
        /// <param name="init"></param>
        /// <returns></returns>
        public static Circuit<T0, T1> Create<T0, T1>(Func<T0, T1> init) =>
            new Circuit<T0, T1>((x0, k1) => k1(init(x0)));

        //public static Reactive<T, Time> Time<T>();
        //public static Reactive<float, float> IntegralFloat();
        //public static Reactive<double, double> IntegralDouble();

        /// <summary>
        /// Circuit outputting a constant value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Circuit<Empty, T> Constant<T>(T value) =>
            new Circuit<Empty, T>((_, k) => k(value));

        /// <summary>
        /// Circuit outputting a constant value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Circuit<Empty, T> Sampler<T>(Func<T> sampler) =>
            new Circuit<Empty, T>((_, k) => k(sampler()));

        /// <summary>
        /// Run a circuit.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="circuit"></param>
        /// <param name="output"></param>
        public static void Run<T>(this Circuit<Empty, T> circuit, Action<T> output) =>
            circuit.Run(Empty.None, output);

        /// <summary>
        /// Filter the signal values.
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static Circuit<T0, T0> Where<T0>(this Circuit<T0, T0> circuit, Func<T0, bool> filter) =>
            new Circuit<T0, T0>((x0, k0) =>
            {
                if (filter(x0))
                    k0(x0);
            });

        /// <summary>
        /// Transmit only unique values.
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        /// <remarks>
        /// This can be dangerous because it accumulates an infinite history of values seen.
        /// </remarks>
        public static Circuit<T0, T0> Distinct<T0>(this Circuit<T0, T0> circuit, Func<T0, bool> filter)
        {
            var set = new HashSet<T0>();
            return circuit.Where(set.Add);
        }

        /// <summary>
        /// Merge two circuits that output events.
        /// </summary>
        /// <typeparam name="T2"></typeparam>
        /// <param name="next"></param>
        /// <returns></returns>
        public static Circuit<T0, Event<T1>> Merge<T0, T1>(this Circuit<T0, Event<T1>> first, Circuit<T0, Event<T1>> second)
        {
            return new Circuit<T0, Event<T1>>((x0, k1) => first.k(x0, x1 => second.k(x0, x2 => k1(x1.HasValue ? x1 : x2))));
        }

        /// <summary>
        /// Switch the active circuit.
        /// </summary>
        /// <typeparam name="T0"></typeparam>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="source"></param>
        /// <param name="map"></param>
        /// <returns></returns>
        public static Circuit<T0, T1> Switch<T0, T1, T2>(this Circuit<T0, (T1, Event<T2>)> source, Func<T2, Circuit<T0, T1>> map)
        {
            Action<T0, Action<T1>> k = null;
            return new Circuit<T0, T1>((x0, k1) => source.k(x0, x1e2 =>
            {
                // switch to another signal when an event occurs
                if (x1e2.Item2.HasValue)
                    k = map(x1e2.Item2.Value).k;
                if (k != null)
                    k(x0, k1);
                else
                    k1(x1e2.Item1);
            }));
        }

        /// <summary>
        /// Construct a feedback circuit.
        /// </summary>
        /// <typeparam name="T0"></typeparam>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="source"></param>
        /// <param name="x2"></param>
        /// <returns></returns>
        public static Circuit<T0, T1> Loop<T0, T1, T2>(this Circuit<(T0, T2), (T1, T2)> source, T2 x2 = default(T2))
        {
            return new Circuit<T0, T1>((x0, k1) => source.k((x0, x2), x12 =>
            {
                x2 = x12.Item2;
                k1(x12.Item1);
            }));
        }
    }
}
