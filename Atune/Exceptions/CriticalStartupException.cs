using System;

namespace Atune.Exceptions
{
    public class CriticalStartupException : Exception
    {
        public CriticalStartupException(string message) : base(message) { }
        public CriticalStartupException(string message, Exception inner)
            : base(message, inner) { }
    }
}
