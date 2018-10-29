using PiServerLite.Html;
using Xunit;

namespace PiServerLite.Tests.HtmlFilters
{
    public class HtmlMasterViewFilterTests
    {
        private readonly HtmlEngine engine;


        public HtmlMasterViewFilterTests()
        {
            var views = new ViewCollection {
                ["test-master"] = "<html>{{master-content}}</html>",
                ["test-content"] = "{{#master test-master}}Hello World!",
            };

            engine = new HtmlEngine(views);
        }

        [Fact]
        public void Wraps_In_Master_View()
        {
            var html = engine.Process("test-content");
            Assert.Equal("<html>Hello World!</html>", html);
        }
    }
}
