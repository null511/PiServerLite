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

        public void Process(string text, string tag, VariableCollection valueCollection, BlockResult result, ref int readPos)
        {
            var _nesting = 0;
            var blockEnd_Start = 0;
            var blockEnd_End = 0;

            var _pos = readPos;
            while (true) {
                var _found = HtmlEngine.FindAnyTag(text, "{{", "}}", _pos, out var _start, out var _end, out var _tag);
                if (!_found) return;

                _pos = _end;

                if (_tag.StartsWith("#Each ", StringComparison.OrdinalIgnoreCase)) {
                    _nesting++;
                    continue;
                }

                if (string.Equals(_tag, "#EndEach", StringComparison.OrdinalIgnoreCase)) {
                    if (_nesting > 0) {
                        _nesting--;
                        continue;
                    }

                    blockEnd_Start = _start;
                    blockEnd_End = _end;
                    break;
                }
            }

            var blockText = text.Substring(readPos, blockEnd_Start - readPos);
            readPos = blockEnd_End;

            var statementStart = tag.IndexOf(' ');
            if (statementStart < 0) return;

            var statement = tag.Substring(statementStart + 1).Trim();

            var statementVarSplit = statement.LastIndexOf('.');
            if (statementVarSplit < 0) return;

            var objName = statement.Substring(0, statementVarSplit);
            var varName = statement.Substring(statementVarSplit + 1);

            if (!valueCollection.TryGetValue(objName, out var objValue)) return;

            // Exit if not a collection
            if (!(objValue is IEnumerable<object> collection)) return;

            foreach (var obj in collection) {
                var blockValues = new VariableCollection(valueCollection) {
                    [varName] = obj,
                };

                var blockResult = Engine.ProcessBlock(blockText, blockValues);

                result.Builder.Append(blockResult.Text);
                // TODO: Append other block result objects?
            }
        }
    }
}
