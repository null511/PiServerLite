using Xunit;

namespace PiServerLite.Tests
{
    public class ViewCollectionTests
    {
        [Fact]
        public void CanLookupViewFromManual()
        {
            const string viewName = "TEST";
            const string testString = "Hello World!";

            var collection = new ViewCollection();
            collection.Add(viewName, () => testString);

            Assert.True(collection.TryFind(viewName, out var viewContent));
            Assert.Equal(testString, viewContent);
        }
    }
}
