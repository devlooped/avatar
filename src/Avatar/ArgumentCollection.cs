using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using TypeNameFormatter;

namespace Avatars
{
    /// <summary>
    /// Contains the arguments of a method invocation as well as their 
    /// parameter information.
    /// </summary>
    [DebuggerTypeProxy(typeof(DebugView))]
    [DebuggerDisplay("Count = {Count}")]
    public class ArgumentCollection : IArgumentCollection
    {
        static readonly object NullValue = new object();

        readonly ParameterInfo[] infos;
        readonly Dictionary<string, ParameterInfo> nameParams;
        // TODO: should we provide box-free holders too?
        readonly Dictionary<string, object?> values = new();

        /// <summary>
        /// Creates a new argument collection by cloning the given <see cref="IArgumentCollection"/> 
        /// and optionally specifying new values.
        /// </summary>
        /// <remarks>
        /// If the received <paramref name="arguments"/> is an instance of <see cref="ArgumentCollection"/>, 
        /// the existing values will be copied over too.
        /// </remarks>
        public ArgumentCollection(IArgumentCollection arguments, params object?[] values)
        {
            if (arguments is ArgumentCollection collection)
            {
                infos = collection.infos;
                nameParams = collection.nameParams;
                this.values = collection.values;
            }
            else
            {
                infos = arguments.ToArray();
                nameParams = infos.ToDictionary(x => x.Name);
            }

            SetValues(values);
        }

        /// <summary>
        /// Creates a new argument collection using the given parameter information 
        /// and optional values.
        /// </summary>
        public ArgumentCollection(ParameterInfo[] infos, params object?[] values)
        {
            this.infos = infos;
            nameParams = infos.ToDictionary(x => x.Name);
            SetValues(values);
        }

        /// <inheritdoc />
        public ParameterInfo this[int index]
        {
            get => (index < 0 || index >= infos.Length)
                ? throw new IndexOutOfRangeException(ThisAssembly.Strings.ArgumentIndexNotFound(index))
                : infos[index];
        }

        /// <inheritdoc />
        public ParameterInfo this[string name]
        {
            get => nameParams.TryGetValue(name, out var parameter)
                ? parameter
                : throw new KeyNotFoundException(ThisAssembly.Strings.ArgumentNotFound(name));
        }

        /// <inheritdoc />
        public int Count => infos.Length;

        /// <inheritdoc />
        public object? GetValue(string name)
        {
            if (!nameParams.ContainsKey(name))
                throw new KeyNotFoundException(ThisAssembly.Strings.ArgumentNotFound(name));

            return values.TryGetValue(name, out var value) ? value : null;
        }

        /// <inheritdoc />
        public object? GetValue(int index)
        {
            if (index < 0 || index >= infos.Length)
                throw new IndexOutOfRangeException(ThisAssembly.Strings.ArgumentIndexNotFound(index));

            return values.TryGetValue(infos[index].Name, out var value) ? value : null;
        }

        /// <inheritdoc />
        public void SetValue(string name, object? value)
        {
            if (!nameParams.ContainsKey(name))
                throw new KeyNotFoundException(ThisAssembly.Strings.ArgumentNotFound(name));

            values[name] = value;
        }

        /// <inheritdoc />
        public void SetValue(int index, object? value)
        {
            if (index < 0 || index >= infos.Length)
                throw new IndexOutOfRangeException(ThisAssembly.Strings.ArgumentIndexNotFound(index));

            SetValue(infos[index].Name, value);
        }

        /// <summary>
        /// Sets the value of the parameter with the given name.
        /// </summary>
        /// <typeparam name="T">Type of the value being set.</typeparam>
        /// <param name="name">Name of the argument being set.</param>
        /// <param name="value">Value of the argument.</param>
        /// <remarks>
        /// This method is equivalent to <see cref="SetValue(string, object?)"/> and 
        /// supports object initialization syntax so you can write code like:
        /// <code>
        /// var args = new ArgumentCollection(parameters) 
        /// {
        ///   { "message", "hello" },
        ///   { "count", 25 },
        ///   { "enabled", true },
        /// };
        /// </code>
        /// </remarks>
        public void Add<T>(string name, T? value) => SetValue(name, value);

        /// <summary>
        /// Sets the value of the parameter with the given index.
        /// </summary>
        /// <typeparam name="T">Type of the value being set.</typeparam>
        /// <param name="index">Index of the argument being set.</param>
        /// <param name="value">Value of the argument.</param>
        /// <remarks>
        /// This method is equivalent to <see cref="SetValue(int, object?)"/> and 
        /// supports object initialization syntax so you can write code like:
        /// <code>
        /// var args = new ArgumentCollection(parameters) 
        /// {
        ///   { 0, "hello" },
        ///   { 1, 25 },
        ///   { 2, true },
        /// };
        /// </code>
        /// </remarks>
        public void Add<T>(int index, T? value) => SetValue(index, value);

        /// <inheritdoc />
        public IEnumerator<ParameterInfo> GetEnumerator() => nameParams.Values.GetEnumerator();

        /// <inheritdoc />
        [DebuggerNonUserCode]
        [ExcludeFromCodeCoverage]
        public override string ToString() => string.Join(", ", infos.Select(ToString));

        [ExcludeFromCodeCoverage]
        [DebuggerNonUserCode]
        string ToString(ParameterInfo parameter) =>
            (parameter.IsOut ? parameter.ParameterType.GetFormattedName().Replace("ref ", "out ") : parameter.ParameterType.GetFormattedName()) +
            " " + parameter.Name +
            (parameter.IsOut ? "" :
                (": " + (!values.TryGetValue(parameter.Name, out var value) ? "null" :
                    ((IsString(parameter.ParameterType) && value != null) ? "\"" + value + "\"" :
                        // render boolean as lowercase to match C#
                        (value is bool b) ? b.ToString().ToLowerInvariant() :
                        value ?? "null"))
                )
            );

        void SetValues(object?[] values)
        {
            if (values != null && values.Length > 0)
            {
                if (values.Length != infos.Length)
                    throw new ArgumentException(ThisAssembly.Strings.ArgumentsMismatch);

                for (var i = 0; i < values.Length; i++)
                {
                    this.values[infos[i].Name] = values[i];
                }
            }
        }

        static bool IsString(Type type) => type == typeof(string) ||
            (type.IsByRef && type.HasElementType && type.GetElementType() == typeof(string));

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is ArgumentCollection collection &&
                infos.SequenceEqual(collection.infos) &&
                values.Count == collection.Count &&
                values.Values.SequenceEqual(collection.values.Values);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hash = new HashCode();
            foreach (var prm in infos)
                hash.Add(prm);

            foreach (var arg in values.Values)
                hash.Add(arg ?? NullValue);

            return hash.ToHashCode();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        [ExcludeFromCodeCoverage]
        [DebuggerNonUserCode]
        class DebugView
        {
            readonly ArgumentCollection arguments;
            public DebugView(ArgumentCollection arguments) => this.arguments = arguments;

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public KeyValuePair<ParameterInfo, object?>[] Items => arguments.infos
                // TODO: get value display, not the actual value??
                .Select(info => new KeyValuePair<ParameterInfo, object?>(info, arguments.GetValue(info.Name)))
                .ToArray();
        }
    }
}
