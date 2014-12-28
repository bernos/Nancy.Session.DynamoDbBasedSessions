using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.DynamoDBv2;
using Nancy.DynamoDbBasedSessions;
using NSubstitute;
using NSubstitute.Core;
using Xunit;

namespace Nancy.Session.Tests
{
    public class DynamoDbBasedSessionsTests : IDisposable
    {
        private readonly DynamoDbBasedSessionsConfiguration _configuration;
        private readonly IDynamoDbSessionRepository _repository;

        public DynamoDbBasedSessionsTests()
        {
            _repository = new MockRepository();

            _configuration = new DynamoDbBasedSessionsConfiguration("Test")
            {
                RepositoryFactory = c => _repository,
                TableInitializerFactory = c => Substitute.For<IDynamoDbTableInitializer>(),
                ClientFactory = c => Substitute.For<IAmazonDynamoDB>()
            };
        }

        [Fact]
        [Trait("Category", "Unit Tests")]
        public void Should_Add_SessionId_Cookie_To_Response()
        {
            // TODO: Could use substitute repos
            var sut = new DynamoDbBasedSessions(_configuration);

            var session = new Session(new Dictionary<string, object>
            {
                {"key_one", "value_one"},
                {"key_two", "value_two"}
            });

            var sessionId = Guid.NewGuid().ToString();
            var response = new Response();

            sut.Save(sessionId, session, response, true);

            Assert.Equal(1, response.Cookies.Count(c => c.Name == _configuration.SessionIdCookieName));
            Assert.Equal(sessionId, response.Cookies.Where(c => c.Name == _configuration.SessionIdCookieName).Select(c => c.Value).First());
        }

        [Fact]
        [Trait("Category", "Unit Tests")]
        public void Should_Save_Session()
        {
            var sessions = new List<ISession>();

            var repository = Substitute.For<IDynamoDbSessionRepository>();
            repository.WhenForAnyArgs(r => r.SaveSession(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<ISession>(), Arg.Any<DateTime>(), Arg.Any<bool>())).Do(x => sessions.Add(x.Arg<ISession>()));

            var session = new Session(new Dictionary<string, object>
            {
                {"key_one", "value_one"},
                {"key_two", "value_two"}
            });

            var configuration = new DynamoDbBasedSessionsConfiguration("Test")
            {
                RepositoryFactory = c => repository,
                TableInitializerFactory = c => Substitute.For<IDynamoDbTableInitializer>(),
                ClientFactory = c => Substitute.For<IAmazonDynamoDB>()
            };

            var sut = new DynamoDbBasedSessions(configuration);

            sut.Save(Guid.NewGuid().ToString(), session, new Response(), true);

            Assert.Equal(session, sessions.First());
        }

        [Fact]
        [Trait("Category", "Unit Tests")]
        public void Should_Load_Session()
        {
            var applicationName = "application_name";
            var sessionId = "session_id";
            var session = new Session(new Dictionary<string, object>
            {
                {"key_one", "value_one"},
                {"key_two", "value_two"}
            });

            var repository = Substitute.For<IDynamoDbSessionRepository>();
            repository.LoadSession(sessionId, applicationName).Returns(new DynamoDbSessionRecord(sessionId, applicationName, DateTime.UtcNow.AddMinutes(10), session, DateTime.UtcNow));

            var configuration = new DynamoDbBasedSessionsConfiguration(applicationName)
            {
                RepositoryFactory = c => repository,
                TableInitializerFactory = c => Substitute.For<IDynamoDbTableInitializer>(),
                ClientFactory = c => Substitute.For<IAmazonDynamoDB>()
            };

            var request = new Request("GET", "/", "http");
            request.Cookies.Add(configuration.SessionIdCookieName, sessionId);

            var sut = new DynamoDbBasedSessions(configuration);

            var loadedSession = sut.Load(request);

            Assert.Equal(session, loadedSession);

        }

        [Fact]
        public void Should_Save_And_Load_Session_Data()
        {
            var sut = new DynamoDbBasedSessions(_configuration);

            var session = new Session(new Dictionary<string, object>
            {
                {"key_one", "value_one"},
                {"key_two", "value_two"}
            });

            var sessionId = Guid.NewGuid().ToString();
            var request = new Request("GET", "/", "http");
            var response = new Response();

            sut.Save(sessionId, session, response, true);
            request.Cookies.Add(_configuration.SessionIdCookieName, sessionId);
            Assert.Equal(sessionId, response.Cookies.First(c => c.Name == _configuration.SessionIdCookieName).Value);

            var loadedSession = sut.Load(request);

            Assert.Equal("value_one", loadedSession["key_one"]);
        }

        [Fact]
        public void Should_Delete_Expired_Session()
        {
            var applicationName = "abc";
            var sessionId = "123";
            var repository = Substitute.For<IDynamoDbSessionRepository>();
            var request = new Request("GET", "/", "http");
            
            var configuration = new DynamoDbBasedSessionsConfiguration("Test")
            {
                RepositoryFactory = c => repository,
                TableInitializerFactory = c => Substitute.For<IDynamoDbTableInitializer>()
            };

            var sut = new DynamoDbBasedSessions(configuration);

            var session = new Session(new Dictionary<string, object>
            {
                {"key_one", "value_one"}
            });

            repository.LoadSession("abc", "123")
                .ReturnsForAnyArgs(new DynamoDbSessionRecord(sessionId, applicationName, DateTime.UtcNow.AddMinutes(-10),
                    session, DateTime.UtcNow.AddMinutes(-20)));

            request.Cookies.Add(configuration.SessionIdCookieName, sessionId);

            var loadedSession = sut.Load(request);

            Assert.Equal(0, loadedSession.Count);

        }

        private class MockRepository : IDynamoDbSessionRepository
        {
            private readonly IDictionary<string, DynamoDbSessionRecord> _sessions;

            public MockRepository()
                : this(new Dictionary<string, DynamoDbSessionRecord>())
            {
            }

            public MockRepository(IDictionary<string, DynamoDbSessionRecord> sessions)
            {
                _sessions = sessions;
            }

            public DynamoDbSessionRecord LoadSession(string sessionId, string applicationName)
            {
                var key = GetHashKey(sessionId, applicationName);

                if (_sessions.ContainsKey(key))
                {
                    return _sessions[key];
                }

                return null;
            }

            public void SaveSession(string sessionId, string applicationName, ISession sessionData, DateTime expires, bool isNew)
            {
                var key = GetHashKey(sessionId, applicationName);
                var session = new DynamoDbSessionRecord(sessionId, applicationName, expires, sessionData, DateTime.UtcNow);
                    
                _sessions[key] = session;
            }

            public void DeleteSession(string sessionId, string applicationName)
            {
                _sessions.Remove(GetHashKey(sessionId, applicationName));
            }

            public string GetHashKey(string sessionId, string applicationName)
            {
                return string.Format("{0}-{1}", applicationName, sessionId);
            }
        }

        public void Dispose()
        {
            _configuration.Dispose();
        }
    }
}