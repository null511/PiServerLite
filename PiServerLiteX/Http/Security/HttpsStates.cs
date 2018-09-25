namespace PiServerLite.Http.Security
{
    /// <summary>
    /// Describes how HTTPS policies are applied.
    /// </summary>
    public enum HttpsStates
    {
        /// <summary>
        /// Disable HTTPS support. HTTP only.
        /// </summary>
        Disabled = 0,

        /// <summary>
        /// Enable HTTP and HTTPS support.
        /// </summary>
        Enabled = 1,

        /// <summary>
        /// Enable HTTPS support, and redirect HTTP requests to HTTPS.
        /// </summary>
        Forced = 2,
    }
}
