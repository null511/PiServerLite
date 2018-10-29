using System;

namespace PiServerLite.Html
{
    public class HtmlTagNotFoundEventArgs : EventArgs
    {
        public string Tag {get;}
        public string Result {get; set;}
        public bool Handled {get; set;}


        public HtmlTagNotFoundEventArgs(string tag)
        {
            this.Tag = tag;
        }
    }
}
