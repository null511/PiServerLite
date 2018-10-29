using PiServerLite.Html;
using Xunit;

namespace PiServerLite.Tests.HtmlFilters
{
    public class HtmlScriptFilterTests
    {
        private readonly HtmlEngine engine;


        public HtmlScriptFilterTests()
        {
            var views = new ViewCollection {
                ["test-master"] = "<html>{{$script-content}}</html>",
                ["test-content"] = "{{#master test-master}}{{#Script}}<script>{{#EndScript}}",
            };

            engine = new HtmlEngine(views);
        }

        [Fact]
        public void Renders_Content_Scripts_In_Master_View()
        {
            var html = engine.Process("test-content");
            Assert.Equal("<html><script></html>", html);
        }
    }
}
