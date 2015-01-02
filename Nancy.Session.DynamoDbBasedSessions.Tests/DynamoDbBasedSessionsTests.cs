using Amazon.DynamoDBv2;
using Nancy.DynamoDbBasedSessions;
using Nancy.Testing;
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
            var repository = Substitute.For<IDynamoDbSessionRepository>();
            repository.SaveSession(Arg.Any<Guid>(), ApplicationName, Arg.Any<ISession>(), Arg.Any<DateTime>())
                .Returns(x => new DynamoDbSessionRecord(sessionId, ApplicationName, DateTime.UtcNow.AddMinutes(10), x.Arg<ISession>(),
                    DateTime.UtcNow));

            var configuration = new DynamoDbBasedSessionsConfiguration(ApplicationName)
            {
                RepositoryFactory = c => repository,
                TableInitializerFactory = c => Substitute.For<IDynamoDbTableInitializer>(),
                ClientFactory = c => Substitute.For<IAmazonDynamoDB>()
            };

            var browser = new Browser(with =>
            {
                with.ApplicationStartup((c, p) => DynamoDbBasedSessions.Enable(p, configuration));
            });

            var response = browser.Get("/");
            
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
            repository.SaveSession(sessionId, ApplicationName, Arg.Any<ISession>(), Arg.Any<DateTime>())
                .Returns(x => new DynamoDbSessionRecord(sessionId, ApplicationName, DateTime.UtcNow.AddMinutes(10), x.Arg<ISession>(),
                    DateTime.UtcNow));

            var configuration = new DynamoDbBasedSessionsConfiguration(ApplicationName)
            {
                RepositoryFactory = c => repository,
                TableInitializerFactory = c => Substitute.For<IDynamoDbTableInitializer>(),
                ClientFactory = c => Substitute.For<IAmazonDynamoDB>()
            };

            var browser = new Browser(with =>
            {
                with.ApplicationStartup((c, p) => DynamoDbBasedSessions.Enable(p, configuration));
                with.Module<SessionTestModule>();
            });

            var response = browser.Get("/", c => c.Cookie(configuration.SessionIdCookieName, sessionId.ToString()));

            Assert.Equal("key_one = value_one", response.Body.AsString());
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
            repository.LoadSession(sessionId, ApplicationName).Returns(new DynamoDbSessionRecord(sessionId, ApplicationName, DateTime.UtcNow.AddMinutes(-10), session, DateTime.UtcNow.AddMinutes(-20)));
            repository.SaveSession(Arg.Any<Guid>(), ApplicationName, Arg.Any<ISession>(), Arg.Any<DateTime>())
                .Returns(x => new DynamoDbSessionRecord(Guid.NewGuid(), ApplicationName, DateTime.UtcNow.AddMinutes(10), x.Arg<ISession>(),
                    DateTime.UtcNow));

            var configuration = new DynamoDbBasedSessionsConfiguration(ApplicationName)
            {
                RepositoryFactory = c => repository,
                TableInitializerFactory = c => Substitute.For<IDynamoDbTableInitializer>(),
                ClientFactory = c => Substitute.For<IAmazonDynamoDB>()
            };

            var browser = new Browser(with =>
            {
                with.ApplicationStartup((c, p) => DynamoDbBasedSessions.Enable(p, configuration));
                with.Module<SessionTestModule>();
            });

            var response = browser.Get("/", c => c.Cookie(configuration.SessionIdCookieName, sessionId.ToString()));

            Assert.Equal("no session", response.Body.AsString());
        }
    }

    public class SessionTestModule : NancyModule
    {
        public SessionTestModule()
        {
            Get["/"] = _ =>
            {
                var session = Request.Session;

                if (session == null || session.Count == 0)
                {
                    return "no session";
                }

                return string.Format("key_one = {0}", session["key_one"]);
            };
        }
    }
}