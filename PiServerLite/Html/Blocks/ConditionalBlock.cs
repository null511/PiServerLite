using System;
using System.Collections.Generic;

namespace PiServerLite.Html.Blocks
{
    internal class ConditionalBlock
    {
        public HtmlEngine Engine {get;}


        public ConditionalBlock(HtmlEngine engine)
        {
            this.Engine = engine;
        }

        public void Process(string text, string tag, IDictionary<string, object> valueCollection, BlockResult result, ref int read_pos)
        {
            var blockEndStart = text.IndexOf("{{#endif}}", read_pos, StringComparison.OrdinalIgnoreCase);
            if (blockEndStart < 0) return;

            var blockText = text.Substring(read_pos, blockEndStart - read_pos);
            read_pos = blockEndStart + 10;

            string trueBlockText, falseBlockText;

            var blockElseStart = blockText.IndexOf("{{#else}}", StringComparison.OrdinalIgnoreCase);
            if (blockElseStart >= 0) {
                trueBlockText = blockText.Substring(0, blockElseStart);
                falseBlockText = blockText.Substring(blockElseStart + 9);
            }
            else {
                trueBlockText = blockText;
                falseBlockText = string.Empty;
            }

            bool conditionResult = false;

            var conditionStart = tag.IndexOf(' ');
            if (conditionStart >= 0) {
                var condition = tag.Substring(conditionStart+1);

                var invert = condition.StartsWith("!");
                if (invert) condition = condition.Substring(1);

                object item_value;
                if (valueCollection != null && Engine.GetVariableValue(valueCollection, condition, out item_value))
                    conditionResult = TruthyEngine.GetValue(item_value);

                if (invert)
                    conditionResult = !conditionResult;
            }

            var conditionResultText = conditionResult ? trueBlockText : falseBlockText;
            var blockResult = Engine.ProcessBlock(conditionResultText, valueCollection);
            result.Builder.Append(blockResult.Builder);
        }
    }
}
