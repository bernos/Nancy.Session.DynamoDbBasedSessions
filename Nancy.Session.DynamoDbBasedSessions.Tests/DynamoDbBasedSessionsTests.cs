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

        private readonly Guid _sessionId;
        private readonly DynamoDbBasedSessionsConfiguration _configuration;
        private readonly IDynamoDbSessionRepository _repository;
        private readonly Browser _browser;
        
        private ISession _session = new Session();

        public DynamoDbBasedSessionsTests()
        {
            _sessionId = Guid.NewGuid();

            _session = new Session(new Dictionary<string, object>
            {
                {"key_one", 0}
            });

            _repository = Substitute.For<IDynamoDbSessionRepository>();
            _repository.LoadSession(Arg.Any<Guid>(), Arg.Any<string>()).Returns(x => new DynamoDbSessionRecord(x.Arg<Guid>(), x.Arg<string>(), DateTime.UtcNow.AddMinutes(10), _session, DateTime.UtcNow));
            _repository.SaveSession(Arg.Any<Guid>(), ApplicationName, Arg.Any<ISession>(), Arg.Any<DateTime>())
                .Returns(x => new DynamoDbSessionRecord(_sessionId, ApplicationName, DateTime.UtcNow.AddMinutes(10), x.Arg<ISession>(), DateTime.UtcNow));

            _repository.When(r => r.SaveSession(_sessionId, ApplicationName, Arg.Any<ISession>(), Arg.Any<DateTime>())).Do(
                x =>
                {
                    _session = x.Arg<ISession>() as Session;
                });

            _configuration = new DynamoDbBasedSessionsConfiguration(ApplicationName)
            {
                RepositoryFactory = c => _repository,
                TableInitializerFactory = c => Substitute.For<IDynamoDbTableInitializer>(),
                ClientFactory = c => Substitute.For<IAmazonDynamoDB>()
            };

            _browser = new Browser(with =>
            {
                with.ApplicationStartup((c, p) => DynamoDbBasedSessions.Enable(p, _configuration));
                with.Module<SessionTestModule>();
            });
        }

        [Fact]
        [Trait("Category", "Unit Tests")]
        public void Should_Add_SessionId_Cookie_To_Response()
        {
            var response = _browser.Get("/");


            Assert.Equal(1, response.Cookies.Count(c => c.Name == _configuration.SessionIdCookieName));
            Assert.Equal(_sessionId.ToString(), response.Cookies.Where(c => c.Name == _configuration.SessionIdCookieName).Select(c => c.Value).First());
        }
        
        [Fact]
        [Trait("Category", "Unit Tests")]
        public void Should_Load_Session_From_SessionId_Cookie()
        {
            var response = _browser.Get("/", c => c.Cookie(_configuration.SessionIdCookieName, _sessionId.ToString()));
            
            Assert.Equal("key_one = 0", response.Body.AsString());
        }

        [Fact]
        [Trait("Category", "Unit Tests")]
        public void Should_Delete_Expired_Session()
        {
            var repository = Substitute.For<IDynamoDbSessionRepository>();
            repository.LoadSession(Arg.Any<Guid>(), Arg.Any<string>()).Returns(x => new DynamoDbSessionRecord(x.Arg<Guid>(), x.Arg<string>(), DateTime.UtcNow.AddMinutes(-10), _session, DateTime.UtcNow.AddMinutes(-20)));
            repository.SaveSession(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<ISession>(), Arg.Any<DateTime>())
                .Returns(x => new DynamoDbSessionRecord(_sessionId, ApplicationName, DateTime.UtcNow.AddMinutes(10), x.Arg<ISession>(),
                    DateTime.UtcNow));

            repository.When(r => r.SaveSession(_sessionId, ApplicationName, Arg.Any<ISession>(), Arg.Any<DateTime>())).Do(
                x =>
                {
                    _session = x.Arg<ISession>() as Session;
                });

            _configuration.RepositoryFactory = c => repository;
            
            //_repository.LoadSession(Arg.Any<Guid>(), Arg.Any<string>()).Returns(x => new DynamoDbSessionRecord(x.Arg<Guid>(), x.Arg<string>(), DateTime.UtcNow.AddMinutes(-10), _session, DateTime.UtcNow.AddMinutes(-20)));
            
            /*_repository.SaveSession(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<ISession>(), Arg.Any<DateTime>())
                .Returns(x => new DynamoDbSessionRecord(_sessionId, ApplicationName, DateTime.UtcNow.AddMinutes(10), x.Arg<ISession>(),
                    DateTime.UtcNow));

            _repository.When(r => r.SaveSession(_sessionId, ApplicationName, Arg.Any<ISession>(), Arg.Any<DateTime>())).Do(
                x =>
                {
                    _session = x.Arg<ISession>() as Session;
                });

            _configuration.RepositoryFactory = c => _repository;
            */

            //_repository.LoadSession(Arg.Any<Guid>(), Arg.Any<string>()).Returns(x => new DynamoDbSessionRecord(x.Arg<Guid>(), x.Arg<string>(), DateTime.UtcNow.AddMinutes(-10), _session, DateTime.UtcNow.AddMinutes(-20)));
            var response = _browser.Get("/", c => c.Cookie(_configuration.SessionIdCookieName, _sessionId.ToString()));

            Assert.Equal("no session", response.Body.AsString());
        }

        [Fact]
        [Trait("Category", "Unit Tests")]
        public void Should_Persist_Session_Between_Requests()
        {
            var repository = Substitute.For<IDynamoDbSessionRepository>();
            repository.LoadSession(Arg.Any<Guid>(), Arg.Any<string>()).Returns(x => new DynamoDbSessionRecord(x.Arg<Guid>(), x.Arg<string>(), DateTime.UtcNow.AddMinutes(10), _session, DateTime.UtcNow.AddMinutes(-20)));
            repository.SaveSession(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<ISession>(), Arg.Any<DateTime>())
                .Returns(x => new DynamoDbSessionRecord(_sessionId, x.Arg<string>(), DateTime.UtcNow.AddMinutes(10), x.Arg<ISession>(), DateTime.UtcNow));

            repository.When(r => r.SaveSession(_sessionId, ApplicationName, Arg.Any<ISession>(), Arg.Any<DateTime>())).Do(
                x =>
                {
                    _session = x.Arg<ISession>() as Session;
                });

            _configuration.RepositoryFactory = c => repository;

            _browser.Get("/increment", c => c.Cookie(_configuration.SessionIdCookieName, _sessionId.ToString()));
            Assert.Equal(1, _session["key_one"]);

            _browser.Get("/increment", c => c.Cookie(_configuration.SessionIdCookieName, _sessionId.ToString()));
            Assert.Equal(2, _session["key_one"]);
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

            Get["/increment"] = _ =>
            {
                var session = Request.Session;

                session["key_one"] = ((int)session["key_one"]) + 1;

                return string.Format("key_one = {0}", session["key_one"]);
            };
        }
    }
}