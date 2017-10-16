using PiServerLite.Html.Blocks;
using PiServerLite.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace PiServerLite.Html
{
    internal class HtmlEngine
    {
        public event EventHandler<TagNotFoundEventArgs> VariableNotFound;

        private readonly ConditionalBlock conditionalBlock;
        private readonly EachBlock eachBlock;
        private readonly ViewCollection views;

        public string UrlRoot {get; set;}
        public bool RemoveComments {get; set;}
        public VariableNotFoundBehavior VariableNotFoundBehavior {get; set;}


        public HtmlEngine(ViewCollection views)
        {
            this.views = views;

            RemoveComments = true;
            VariableNotFoundBehavior = VariableNotFoundBehavior.Source;

            conditionalBlock = new ConditionalBlock(this);
            eachBlock = new EachBlock(this);
        }

        public string Process(string text, object param)
        {
            var valueCollection = ObjectExtensions.ToDictionary(param);

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

        public BlockResult ProcessBlock(string text, IDictionary<string, object> valueCollection)
        {
            var result = new BlockResult();
            if (string.IsNullOrEmpty(text))
                return result;

            if (RemoveComments)
                text = RemoveTextComments(text);

            var read_pos = 0;
            while (read_pos < text.Length) {
                string tag;
                int tagStart, tagEnd;
                if (!FindAnyTag(text, "{{", "}}", read_pos, out tagStart, out tagEnd, out tag)) break;

                result.Builder.Append(text, read_pos, tagStart - read_pos);
                read_pos = tagEnd;

                if (tag.StartsWith("#")) {
                    if (tag.StartsWith("#if ", StringComparison.OrdinalIgnoreCase))
                        conditionalBlock.Process(text, tag, valueCollection, result, ref read_pos);

                    else if (tag.StartsWith("#master ", StringComparison.OrdinalIgnoreCase))
                        ProcessMasterTag(tag, result);

                    else if (tag.StartsWith("#url ", StringComparison.OrdinalIgnoreCase))
                        ProcessUrlTag(tag, result);

                    else if (string.Equals(tag, "#script", StringComparison.OrdinalIgnoreCase))
                        ProcessScriptBlock(text, valueCollection, result, ref read_pos);

                    else if (string.Equals(tag, "#style", StringComparison.OrdinalIgnoreCase))
                        ProcessStyleBlock(text, valueCollection, result, ref read_pos);

                    else if (tag.StartsWith("#each ", StringComparison.OrdinalIgnoreCase))
                        eachBlock.Process(text, tag, valueCollection, result, ref read_pos);

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

                    var sourceText = text.Substring(tagStart, tagEnd - tagStart);
                    var varResult = OnVariableNotFound(tag, sourceText);

                    if (!string.IsNullOrEmpty(varResult))
                        result.Builder.Append(varResult);
                }
            }

            if (read_pos < text.Length) {
                result.Builder.Append(text, read_pos, text.Length - read_pos);
            }

            return result;
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
            url = NetPath.Combine(UrlRoot, url);

            result.Builder.Append(url);
        }

        private void ProcessScriptBlock(string text, IDictionary<string, object> valueCollection, BlockResult result, ref int readPos)
        {
            var endTag = "{{#endscript}}";
            var blockEndStart = text.IndexOf(endTag, readPos, StringComparison.OrdinalIgnoreCase);
            if (blockEndStart < 0) return;

            var blockText = text.Substring(readPos, blockEndStart - readPos);
            readPos = blockEndStart + endTag.Length;

            var blockResult = ProcessBlock(blockText, valueCollection);
            result.Scripts.Add(blockResult.Text);
        }

        private void ProcessStyleBlock(string text, IDictionary<string, object> valueCollection, BlockResult result, ref int readPos)
        {
            var endTag = "{{#endstyle}}";
            var blockEndStart = text.IndexOf(endTag, readPos, StringComparison.OrdinalIgnoreCase);
            if (blockEndStart < 0) return;

            var blockText = text.Substring(readPos, blockEndStart - readPos);
            readPos = blockEndStart + endTag.Length;

            var blockResult = ProcessBlock(blockText, valueCollection);
            result.Styles.Add(blockResult.Text);
        }

        public virtual bool GetVariableValue(IDictionary<string, object> valueCollection, string key, out object value)
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

        protected virtual string OnVariableNotFound(string tag, string sourceText)
        {
            var e = new TagNotFoundEventArgs(tag);

            try {
                VariableNotFound?.Invoke(this, e);
            }
            catch {}

            if (e.Handled) return e.Result;

            switch (VariableNotFoundBehavior) {
                case VariableNotFoundBehavior.Source:
                    return sourceText;
                case VariableNotFoundBehavior.Empty:
                default:
                    return null;
            }
        }

        private static bool FindAnyTag(string text, string tagStartChars, string tagStopChars, int startPos, out int tagStartPos, out int tagEndPos, out string tag)
        {
            var tagStart = text.IndexOf(tagStartChars, startPos, StringComparison.Ordinal);
            if (tagStart < 0) {
                tagStartPos = tagEndPos = -1;
                tag = null;
                return false;
            }

            var tagEnd = text.IndexOf(tagStopChars, tagStart, StringComparison.Ordinal);
            if (tagEnd < 0) {
                tagStartPos = tagEndPos = -1;
                tag = null;
                return false;
            }

            tagStartPos = tagStart;
            tagEndPos = tagEnd + tagStopChars.Length;
            tag = text.Substring(tagStart + tagStartChars.Length, tagEnd - tagStart - tagStartChars.Length);
            return true;
        }

        private static string RemoveTextComments(string text)
        {
            var result = new StringBuilder();

            var read_pos = 0;
            while (read_pos < text.Length) {
                string tag;
                int tagStart, tagEnd;
                if (!FindAnyTag(text, "<!--", "-->", read_pos, out tagStart, out tagEnd, out tag)) break;

                result.Append(text, read_pos, tagStart - read_pos);
                read_pos = tagEnd;
            }

            if (read_pos < text.Length)
                result.Append(text, read_pos, text.Length - read_pos);

            return result.ToString();
        }
    }
}
