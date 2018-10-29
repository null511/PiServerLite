using PiServerLite.Html;
using Xunit;

namespace PiServerLite.Tests.HtmlFilters
{
    public class HtmlUrlFilterTests
    {
        private const string viewKey = "test";

        private readonly HtmlEngine engine;


        public HtmlUrlFilterTests()
        {
            var views = new ViewCollection {
                [viewKey] = "<a href=\"{{#Url /test-path}}\">link</a>",
            };

            engine = new HtmlEngine(views) {
                UrlRoot = "server-name",
            };
        }

        [Fact]
        public void Inserts_Url()
        {
            var html = engine.Process(viewKey, new {value = true});
            Assert.Equal("<a href=\"server-name/test-path\">link</a>", html);
        }
    }
}
