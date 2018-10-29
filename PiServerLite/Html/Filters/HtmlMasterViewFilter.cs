using System;

namespace PiServerLite.Html.Filters
{
    internal class HtmlMasterViewFilter : IHtmlTagFilter
    {
        public bool MatchesTag(string tag)
        {
            return tag.StartsWith("#master ", StringComparison.OrdinalIgnoreCase);
        }

        public void Process(string text, string tag, VariableCollection valueCollection, BlockResult result, ref int read_pos)
        {
            var valueStart = tag.IndexOf(' ');
            if (valueStart < 0) throw new RenderingException("Master view path is undefined!");

            result.MasterView = tag.Substring(valueStart+1).Trim();
        }
    }
}
