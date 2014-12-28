using System;
using Xunit;

namespace Nancy.Session.Tests
{
    public class DynamoDbSessionRecordTests
    {
        [Fact]
        [Trait("Category", "Unit tests")]
        public void Should_Correctly_Determine_Expiry_Status()
        {
            var record = new DynamoDbSessionRecord("abc123", "may application", DateTime.UtcNow.AddSeconds(-1), new Session(), 
                DateTime.UtcNow);

            Assert.True(record.HasExpired);
        }

        [Fact]
        [Trait("Category", "Unit tests")]
        public void Should_Throw_For_Invalid_SessionId()
        {
            Assert.Throws<ArgumentNullException>(
                () => new DynamoDbSessionRecord("", "asdf", DateTime.UtcNow, new Session(), DateTime.UtcNow));
        }

        [Fact]
        [Trait("Category", "Unit tests")]
        public void Should_Throw_For_Invalid_ApplicationName()
        {
            Assert.Throws<ArgumentNullException>(
                () => new DynamoDbSessionRecord("asdf", "", DateTime.UtcNow, new Session(), DateTime.UtcNow));
        }
    }
}