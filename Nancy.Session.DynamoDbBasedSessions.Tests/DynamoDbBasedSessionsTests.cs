using Amazon.DynamoDBv2;
using Nancy.DynamoDbBasedSessions;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Nancy.Session.Tests
{
    public class DynamoDbBasedSessionsTests
    {
        public const string ApplicationName = "DynamoDbBasedSessionsTests";

        [Fact]
        [Trait("Category", "Unit Tests")]
        public void Should_Add_SessionId_Cookie_To_Response()
        {
            var sessionId = Guid.NewGuid();
            var session = new Session(new Dictionary<string, object>
            {
                {"key_one", "value_one"},
                {"key_two", "value_two"}
            });

            var repository = Substitute.For<IDynamoDbSessionRepository>();
            repository.SaveSession(sessionId, ApplicationName, session, Arg.Any<DateTime>())
                .Returns(new DynamoDbSessionRecord(sessionId, ApplicationName, DateTime.UtcNow.AddMinutes(10), session,
                    DateTime.UtcNow));

            var configuration = new DynamoDbBasedSessionsConfiguration(ApplicationName)
            {
                RepositoryFactory = c => repository,
                TableInitializerFactory = c => Substitute.For<IDynamoDbTableInitializer>(),
                ClientFactory = c => Substitute.For<IAmazonDynamoDB>()
            };

            var response = new Response();

            var sut = new DynamoDbBasedSessions(configuration);
            
            var savedSession = sut.Save(sessionId, session, response);

            Assert.Equal(1, response.Cookies.Count(c => c.Name == configuration.SessionIdCookieName));
            Assert.Equal(sessionId.ToString(), response.Cookies.Where(c => c.Name == configuration.SessionIdCookieName).Select(c => c.Value).First());
          
        }
        
        [Fact]
        [Trait("Category", "Unit Tests")]
        public void Should_Load_Session_From_SessionId_Cookie()
        {
            var sessionId = Guid.NewGuid();
            var session = new Session(new Dictionary<string, object>
            {
                {"key_one", "value_one"},
                {"key_two", "value_two"}
            });

            var repository = Substitute.For<IDynamoDbSessionRepository>();
            repository.LoadSession(sessionId, ApplicationName).Returns(new DynamoDbSessionRecord(sessionId, ApplicationName, DateTime.UtcNow.AddMinutes(10), session, DateTime.UtcNow));

            var configuration = new DynamoDbBasedSessionsConfiguration(ApplicationName)
            {
                RepositoryFactory = c => repository,
                TableInitializerFactory = c => Substitute.For<IDynamoDbTableInitializer>(),
                ClientFactory = c => Substitute.For<IAmazonDynamoDB>()
            };

            var request = new Request("GET", "/", "http");
            request.Cookies.Add(configuration.SessionIdCookieName, sessionId.ToString());

            var sut = new DynamoDbBasedSessions(configuration);

            var loadedSession = sut.Load(request);

            Assert.Equal(session, loadedSession);

        }

        [Fact]
        [Trait("Category", "Unit Tests")]
        public void Should_Delete_Expired_Session()
        {
            var sessionId = Guid.NewGuid();
            var session = new Session(new Dictionary<string, object>
            {
                {"key_one", "value_one"}
            });

            var repository = Substitute.For<IDynamoDbSessionRepository>();
            repository.LoadSession(sessionId, ApplicationName)
                .Returns(new DynamoDbSessionRecord(sessionId, ApplicationName, DateTime.UtcNow.AddMinutes(-10),
                    session, DateTime.UtcNow.AddMinutes(-20)));

            var configuration = new DynamoDbBasedSessionsConfiguration(ApplicationName)
            {
                RepositoryFactory = c => repository,
                TableInitializerFactory = c => Substitute.For<IDynamoDbTableInitializer>(),
                ClientFactory = c => Substitute.For<IAmazonDynamoDB>()
            };

            var request = new Request("GET", "/", "http");
            request.Cookies.Add(configuration.SessionIdCookieName, sessionId.ToString());

            var sut = new DynamoDbBasedSessions(configuration);

            var loadedSession = sut.Load(request);

            Assert.Equal(0, loadedSession.Count);
        }
    }
}