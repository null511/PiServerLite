using System;
using System.Collections.Generic;

namespace PiServerLite.Html.Blocks
{
    internal class EachBlock
    {
        public HtmlEngine Engine {get;}


        public EachBlock(HtmlEngine engine)
        {
            this.Engine = engine;
        }

        public void Process(string text, string tag, IDictionary<string, object> valueCollection, BlockResult result, ref int readPos)
        {
            var endTag = "{{#endeach}}";
            var blockEndStart = text.IndexOf(endTag, readPos, StringComparison.OrdinalIgnoreCase);
            if (blockEndStart < 0) return;

            var blockText = text.Substring(readPos, blockEndStart - readPos);
            readPos = blockEndStart + endTag.Length;

            var statementStart = tag.IndexOf(' ');
            if (statementStart < 0) return;

            var statement = tag.Substring(statementStart + 1).Trim();

            var x = statement.IndexOf('.');
            if (x < 0) return;

            var objName = statement.Substring(0, x);
            var varName = statement.Substring(x + 1);

            object collectionObj;
            if (!valueCollection.TryGetValue(objName, out collectionObj))
                return;

            var collection = collectionObj as IEnumerable<object>;
            if (collection == null) return;

            foreach (var obj in collection) {
                var blockValues = new Dictionary<string, object>(valueCollection, StringComparer.OrdinalIgnoreCase) {
                    [varName] = obj,
                };

                var blockResult = Engine.ProcessBlock(blockText, blockValues);

                result.Builder.Append(blockResult.Text);
                // TODO: Append other block result objects?
            }
        }
    }
}
