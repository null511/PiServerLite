using System.Collections.Generic;
using System.Text;

namespace PiServerLite.Html
{
    internal class BlockResult
    {
        public StringBuilder Builder {get;}
        public string MasterView {get; set;}
        public List<string> Scripts {get;}
        public List<string> Styles {get;}

        public string Text => Builder.ToString();


        public BlockResult()
        {
            Builder = new StringBuilder();

            Scripts = new List<string>();
            Styles = new List<string>();
        }
    }
}
