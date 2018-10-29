using PiServerLite.Html.Filters;
using System;
using System.Collections.Generic;
using System.Text;

namespace PiServerLite.Html
{
    public class HtmlEngine
    {
        public event EventHandler<HtmlTagNotFoundEventArgs> VariableNotFound;

        private readonly ViewCollection views;

        public string UrlRoot {get; set;}
        public bool RemoveComments {get; set;}
        public VariableNotFoundBehavior VariableNotFoundBehavior {get; set;}
        public List<IHtmlTagFilter> Filters {get;}


        public HtmlEngine(ViewCollection views)
        {
            this.views = views;

            RemoveComments = true;
            VariableNotFoundBehavior = VariableNotFoundBehavior.Source;

            Filters = new List<IHtmlTagFilter> {
                new HtmlConditionalFilter(this),
                new HtmlEachFilter(this),
                new HtmlUrlFilter(this),
                new HtmlScriptFilter(this),
                new HtmlStyleFilter(this),
                new HtmlMasterViewFilter(),
            };
        }

        public string Process(string viewKey, object param = null)
        {
            if (!views.TryFind(viewKey, out var viewContent))
                throw new ApplicationException($"View '{viewKey}' was not found!");

            var valueCollection = new VariableCollection(param);

            // Process root text block
            var result = ProcessBlock(viewContent, valueCollection);

            var scriptList = new List<string>(result.Scripts);
            var styleList = new List<string>(result.Styles);

            // Apply master-view chain
            while (!string.IsNullOrEmpty(result.MasterView)) {
                if (!views.TryFind(result.MasterView, out var masterText))
                    throw new RenderingException($"Master view '{result.MasterView}' was not found!");

                valueCollection["master-content"] = result.Text;
                valueCollection["script-content"] = string.Join("\r\n", scriptList);
                valueCollection["style-content"] = string.Join("\r\n", styleList);

                result = ProcessBlock(masterText, valueCollection);

                scriptList.AddRange(result.Scripts);
                styleList.AddRange(result.Styles);
            }

            return result.Text;
        }

        public BlockResult ProcessBlock(string text, VariableCollection valueCollection)
        {
            var result = new BlockResult();
            if (string.IsNullOrEmpty(text))
                return result;

            if (RemoveComments)
                text = RemoveTextComments(text);

            var read_pos = 0;
            while (read_pos < text.Length) {
                if (!FindAnyTag(text, "{{", "}}", read_pos, out var tagStart, out var tagEnd, out var tag)) break;

                result.Builder.Append(text, read_pos, tagStart - read_pos);
                read_pos = tagEnd;

                var isProcessed = false;
                foreach (var filter in Filters) {
                    if (filter.MatchesTag(tag)) {
                        filter.Process(text, tag, valueCollection, result, ref read_pos);
                        isProcessed = true;
                        break;
                    }
                }

                if (!isProcessed) {
                    // Process Variable Tag
                    if (valueCollection != null && valueCollection.TryGetFormattedValue(tag, out var item_value)) {
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

        protected virtual string OnVariableNotFound(string tag, string sourceText)
        {
            var e = new HtmlTagNotFoundEventArgs(tag);

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

        internal static bool FindAnyTag(string text, string tagStartChars, string tagStopChars, int startPos, out int tagStartPos, out int tagEndPos, out string tag)
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
                if (!FindAnyTag(text, "<!--", "-->", read_pos, out var tagStart, out var tagEnd, out _)) break;

                result.Append(text, read_pos, tagStart - read_pos);
                read_pos = tagEnd;
            }

            if (read_pos < text.Length)
                result.Append(text, read_pos, text.Length - read_pos);

            return result.ToString();
        }
    }
}
