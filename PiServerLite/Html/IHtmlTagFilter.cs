namespace PiServerLite.Html
{
    public interface IHtmlTagFilter
    {
        bool MatchesTag(string tag);
        void Process(string text, string tag, VariableCollection valueCollection, BlockResult result, ref int read_pos);
    }
}
