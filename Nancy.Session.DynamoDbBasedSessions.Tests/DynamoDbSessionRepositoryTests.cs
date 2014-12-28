using System;
using System.Collections.Generic;
using Amazon;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Nancy.DynamoDbBasedSessions;
using Xunit;

namespace Nancy.Session.Tests
{
    public class DynamoDbSessionRepositoryTests : IDisposable
    {
        private readonly DynamoDbBasedSessionsConfiguration _configuration;
        private readonly Table _table;
        public DynamoDbSessionRepositoryTests()
        {
            _configuration= new DynamoDbBasedSessionsConfiguration("Test")
            {
                TableName = "DynamoDbSessionRepositoryTests_Table",
                DynamoDbConfig = new Amazon.DynamoDBv2.AmazonDynamoDBConfig
                {
                    ServiceURL = "http://localhost:8000"
                }
            };

            var initializer = new DynamoDbTableInitializer(_configuration);
            initializer.Initialize();

            _table = Table.LoadTable(_configuration.CreateClient(), _configuration.TableName);
        }

        public void Dispose()
        {
            using (var client = _configuration.CreateClient())
            {
                client.DeleteTable(_configuration.TableName);
                Console.WriteLine("Deleted test table '{0}'", _configuration.TableName);
            }
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void Should_Save_New_Session()
        {
            var repository = new DynamoDbSessionRepository(_configuration);
            var sessionId = Guid.NewGuid().ToString();
            var hashKey = repository.GetHashKey(sessionId, _configuration.ApplicationName);
            var sessionData = new Session();

            repository.SaveSession(sessionId, _configuration.ApplicationName, sessionData, DateTime.UtcNow.AddMinutes(30), true);

            var sessionDocument = _table.GetItem(hashKey);

            Assert.Equal(_configuration.SessionSerializer.Serialize(sessionData), sessionDocument["Data"].AsString());
            
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void Should_Update_ExistingSession()
        {
            var repository = new DynamoDbSessionRepository(_configuration);
            var sessionId = Guid.NewGuid().ToString();
            var hashKey = repository.GetHashKey(sessionId, _configuration.ApplicationName);
            var initialSessionData = new Session(new Dictionary<string, object>{ { "key_one", "initial_one" } });
            var updatedSessionData = new Session(new Dictionary<string, object> { { "key_one", "updated_one" } });

            repository.SaveSession(sessionId, _configuration.ApplicationName, initialSessionData, DateTime.UtcNow.AddMinutes(30), true);
            repository.SaveSession(sessionId, _configuration.ApplicationName, updatedSessionData, DateTime.UtcNow.AddMinutes(30), false);

            var sessionDocument = _table.GetItem(hashKey);

            Assert.Equal(_configuration.SessionSerializer.Serialize(updatedSessionData), sessionDocument["Data"].AsString());
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void Should_Load_Session()
        {
            var repository = new DynamoDbSessionRepository(_configuration);
            var sessionId = Guid.NewGuid().ToString();
            var data = new Session(new Dictionary<string, object> {{"key_one", "value_one"}});
            var expires = DateTime.UtcNow.AddMinutes(130);

            repository.SaveSession(sessionId, _configuration.ApplicationName, data, expires, true);

            var savedSession = repository.LoadSession(sessionId, _configuration.ApplicationName);

            Assert.Equal(data, savedSession.Data);
            Assert.True(Math.Abs((expires - savedSession.Expires).Seconds) < 1);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void Should_Return_Null_When_Loading_Non_Existent_Session()
        {
            var repository = new DynamoDbSessionRepository(_configuration);
            Assert.Null(repository.LoadSession("a","b"));
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void Should_Delete_Session()
        {
            var repository = new DynamoDbSessionRepository(_configuration);
            var sessionId = Guid.NewGuid().ToString();
            var data = new Session(new Dictionary<string, object>{{"key_one", "value_one"}});
            var expires = DateTime.UtcNow.AddMinutes(10);

            repository.SaveSession(sessionId, _configuration.ApplicationName, data, expires, true);

            Assert.DoesNotThrow(() => repository.DeleteSession(sessionId, _configuration.ApplicationName));

            Assert.Null(repository.LoadSession(sessionId, _configuration.ApplicationName));

        }
    }
}