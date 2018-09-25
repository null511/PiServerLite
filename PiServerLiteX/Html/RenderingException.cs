using System;

namespace PiServerLite.Html
{
    public class RenderingException : ApplicationException
    {
        public RenderingException() : base() {}

        public RenderingException(string message) : base(message) {}
    }
}
