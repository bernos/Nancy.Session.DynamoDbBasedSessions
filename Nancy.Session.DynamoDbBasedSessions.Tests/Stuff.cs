using Amazon;
using Nancy.DynamoDbBasedSessions;
using Xunit;

namespace Nancy.Session.Tests
{
    public class Stuff
    {
        [Fact]
        public void Should_Create_Table()
        {
            var config = new DynamoDbBasedSessionsConfiguration("TestApp")
            {
                RegionEndpoint = RegionEndpoint.APSoutheast2
            };

            var sessions = new DynamoDbBasedSessions(config);
        }
    }
}