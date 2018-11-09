using System;

namespace PiServerLite.Html.Filters
{
    internal class HtmlViewFilter : IHtmlTagFilter
    {
        private readonly HtmlEngine engine;


        public HtmlViewFilter(HtmlEngine engine)
        {
            this.engine = engine;
        }

        public bool MatchesTag(string tag)
        {
            return tag.StartsWith("#view ", StringComparison.OrdinalIgnoreCase);
        }

        public void Process(string text, string tag, VariableCollection valueCollection, BlockResult result, ref int read_pos)
        {
            var valueStart = tag.IndexOf(' ');
            if (valueStart < 0) throw new RenderingException("View path is undefined!");

            var viewKey = tag.Substring(valueStart+1).Trim();

            var viewContent = engine.Process(viewKey, valueCollection);

            result.Builder.Append(viewContent);
        }
    }
}
