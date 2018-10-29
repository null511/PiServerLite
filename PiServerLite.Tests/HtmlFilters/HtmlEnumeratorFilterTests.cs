using PiServerLite.Html;
using Xunit;

namespace PiServerLite.Tests.HtmlFilters
{
    public class HtmlEnumeratorFilterTests
    {
        private const string viewKey = "test";

        private readonly HtmlEngine engine;


        public HtmlEnumeratorFilterTests()
        {
            var views = new ViewCollection {
                [viewKey] = "<{{#Each items.item}}{{item}}{{#EndEach}}>",
            };

            engine = new HtmlEngine(views);
        }

        [Fact]
        public void Insert_0_Items()
        {
            var items = new string[0];
            var html = engine.Process(viewKey, new {items});
            Assert.Equal("<>", html);
        }

        [Fact]
        public void Insert_1_Items()
        {
            var items = new[] {"x"};
            var html = engine.Process(viewKey, new {items});
            Assert.Equal("<x>", html);
        }

        [Fact]
        public void Insert_3_Items()
        {
            var items = new[] {"a", "b", "c"};
            var html = engine.Process(viewKey, new {items});
            Assert.Equal("<abc>", html);
        }
    }
}
