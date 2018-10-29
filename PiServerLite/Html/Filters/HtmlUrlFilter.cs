using PiServerLite.Extensions;
using System;

namespace PiServerLite.Html.Filters
{
    internal class HtmlUrlFilter : IHtmlTagFilter
    {
        private readonly HtmlEngine engine;


        public HtmlUrlFilter(HtmlEngine engine)
        {
            this.engine = engine;
        }

        public bool MatchesTag(string tag)
        {
            return tag.StartsWith("#url ", StringComparison.OrdinalIgnoreCase);
        }

        public void Process(string text, string tag, VariableCollection valueCollection, BlockResult result, ref int read_pos)
        {
            var valueStart = tag.IndexOf(' ');
            if (valueStart < 0) throw new RenderingException("Url path is undefined!");

            var url = tag.Substring(valueStart+1).Trim();
            url = NetPath.Combine(engine.UrlRoot, url);

            result.Builder.Append(url);
        }
    }
}
