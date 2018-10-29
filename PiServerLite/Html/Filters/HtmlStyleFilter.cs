using System;

namespace PiServerLite.Html.Filters
{
    internal class HtmlStyleFilter : IHtmlTagFilter
    {
        private readonly HtmlEngine engine;


        public HtmlStyleFilter(HtmlEngine engine)
        {
            this.engine = engine;
        }

        public bool MatchesTag(string tag)
        {
            return string.Equals(tag, "#Style", StringComparison.OrdinalIgnoreCase);
        }

        public void Process(string text, string tag, VariableCollection valueCollection, BlockResult result, ref int read_pos)
        {
            const string endTag = "{{#EndStyle}}";
            var blockEndStart = text.IndexOf(endTag, read_pos, StringComparison.OrdinalIgnoreCase);
            if (blockEndStart < 0) throw new RenderingException("Ending tag '#EndStyle' was not found!");

            var blockText = text.Substring(read_pos, blockEndStart - read_pos);
            read_pos = blockEndStart + endTag.Length;

            var blockResult = engine.ProcessBlock(blockText, valueCollection);
            result.Styles.Add(blockResult.Text);
        }
    }
}
