using System;

namespace PiServerLite.Html
{
    class TagNotFoundEventArgs : EventArgs
    {
        public string Tag {get;}
        public string Result {get; set;}
        public bool Handled {get; set;}

        public TagNotFoundEventArgs(string tag)
        {
            this.Tag = tag;
        }
    }
}
