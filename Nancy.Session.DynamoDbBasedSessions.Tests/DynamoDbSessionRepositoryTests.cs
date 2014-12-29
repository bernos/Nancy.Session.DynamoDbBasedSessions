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
            var sessionData = new Session(new Dictionary<string, object> {{"key_one", "value_one"}});

            var session = repository.SaveSession(String.Empty, Configuration.ApplicationName, sessionData, DateTime.UtcNow.AddMinutes(30));

            var sessionDocument = Table.GetItem(repository.GetHashKey(session.SessionId, Configuration.ApplicationName));

            Assert.Equal(Configuration.SessionSerializer.Serialize(sessionData), sessionDocument["Data"].AsString());
        }

        [Fact]
        [Trait("Category", "Integration Tests")]
        public void Should_Set_Created_Time()
        {
            var repository = new DynamoDbSessionRepository(Configuration);
            var sessionData = new Session(new Dictionary<string, object> { { "key_one", "value_one" } });

            var session = repository.SaveSession(string.Empty, Configuration.ApplicationName, sessionData, DateTime.UtcNow.AddMinutes(30));

            Assert.True((DateTime.UtcNow - session.CreateDate).Seconds < 10);
        }


        [Fact]
        [Trait("Category", "Integration Tests")]
        public void Should_Update_ExistingSession()
        {
            var repository = new DynamoDbSessionRepository(Configuration);
            var initialSessionData = new Session(new Dictionary<string, object>{ { "key_one", "initial_one" } });
            var updatedSessionData = new Session(new Dictionary<string, object> { { "key_one", "updated_one" } });

            var initialSession = repository.SaveSession(string.Empty, Configuration.ApplicationName, initialSessionData, DateTime.UtcNow.AddMinutes(30));
            var updatedSession = repository.SaveSession(initialSession.SessionId, Configuration.ApplicationName, updatedSessionData, DateTime.UtcNow.AddMinutes(30));
            
            Assert.True(Math.Abs((initialSession.CreateDate - updatedSession.CreateDate).Seconds) < 1);

            var sessionDocument = Table.GetItem(repository.GetHashKey(initialSession.SessionId, Configuration.ApplicationName));

            Assert.Equal(Configuration.SessionSerializer.Serialize(updatedSessionData), sessionDocument["Data"].AsString());
        }

        [Fact]
        [Trait("Category", "Integration Tests")]
        public void Should_Load_Session()
        {
            var repository = new DynamoDbSessionRepository(Configuration);
            var data = new Session(new Dictionary<string, object> {{"key_one", "value_one"}});
            var expires = DateTime.UtcNow.AddMinutes(130);

            var session = repository.SaveSession(string.Empty, Configuration.ApplicationName, data, expires);

            var savedSession = repository.LoadSession(session.SessionId, Configuration.ApplicationName);

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
            var data = new Session(new Dictionary<string, object>{{"key_one", "value_one"}});
            var expires = DateTime.UtcNow.AddMinutes(10);

            var session = repository.SaveSession(string.Empty, Configuration.ApplicationName, data, expires);

            Assert.DoesNotThrow(() => repository.DeleteSession(session.SessionId, Configuration.ApplicationName));

            Assert.Null(repository.LoadSession(session.SessionId, Configuration.ApplicationName));

        }
    }
}