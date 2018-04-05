using System;

namespace PiServerLite.Html.Blocks
{
    internal class ConditionalBlock
    {
        public HtmlEngine Engine {get;}


        public ConditionalBlock(HtmlEngine engine)
        {
            this.Engine = engine;
        }

        public void Process(string text, string tag, VariableCollection valueCollection, BlockResult result, ref int read_pos)
        {
            var _nesting = 0;
            int blockEnd_Start;
            int blockEnd_End;

            var _pos = read_pos;
            while (true) {
                var _found = HtmlEngine.FindAnyTag(text, "{{", "}}", _pos, out var _start, out var _end, out var _tag);
                if (!_found) throw new RenderingException("No #EndIf tag found!");

                _pos = _end;

                if (_tag.StartsWith("#If ", StringComparison.OrdinalIgnoreCase)) {
                    _nesting++;
                    continue;
                }

                if (string.Equals(_tag, "#EndIf", StringComparison.OrdinalIgnoreCase)) {
                    if (_nesting > 0) {
                        _nesting--;
                        continue;
                    }

                    blockEnd_Start = _start;
                    blockEnd_End = _end;
                    break;
                }
            }

            var blockText = text.Substring(read_pos, blockEnd_Start - read_pos);
            read_pos = blockEnd_End;

            _nesting = 0;
            blockEnd_Start = -1;
            blockEnd_End = -1;

            _pos = 0;
            while (true) {
                var _found = HtmlEngine.FindAnyTag(blockText, "{{", "}}", _pos, out var _start, out var _end, out var _tag);
                if (!_found) break;

                _pos = _end;

                if (_tag.StartsWith("#If ", StringComparison.OrdinalIgnoreCase)) {
                    _nesting++;
                    continue;
                }

                if (string.Equals(_tag, "#EndIf", StringComparison.OrdinalIgnoreCase)) {
                    if (_nesting > 0) {
                        _nesting--;
                        continue;
                    }

                    throw new RenderingException("Found too many #EndIf tags!");
                }

                if (string.Equals(_tag, "#Else", StringComparison.OrdinalIgnoreCase)) {
                    if (_nesting > 0) continue;

                    blockEnd_Start = _start;
                    blockEnd_End = _end;
                    break;
                }
            }

            string trueBlockText, falseBlockText;

            if (blockEnd_Start >= 0) {
                trueBlockText = blockText.Substring(0, blockEnd_Start);
                falseBlockText = blockText.Substring(blockEnd_End);
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

                if (valueCollection != null && valueCollection.TryGetValue(condition, out var item_value))
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
