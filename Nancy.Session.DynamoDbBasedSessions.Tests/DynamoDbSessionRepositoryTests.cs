using System;
using System.Collections.Generic;
using Amazon.DynamoDBv2.DocumentModel;
using Nancy.DynamoDbBasedSessions;
using Xunit;

namespace Nancy.Session.Tests
{
    
    public class DynamoDbSessionRepositoryTests : IUseFixture<LocalDbFixture>
    {
        private LocalDbFixture _fixture;

        public Table Table { get { return _fixture.Table; } }
        public DynamoDbBasedSessionsConfiguration Configuration { get { return _fixture.Configuration; } }

        public void SetFixture(LocalDbFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        [Trait("Category", "Integration Tests")]
        public void Should_Save_New_Session()
        {
            var repository = new DynamoDbSessionRepository(new DynamoDbTableWrapper(Configuration.DynamoDbClient, Configuration.TableName), Configuration);
            var sessionData = new Session(new Dictionary<string, object> {{"key_one", "value_one"}});

            var session = repository.SaveSession(Guid.Empty, Configuration.ApplicationName, sessionData, DateTime.UtcNow.AddMinutes(30));

            var sessionDocument = Table.GetItem(repository.GetHashKey(session.SessionId, Configuration.ApplicationName));

            Assert.Equal(Configuration.SessionSerializer.Serialize(sessionData), sessionDocument["Data"].AsString());
        }

        public void Should_Generate_Guid_For_New_Session()
        {
            Assert.True(false);
        }

        [Fact]
        [Trait("Category", "Integration Tests")]
        public void Should_Set_Created_Time()
        {
            var repository = new DynamoDbSessionRepository(new DynamoDbTableWrapper(Configuration.DynamoDbClient, Configuration.TableName), Configuration);
            var sessionData = new Session(new Dictionary<string, object> { { "key_one", "value_one" } });

            var session = repository.SaveSession(Guid.Empty, Configuration.ApplicationName, sessionData, DateTime.UtcNow.AddMinutes(30));

            Assert.True((DateTime.UtcNow - session.CreateDate).Seconds < 10);
        }


        [Fact]
        [Trait("Category", "Integration Tests")]
        public void Should_Update_ExistingSession()
        {
            var repository = new DynamoDbSessionRepository(new DynamoDbTableWrapper(Configuration.DynamoDbClient, Configuration.TableName), Configuration);
            var initialSessionData = new Session(new Dictionary<string, object>{ { "key_one", "initial_one" } });
            var updatedSessionData = new Session(new Dictionary<string, object> { { "key_one", "updated_one" } });

            var initialSession = repository.SaveSession(Guid.Empty, Configuration.ApplicationName, initialSessionData, DateTime.UtcNow.AddMinutes(30));
            var updatedSession = repository.SaveSession(initialSession.SessionId, Configuration.ApplicationName, updatedSessionData, DateTime.UtcNow.AddMinutes(30));
            
            Assert.True(Math.Abs((initialSession.CreateDate - updatedSession.CreateDate).Seconds) < 1);

            var sessionDocument = Table.GetItem(repository.GetHashKey(initialSession.SessionId, Configuration.ApplicationName));

            Assert.Equal(Configuration.SessionSerializer.Serialize(updatedSessionData), sessionDocument["Data"].AsString());
            Assert.Equal(initialSession.SessionId, updatedSession.SessionId);
        }

        [Fact]
        [Trait("Category", "Integration Tests")]
        public void Should_Load_Session()
        {
            var repository = new DynamoDbSessionRepository(new DynamoDbTableWrapper(Configuration.DynamoDbClient, Configuration.TableName), Configuration);
            var data = new Session(new Dictionary<string, object> {{"key_one", "value_one"}});
            var expires = DateTime.UtcNow.AddMinutes(130);

            var session = repository.SaveSession(Guid.Empty, Configuration.ApplicationName, data, expires);

            var savedSession = repository.LoadSession(session.SessionId, Configuration.ApplicationName);

            Assert.Equal(data, savedSession.Data);
            Assert.True(Math.Abs((expires - savedSession.Expires).Seconds) < 1);
        }

        [Fact]
        [Trait("Category", "Integration Tests")]
        public void Should_Return_Null_When_Loading_Non_Existent_Session()
        {
            var repository = new DynamoDbSessionRepository(new DynamoDbTableWrapper(Configuration.DynamoDbClient, Configuration.TableName), Configuration);
            Assert.Null(repository.LoadSession(Guid.NewGuid(),"b"));
        }

        [Fact]
        [Trait("Category", "Integration Tests")]
        public void Should_Delete_Session()
        {
            var repository = new DynamoDbSessionRepository(new DynamoDbTableWrapper(Configuration.DynamoDbClient, Configuration.TableName), Configuration);
            var data = new Session(new Dictionary<string, object>{{"key_one", "value_one"}});
            var expires = DateTime.UtcNow.AddMinutes(10);

            var session = repository.SaveSession(Guid.Empty, Configuration.ApplicationName, data, expires);

            Assert.DoesNotThrow(() => repository.DeleteSession(session.SessionId, Configuration.ApplicationName));

            Assert.Null(repository.LoadSession(session.SessionId, Configuration.ApplicationName));

        }

        
    }
}