using System;
using Robust.Client.UserInterface;

namespace Robust.Client.ViewVariables
{
    /// <summary>
    ///     An editor for the value of a property.
    /// </summary>
    public abstract class VVPropEditor
    {
        /// <summary>
        ///     The string to display when a given object is null.
        /// </summary>
        protected const string NullString = "null";

        /// <summary>
        ///     Invoked when the value was changed.
        /// </summary>
        internal event Action<object?, bool>? OnValueChanged;

        protected bool ReadOnly { get; private set; }

        /// <summary>
        ///     True if the underlying field can be nullable.
        /// </summary>
        protected bool Nullable { get; private set; }

        public Control Initialize(object? value, bool readOnly, bool nullable = false)
        {
            ReadOnly = readOnly;
            Nullable = nullable;
            return MakeUI(value);
        }

        protected abstract Control MakeUI(object? value);

        protected void ValueChanged(object? newValue, bool reinterpretValue = false)
        {
            OnValueChanged?.Invoke(newValue, reinterpretValue);
        }

        public virtual void WireNetworkSelector(uint sessionId, object[] selectorChain)
        {

        }

        /// <summary>
        ///     Checks if the string passed should be treated as a null string.
        /// </summary>
        protected virtual bool IsNullString(string value)
        {
            return string.Equals(value, NullString, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
