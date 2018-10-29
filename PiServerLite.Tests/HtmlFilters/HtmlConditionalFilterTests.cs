using PiServerLite.Html;
using Xunit;

namespace PiServerLite.Tests.HtmlFilters
{
    public class HtmlConditionalFilterTests
    {
        private const string viewKey = "test";

        private readonly HtmlEngine engine;


        public HtmlConditionalFilterTests()
        {
            var views = new ViewCollection {
                [viewKey] = "Hello{{#If value}} World{{#EndIf}}!",
            };

            engine = new HtmlEngine(views);
        }

        [Fact]
        public void Inserts_True_Condition()
        {
            var html = engine.Process(viewKey, new {value = true});
            Assert.Equal("Hello World!", html);
        }

        [Fact]
        public void Does_Not_Insert_False_Condition()
        {
            var html = engine.Process(viewKey, new {value = false});
            Assert.Equal("Hello!", html);
        }
    }
}
