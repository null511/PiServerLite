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
            var blockEnd_Start = -1;
            var blockEnd_End = -1;

            var _pos = readPos;
            while (true) {
                var _found = HtmlEngine.FindAnyTag(text, "{{", "}}", _pos, out var _start, out var _end, out var _tag);
                if (!_found) throw new RenderingException("No #EndEach tag found!");

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
            if (statementStart < 0) throw new RenderingException("#Each tag is missing statement!");

            var statement = tag.Substring(statementStart + 1).Trim();

            var statementVarSplit = statement.LastIndexOf('.');
            if (statementVarSplit < 0) throw new RenderingException("#Each tag statement is missing item alias!");

            var objName = statement.Substring(0, statementVarSplit);
            var varName = statement.Substring(statementVarSplit + 1);

            if (!valueCollection.TryGetValue(objName, out var objValue))
                throw new RenderingException($"Variable '{objName}' not found!");

            if (!(objValue is IEnumerable<object> collection))
                throw new RenderingException($"Variable '{objName}' is not a collection!");

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
