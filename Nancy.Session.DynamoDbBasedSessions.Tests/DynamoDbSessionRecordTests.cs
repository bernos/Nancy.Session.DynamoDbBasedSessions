using System;
using Xunit;

namespace Nancy.Session.Tests
{
    public class DynamoDbSessionRecordTests
    {
        [Fact]
        [Trait("Category", "Unit Tests")]
        public void Should_Correctly_Determine_Expiry_Status()
        {
            var record = new DynamoDbSessionRecord(Guid.NewGuid(), "may application", DateTime.UtcNow.AddSeconds(-1), new Session(), 
                DateTime.UtcNow);

            Assert.True(record.HasExpired);
        }

        [Fact]
        [Trait("Category", "Unit Tests")]
        public void Should_Throw_For_Invalid_SessionId()
        {
            Assert.Throws<ArgumentNullException>(
                () => new DynamoDbSessionRecord(Guid.Empty, "asdf", DateTime.UtcNow, new Session(), DateTime.UtcNow));
        }

        [Fact]
        [Trait("Category", "Unit Tests")]
        public void Should_Throw_For_Invalid_ApplicationName()
        {
            Assert.Throws<ArgumentNullException>(
                () => new DynamoDbSessionRecord(Guid.NewGuid(), "", DateTime.UtcNow, new Session(), DateTime.UtcNow));
        }
    }
}