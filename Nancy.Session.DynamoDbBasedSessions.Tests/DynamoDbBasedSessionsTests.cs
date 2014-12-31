using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.EC2.Util;
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

            var sessionId = Guid.NewGuid();
            var response = new Response();

            sut.Save(sessionId, session, response);

            Assert.Equal(1, response.Cookies.Count(c => c.Name == _configuration.SessionIdCookieName));
            Assert.Equal(sessionId.ToString(), response.Cookies.Where(c => c.Name == _configuration.SessionIdCookieName).Select(c => c.Value).First());
        }

        [Fact]
        [Trait("Category", "Unit Tests")]
        public void Should_Save_New_Session()
        {
            var sessions = new List<ISession>();
            
            var repository = Substitute.For<IDynamoDbSessionRepository>();
            repository.WhenForAnyArgs(r => r.SaveSession(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<ISession>(), Arg.Any<DateTime>())).Do(x => sessions.Add(x.Arg<ISession>()));
            repository.SaveSession(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<ISession>(), Arg.Any<DateTime>())
                .ReturnsForAnyArgs(
                    x =>
                        new DynamoDbSessionRecord(x.Arg<Guid>(), x.Arg<string>(), DateTime.UtcNow, x.Arg<ISession>(),
                            DateTime.UtcNow));

            
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

            sut.Save(Guid.Empty, session, new Response());

            Assert.Equal(session, sessions.First());
        }

        [Fact]
        [Trait("Category", "Unit Tests")]
        public void Should_Load_Session()
        {
            var applicationName = "application_name";
            var sessionId = Guid.NewGuid();
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
            request.Cookies.Add(configuration.SessionIdCookieName, sessionId.ToString());

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

            var sessionId = Guid.NewGuid();
            var request = new Request("GET", "/", "http");
            var response = new Response();

            sut.Save(sessionId, session, response);
            request.Cookies.Add(_configuration.SessionIdCookieName, sessionId.ToString());
            Assert.Equal(sessionId.ToString(), response.Cookies.First(c => c.Name == _configuration.SessionIdCookieName).Value);

            var loadedSession = sut.Load(request);

            Assert.Equal("value_one", loadedSession["key_one"]);
        }

        [Fact]
        public void Should_Delete_Expired_Session()
        {
            var applicationName = "abc";
            var sessionId = Guid.NewGuid();
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

            repository.LoadSession(sessionId, applicationName)
                .ReturnsForAnyArgs(new DynamoDbSessionRecord(sessionId, applicationName, DateTime.UtcNow.AddMinutes(-10),
                    session, DateTime.UtcNow.AddMinutes(-20)));

            request.Cookies.Add(configuration.SessionIdCookieName, sessionId.ToString());

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

            public DynamoDbSessionRecord LoadSession(Guid sessionId, string applicationName)
            {
                var key = GetHashKey(sessionId, applicationName);

                if (_sessions.ContainsKey(key))
                {
                    return _sessions[key];
                }

                return null;
            }

            public DynamoDbSessionRecord SaveSession(string applicationName, ISession sessionData, DateTime expires)
            {
                return SaveSession(Guid.Empty, applicationName, sessionData, expires);
            }

            public DynamoDbSessionRecord SaveSession(Guid sessionId, string applicationName, ISession sessionData, DateTime expires)
            {
                if (sessionId == Guid.Empty)
                {
                    sessionId = Guid.NewGuid();
                }

                var key = GetHashKey(sessionId, applicationName);
                var session = new DynamoDbSessionRecord(sessionId, applicationName, expires, sessionData, DateTime.UtcNow);
                    
                _sessions[key] = session;

                return session;
            }

            public void DeleteSession(Guid sessionId, string applicationName)
            {
                _sessions.Remove(GetHashKey(sessionId, applicationName));
            }

            public string GetHashKey(Guid sessionId, string applicationName)
            {
                return string.Format("{0}-{1}", applicationName, sessionId.ToString());
            }
        }

        public void Dispose()
        {
            _configuration.Dispose();
        }
    }
}