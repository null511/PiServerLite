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
        public Uri ListenUri {get; set;}

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


        public HttpReceiverContext()
        {
            Views = new ViewCollection();
            MimeTypes = MimeTypeDictionary.CreateDefault();
            ContentDirectories = new List<ContentDirectory>();
        }
    }
}
