// OvfParameterModifier/OvfParameterModifierException.cs

using System;

namespace OvfParameterModifier {
    /// <summary>
    /// Represents errors that occur during the OVF parameter modification process.
    /// </summary>
    public class OvfParameterModifierException : Exception {
        public OvfParameterModifierException() { }

        public OvfParameterModifierException(string message)
            : base(message) { }

        public OvfParameterModifierException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}