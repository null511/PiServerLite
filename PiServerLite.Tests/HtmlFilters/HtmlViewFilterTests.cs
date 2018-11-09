using PiServerLite.Html;
using Xunit;

namespace PiServerLite.Tests.HtmlFilters
{
    public class HtmlViewFilterTests
    {
        private readonly HtmlEngine engine;


        public HtmlViewFilterTests()
        {
            var views = new ViewCollection {
                ["test"] = "<html>{{#view test-inner}}</html>",
                ["test-inner"] = "Hello World!",
            };

            engine = new HtmlEngine(views);
        }

        [Fact]
        public void Inserts_Child_View()
        {
            var html = engine.Process("test");
            Assert.Equal("<html>Hello World!</html>", html);
        }
    }
}
