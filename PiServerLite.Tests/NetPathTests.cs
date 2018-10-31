using PiServerLite.Extensions;
using Xunit;

namespace PiServerLite.Tests
{
    public class NetPathTests
    {
        [Fact]
        public void SlashPrefix()
        {
            Assert.Equal("/path", NetPath.Combine("/", "path"));
        }

        [Fact]
        public void SlashSuffix()
        {
            Assert.Equal("path/", NetPath.Combine("path", "/"));
        }

        [Fact]
        public void JoinsNoSlash()
        {
            Assert.Equal("root/path", NetPath.Combine("root", "path"));
        }

        [Fact]
        public void JoinsLeftSlash()
        {
            Assert.Equal("root/path", NetPath.Combine("root/", "path"));
        }

        [Fact]
        public void JoinsRightSlash()
        {
            Assert.Equal("root/path", NetPath.Combine("root", "/path"));
        }

        [Fact]
        public void JoinsBothSlash()
        {
            Assert.Equal("root/path", NetPath.Combine("root/", "/path"));
        }

        [Fact]
        public void JoinsMultiple()
        {
            Assert.Equal("root/path1/path2/path3", NetPath.Combine("root", "path1", "path2", "path3"));
        }
    }
}
