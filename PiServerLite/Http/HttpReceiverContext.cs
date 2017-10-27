using PiServerLite.Http.Content;
using PiServerLite.Http.Security;
using System;
using System.Collections.Generic;

namespace PiServerLite.Http
{
    /// <summary>
    /// Provides configuration information used to construct
    /// an instance of <see cref="HttpReceiver"/>.
    /// </summary>
    public class HttpReceiverContext
    {
        /// <summary>
        /// The full external URI used to access the receiver.
        /// ie: 'http://localhost:8080/piServer/'
        /// </summary>
        //public Uri ListenUri {get; set;}

        public string ListenerPath {get; set;}

        /// <summary>
        /// A collection of string-based view resources.
        /// </summary>
        public ViewCollection Views {get; set;}

        /// <summary>
        /// A mapping of file-extensions to mime-types for <see cref="ContentDirectory"/>.
        /// </summary>
        public MimeTypeDictionary MimeTypes {get; set;}

        /// <summary>
        /// A list of file-system directories that can provide raw content.
        /// The returned mime-type is determined using <seealso cref="MimeTypes"/>.
        /// </summary>
        /// <remarks>Mime-Type is determined using <seealso cref="MimeTypes"/>.</remarks>
        public List<ContentDirectory> ContentDirectories {get; set;}

        /// <summary>
        /// Optional implementation of <see cref="ISecurityManager"/> for authenticating
        /// requests to resource marked by the <see cref="SecureAttribute"/>.
        /// </summary>
        public ISecurityManager SecurityMgr {get; set;}

        /// <summary>
        /// When enabled, HTTP requests will be redirected to HTTPS.
        /// </summary>
        public HttpsStates Https {get; set;}

        /// <summary>
        /// Port to use for HTTP communication. Default is 80.
        /// </summary>
        public int HttpPort {get; set;}

        /// <summary>
        /// Port to use for HTTPS communication. Default is 443.
        /// </summary>
        public int HttpsPort {get; set;}


        /// <summary>
        /// Constructs a new instance of <see cref="HttpReceiverContext"/>
        /// with the default settings.
        /// </summary>
        public HttpReceiverContext()
        {
            HttpPort = 80;
            HttpsPort = 443;

            Views = new ViewCollection();
            MimeTypes = MimeTypeDictionary.CreateDefault();
            ContentDirectories = new List<ContentDirectory>();
        }
    }
}
