using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Amazon;
using Amazon.CodeDeploy.Model;
using Nancy.DynamoDbBasedSessions;
using Xunit;

namespace Nancy.Session.Tests
{
    public class DynamoDbBasedSessionsTests
    {
        private readonly DynamoDbBasedSessionsConfiguration _configuration;

        public DynamoDbBasedSessionsTests()
        {
            _configuration = new DynamoDbBasedSessionsConfiguration("Test")
            {
                RegionEndpoint = RegionEndpoint.APSoutheast2,
                ProfileName = "personal"
            };
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void Should_Save_Session_Data()
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

        private class MockRepository : IDynamoDbSessionRepository
        {
            private readonly IDictionary<string, DynamoDbSessionRecord> _sessions;

            public MockRepository()
            {
                _sessions = new Dictionary<string, DynamoDbSessionRecord>();
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