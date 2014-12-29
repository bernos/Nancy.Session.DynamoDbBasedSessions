using System;
using System.Collections.Generic;
using Amazon;
using Amazon.DynamoDBv2.Model;
using Xunit;

namespace Nancy.Session.Tests
{
    public class DynamoDbSessionRepositoryTests : IntegrationTest
    {
        [Fact]
        [Trait("Category", "Integration Tests")]
        public void Should_Save_New_Session()
        {
            var repository = new DynamoDbSessionRepository(Configuration);
            var sessionId = Guid.NewGuid().ToString();
            var hashKey = repository.GetHashKey(sessionId, Configuration.ApplicationName);
            var sessionData = new Session(new Dictionary<string, object> {{"key_one", "value_one"}});

            repository.SaveSession(sessionId, Configuration.ApplicationName, sessionData, DateTime.UtcNow.AddMinutes(30), true);

            var sessionDocument = Table.GetItem(hashKey);

            Assert.Equal(Configuration.SessionSerializer.Serialize(sessionData), sessionDocument["Data"].AsString());
        }

        [Fact]
        [Trait("Category", "Integration Tests")]
        public void Should_Set_Created_Time()
        {
            var repository = new DynamoDbSessionRepository(Configuration);
            var sessionId = Guid.NewGuid().ToString();
            var sessionData = new Session(new Dictionary<string, object> { { "key_one", "value_one" } });

            var session = repository.SaveSession(sessionId, Configuration.ApplicationName, sessionData, DateTime.UtcNow.AddMinutes(30), true);

            Assert.True((DateTime.UtcNow - session.CreateDate).Seconds < 10);
        }


        [Fact]
        [Trait("Category", "Integration Tests")]
        public void Should_Update_ExistingSession()
        {
            var repository = new DynamoDbSessionRepository(Configuration);
            var sessionId = Guid.NewGuid().ToString();
            var hashKey = repository.GetHashKey(sessionId, Configuration.ApplicationName);
            var initialSessionData = new Session(new Dictionary<string, object>{ { "key_one", "initial_one" } });
            var updatedSessionData = new Session(new Dictionary<string, object> { { "key_one", "updated_one" } });

            var initialSession = repository.SaveSession(sessionId, Configuration.ApplicationName, initialSessionData, DateTime.UtcNow.AddMinutes(30), true);
            var updatedSession = repository.SaveSession(sessionId, Configuration.ApplicationName, updatedSessionData, DateTime.UtcNow.AddMinutes(30), false);
            
            Assert.True(Math.Abs((initialSession.CreateDate - updatedSession.CreateDate).Seconds) < 1);

            var sessionDocument = Table.GetItem(hashKey);

            Assert.Equal(Configuration.SessionSerializer.Serialize(updatedSessionData), sessionDocument["Data"].AsString());
        }

        [Fact]
        [Trait("Category", "Integration Tests")]
        public void Should_Load_Session()
        {
            var repository = new DynamoDbSessionRepository(Configuration);
            var sessionId = Guid.NewGuid().ToString();
            var data = new Session(new Dictionary<string, object> {{"key_one", "value_one"}});
            var expires = DateTime.UtcNow.AddMinutes(130);

            repository.SaveSession(sessionId, Configuration.ApplicationName, data, expires, true);

            var savedSession = repository.LoadSession(sessionId, Configuration.ApplicationName);

            Assert.Equal(data, savedSession.Data);
            Assert.True(Math.Abs((expires - savedSession.Expires).Seconds) < 1);
        }

        [Fact]
        [Trait("Category", "Integration Tests")]
        public void Should_Return_Null_When_Loading_Non_Existent_Session()
        {
            var repository = new DynamoDbSessionRepository(Configuration);
            Assert.Null(repository.LoadSession("a","b"));
        }

        [Fact]
        [Trait("Category", "Integration Tests")]
        public void Should_Delete_Session()
        {
            var repository = new DynamoDbSessionRepository(Configuration);
            var sessionId = Guid.NewGuid().ToString();
            var data = new Session(new Dictionary<string, object>{{"key_one", "value_one"}});
            var expires = DateTime.UtcNow.AddMinutes(10);

            repository.SaveSession(sessionId, Configuration.ApplicationName, data, expires, true);

            Assert.DoesNotThrow(() => repository.DeleteSession(sessionId, Configuration.ApplicationName));

            Assert.Null(repository.LoadSession(sessionId, Configuration.ApplicationName));

        }
    }
}