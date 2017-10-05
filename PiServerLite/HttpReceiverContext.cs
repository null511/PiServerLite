using PiServerLite.Http;
using System.Collections.Generic;

namespace PiServerLite
{
    public class HttpReceiverContext
    {
        public string DefaultRoute {get; set;}
        public string UrlRoot {get; set;}
        public ViewCollection Views {get; set;}
        public MimeTypeDictionary MimeTypes {get; set;}
        public List<ContentDirectory> ContentDirectories {get; set;}


        public HttpReceiverContext()
        {
            Views = new ViewCollection();
            MimeTypes = MimeTypeDictionary.CreateDefault();
            ContentDirectories = new List<ContentDirectory>();
        }
    }
}
