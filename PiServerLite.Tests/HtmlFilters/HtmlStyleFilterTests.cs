using PiServerLite.Html;
using Xunit;

namespace PiServerLite.Tests.HtmlFilters
{
    public class HtmlStyleFilterTests
    {
        private readonly HtmlEngine engine;


        public HtmlStyleFilterTests()
        {
            var views = new ViewCollection {
                ["test-master"] = "<html>{{$style-content}}</html>",
                ["test-content"] = "{{#master test-master}}{{#Style}}<style>{{#EndStyle}}",
            };

            engine = new HtmlEngine(views);
        }

        [Fact]
        public void Renders_Content_Styles_In_Master_View()
        {
            var html = engine.Process("test-content");
            Assert.Equal("<html><style></html>", html);
        }
    }
}
