using System;
using System.IO;

namespace PiServerLite.Http.Handlers
{
    public class ResponseBodyBuilder
    {
        internal Action<long> SetLengthAction {get; set;}
        internal Func<Stream> StreamFunc {get; set;}


        public void SetLength(long value)
        {
            SetLengthAction?.Invoke(value);
        }

        public Stream GetStream()
        {
            return StreamFunc?.Invoke();
        }
    }
}
