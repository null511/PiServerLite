using System;

namespace PiServerLite.Html.Filters
{
    internal class HtmlScriptFilter : IHtmlTagFilter
    {
        private readonly HtmlEngine engine;


        public HtmlScriptFilter(HtmlEngine engine)
        {
            this.engine = engine;
        }

        public bool MatchesTag(string tag)
        {
            return string.Equals(tag, "#Script", StringComparison.OrdinalIgnoreCase);
        }

        public void Process(string text, string tag, VariableCollection valueCollection, BlockResult result, ref int read_pos)
        {
            const string endTag = "{{#EndScript}}";
            var blockEndStart = text.IndexOf(endTag, read_pos, StringComparison.OrdinalIgnoreCase);
            if (blockEndStart < 0) throw new RenderingException("Ending tag '#EndScript' was not found!");

            var blockText = text.Substring(read_pos, blockEndStart - read_pos);
            read_pos = blockEndStart + endTag.Length;

            var blockResult = engine.ProcessBlock(blockText, valueCollection);
            result.Scripts.Add(blockResult.Text);
        }
    }
}
