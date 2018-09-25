using System;

namespace PiServerLite.Http.Handlers
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class HttpFilterAttribute : Attribute
    {
        public string FunctionName {get; set;}
        public Func<IHttpHandler, HttpHandlerResult> Function {get; set;}
        public HttpFilterEvents Events {get; set;}


        public HttpFilterAttribute()
        {
            FunctionName = "OnFilter";
            Events = HttpFilterEvents.None;
        }

        public HttpHandlerResult RunBefore(IHttpHandler httpHandler, HttpFilterEvents eventType)
        {
            return Run(httpHandler, HttpFilterEvents.Before);
        }

        public HttpHandlerResult RunAfter(IHttpHandler httpHandler, HttpHandlerResult result)
        {
            return Run(httpHandler, HttpFilterEvents.After);
        }

        public HttpHandlerResult Run(IHttpHandler httpHandler, HttpFilterEvents eventType)
        {
            if (string.IsNullOrEmpty(FunctionName))
                throw new ApplicationException("FunctionName is undefined!");

            if (Events == HttpFilterEvents.None) return null;
            if (!Events.HasFlag(eventType)) return null;

            if (Function != null) {
                return Function.Invoke(httpHandler);
            }

            var method = httpHandler.GetType().GetMethod(FunctionName);
            if (method == null) throw new ApplicationException($"HttpFilter method '{FunctionName}' not found!");

            var @params = new object[] {httpHandler};
            return (HttpHandlerResult)method.Invoke(httpHandler, @params);
        }
    }

    [Flags]
    public enum HttpFilterEvents
    {
        None,
        Before,
        After,
    }
}
