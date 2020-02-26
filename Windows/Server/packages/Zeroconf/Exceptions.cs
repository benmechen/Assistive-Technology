using System;
namespace Zeroconf
{
    public class IncomingDecodeError : Exception
    {
        public IncomingDecodeError(string message) : base(message)
        {
        }
    };
    public class NonUniqueNameException : Exception
    {
        public NonUniqueNameException(string message) : base(message)
        {
        }
    };
    public class NamePartTooLongException : Exception
    {
        public NamePartTooLongException(string message) : base(message)
        {
        }
    };
    public class AbstractMethodException : Exception
    {
        public AbstractMethodException(string message) : base(message)
        {
        }
    };
    public class BadTypeInNameException : Exception
    {
        public BadTypeInNameException(string message) : base(message)
        {
        }
    };
}
