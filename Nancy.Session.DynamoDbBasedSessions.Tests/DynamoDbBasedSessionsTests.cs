using System;
using System.Collections.Generic;
using System.Linq;
using Nancy.DynamoDbBasedSessions;
using NSubstitute;
using NSubstitute.Core;
using Xunit;

namespace Nancy.Session.Tests
{
    public class DynamoDbBasedSessionsTests
    {
        private readonly DynamoDbBasedSessionsConfiguration _configuration;
        private readonly IDynamoDbSessionRepository _repository;

        public DynamoDbBasedSessionsTests()
        {
            _repository = new MockRepository();

            _configuration = new DynamoDbBasedSessionsConfiguration("Test")
            {
                RepositoryFactory = c => _repository,
                TableInitializerFactory = c => Substitute.For<IDynamoDbTableInitializer>()
            };
        }

        [Fact]
        public void Should_Add_SessionId_Cookie_To_Response()
        {
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

            repository.LoadSession("abc", "123")
                .ReturnsForAnyArgs(new DynamoDbSessionRecord(sessionId, applicationName, DateTime.UtcNow.AddMinutes(-10),
                    "datahere", DateTime.UtcNow.AddMinutes(-20)));

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

            public void SaveSession(string sessionId, string applicationName, string sessionData, DateTime expires, bool isNew)
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
    }
}