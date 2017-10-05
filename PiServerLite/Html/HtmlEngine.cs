using PiServerLite.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PiServerLite.Html
{
    internal class HtmlEngine
    {
        private readonly ViewCollection views;

        public string UrlRoot {get; set;}
        public TagNotFoundBehavior NotFoundBehavior {get; set;}
        public string MoustacheStartChars {get; set;}
        public string MoustacheStopChars {get; set;}


        public HtmlEngine(ViewCollection views)
        {
            this.views = views;

            NotFoundBehavior = TagNotFoundBehavior.Source;
            MoustacheStartChars = "{{";
            MoustacheStopChars = "}}";
        }

        public string Process(string text, object param)
        {
            var valueCollection = ToDictionary(param);

            // Process root text block
            var result = ProcessBlock(text, valueCollection);

            var scriptList = new List<string>(result.Scripts);
            var styleList = new List<string>(result.Styles);

            // Apply master-view chain
            while (!string.IsNullOrEmpty(result.MasterView)) {
                string masterText;
                if (!views.TryFind(result.MasterView, out masterText))
                    throw new ApplicationException($"Master view '{result.MasterView}' was not found!");

                valueCollection["master-content"] = result.Text;
                valueCollection["script-content"] = string.Join("\r\n", scriptList);
                valueCollection["style-content"] = string.Join("\r\n", styleList);

                result = ProcessBlock(masterText, valueCollection);

                scriptList.AddRange(result.Scripts);
                styleList.AddRange(result.Styles);
            }

            return result.Text;
        }

        private BlockResult ProcessBlock(string text, IDictionary<string, object> valueCollection)
        {
            var result = new BlockResult();
            if (string.IsNullOrEmpty(text))
                return result;

            var read_pos = 0;
            while (read_pos < text.Length) {
                string tag;
                int tagStart, tagEnd;
                if (!FindAnyTag(text, read_pos, out tagStart, out tagEnd, out tag)) break;

                result.Builder.Append(text, read_pos, tagStart - read_pos);
                read_pos = tagEnd;

                if (tag.StartsWith("#")) {
                    if (tag.StartsWith("#if ", StringComparison.OrdinalIgnoreCase))
                        ProcessConditionalBlock(text, tag, valueCollection, result, ref read_pos);

                    else if (tag.StartsWith("#master ", StringComparison.OrdinalIgnoreCase))
                        ProcessMasterTag(tag, result);

                    else if (tag.StartsWith("#url ", StringComparison.OrdinalIgnoreCase))
                        ProcessUrlTag(tag, result);

                    else if (string.Equals(tag, "#script", StringComparison.OrdinalIgnoreCase))
                        ProcessScriptBlock(text, valueCollection, result, ref read_pos);

                    else if (string.Equals(tag, "#style", StringComparison.OrdinalIgnoreCase))
                        ProcessStyleBlock(text, valueCollection, result, ref read_pos);

                    else if (tag.StartsWith("#each ", StringComparison.OrdinalIgnoreCase))
                        ProcessEachBlock(text, tag, valueCollection, result, ref read_pos);

                    else
                        result.Builder.Append(text.Substring(tagStart, tagEnd - tagStart));
                }
                else {
                    // Process Variable Tag
                    object item_value;
                    if (valueCollection != null && GetVariableValue(valueCollection, tag, out item_value)) {
                        result.Builder.Append(item_value);
                        continue;
                    }

                    switch (NotFoundBehavior) {
                        case TagNotFoundBehavior.Source:
                            result.Builder.Append(text.Substring(tagStart, tagEnd - tagStart));
                            break;
                    }
                }
            }

            if (read_pos < text.Length) {
                result.Builder.Append(text, read_pos, text.Length - read_pos);
            }

            return result;
        }

        private void ProcessConditionalBlock(string text, string tag, IDictionary<string, object> valueCollection, BlockResult result, ref int read_pos)
        {
            var blockEndStart = text.IndexOf("{{#endif}}", read_pos);
            if (blockEndStart < 0) return;

            var blockText = text.Substring(read_pos, blockEndStart - read_pos);
            read_pos = blockEndStart + 10;

            string trueBlockText, falseBlockText;

            var blockElseStart = blockText.IndexOf("{{#else}}");
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

                object item_value;
                if (valueCollection != null && GetVariableValue(valueCollection, condition, out item_value))
                    conditionResult = TruthyEngine.GetValue(item_value);

                if (condition.StartsWith("!"))
                    conditionResult = !conditionResult;
            }

            var conditionResultText = conditionResult ? trueBlockText : falseBlockText;
            var blockResult = ProcessBlock(conditionResultText, valueCollection);
            result.Builder.Append(blockResult.Builder);
        }

        private void ProcessMasterTag(string tag, BlockResult result)
        {
            var valueStart = tag.IndexOf(' ');
            if (valueStart < 0) return;

            result.MasterView = tag.Substring(valueStart+1).Trim();
        }

        private void ProcessUrlTag(string tag, BlockResult result)
        {
            var valueStart = tag.IndexOf(' ');
            if (valueStart < 0) return;

            var url = tag.Substring(valueStart+1).Trim();
            url = UrlRoot + url;

            result.Builder.Append(url);
        }

        private void ProcessScriptBlock(string text, IDictionary<string, object> valueCollection, BlockResult result, ref int readPos)
        {
            var endTag = "{{#endscript}}";
            var blockEndStart = text.IndexOf(endTag, readPos);
            if (blockEndStart < 0) return;

            var blockText = text.Substring(readPos, blockEndStart - readPos);
            readPos = blockEndStart + endTag.Length;

            var blockResult = ProcessBlock(blockText, valueCollection);
            result.Scripts.Add(blockResult.Text);
        }

        private void ProcessStyleBlock(string text, IDictionary<string, object> valueCollection, BlockResult result, ref int readPos)
        {
            var endTag = "{{#endstyle}}";
            var blockEndStart = text.IndexOf(endTag, readPos);
            if (blockEndStart < 0) return;

            var blockText = text.Substring(readPos, blockEndStart - readPos);
            readPos = blockEndStart + endTag.Length;

            var blockResult = ProcessBlock(blockText, valueCollection);
            result.Styles.Add(blockResult.Text);
        }

        private bool ProcessEachBlock(string text, string tag, IDictionary<string, object> valueCollection, BlockResult result, ref int readPos)
        {
            var endTag = "{{#endeach}}";
            var blockEndStart = text.IndexOf(endTag, readPos);
            if (blockEndStart < 0) return false;

            var blockText = text.Substring(readPos, blockEndStart - readPos);
            readPos = blockEndStart + endTag.Length;

            var statementStart = tag.IndexOf(' ');
            if (statementStart < 0) return false;

            var statement = tag.Substring(statementStart + 1).Trim();

            var x = statement.IndexOf('.');
            if (x < 0) return false;

            var objName = statement.Substring(0, x);
            var varName = statement.Substring(x + 1);

            object collectionObj;
            if (!valueCollection.TryGetValue(objName, out collectionObj))
                return false;

            var collection = collectionObj as IEnumerable<object>;
            if (collection == null) return false;

            foreach (var obj in collection) {
                var blockValues = new Dictionary<string, object>(valueCollection, StringComparer.OrdinalIgnoreCase);

                blockValues[varName] = obj;

                var blockResult = ProcessBlock(blockText, blockValues);

                result.Builder.Append(blockResult.Text);
                // TODO: Append other block result objects?
            }

            return true;
        }

        private bool FindAnyTag(string text, int startPos, out int tagStartPos, out int tagEndPos, out string tag)
        {
            var tagStart = text.IndexOf(MoustacheStartChars, startPos, StringComparison.Ordinal);
            if (tagStart < 0) {
                tagStartPos = tagEndPos = -1;
                tag = null;
                return false;
            }

            var tagEnd = text.IndexOf(MoustacheStopChars, tagStart, StringComparison.Ordinal);
            if (tagEnd < 0) {
                tagStartPos = tagEndPos = -1;
                tag = null;
                return false;
            }

            tagStartPos = tagStart;
            tagEndPos = tagEnd + MoustacheStopChars.Length;
            tag = text.Substring(tagStart + MoustacheStartChars.Length, tagEnd - tagStart - MoustacheStartChars.Length);
            return true;
        }

        protected virtual bool GetVariableValue(IDictionary<string, object> valueCollection, string key, out object value)
        {
            var keySegments = key.Split('.');
            var rootSegment = keySegments[0];

            if (!valueCollection.TryGetValue(rootSegment, out value))
                return false;

            for (var i = 1; i < keySegments.Length; i++) {
                if (value == null) return false;

                var segment = keySegments[i];

                var xType = value.GetType();

                var xField = xType.GetField(segment);
                if (xField != null) {
                    value = xField.GetValue(value);
                    continue;
                }

                var xProperty = xType.GetProperty(segment);
                if (xProperty != null) {
                    value = xProperty.GetValue(value);
                    continue;
                }

                return false;
            }

            return true;
        }

        private static IDictionary<string, object> ToDictionary(object parameters)
        {
            if (parameters == null) return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            var dictionary = parameters as IDictionary<string, object>;
            if (dictionary != null) return dictionary;

            return parameters.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Select(property => new KeyValuePair<string, object>(property.Name, property.GetValue(parameters)))
                .ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
        }
    }
}
