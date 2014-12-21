﻿using System;
using Amazon;
using Amazon.DynamoDBv2.DocumentModel;
using Nancy.DynamoDbBasedSessions;
using Xunit;

namespace Nancy.Session.Tests
{
    public class DynamoDbSessionRepositoryTests
    {
        private readonly DynamoDbBasedSessionsConfiguration _configuration;
        private readonly Table _table;
        public DynamoDbSessionRepositoryTests()
        {
            _configuration= new DynamoDbBasedSessionsConfiguration("Test")
            {
                RegionEndpoint = RegionEndpoint.APSoutheast2
            };

            _table = Table.LoadTable(_configuration.CreateClient(), _configuration.TableName);
        }

        [Fact]
        public void Should_Save_New_Session()
        {
            var repository = new DynamoDbSessionRepository(_configuration);
            var sessionId = Guid.NewGuid().ToString();
            var hashKey = repository.GetHashKey(sessionId, _configuration.ApplicationName);
            var sessionData = "Hello there";

            repository.SaveSession(sessionId, _configuration.ApplicationName, sessionData, DateTime.UtcNow.AddMinutes(30), true);

            var sessionDocument = _table.GetItem(hashKey);

            Assert.Equal(sessionData, sessionDocument["Data"].AsString());
        }

        [Fact]
        public void Should_Update_ExistingSession()
        {
            var repository = new DynamoDbSessionRepository(_configuration);
            var sessionId = Guid.NewGuid().ToString();
            var hashKey = repository.GetHashKey(sessionId, _configuration.ApplicationName);
            var initialSessionData = "Initial session data";
            var updatedSessionData = "Updated session data";

            repository.SaveSession(sessionId, _configuration.ApplicationName, initialSessionData, DateTime.UtcNow.AddMinutes(30), true);
            repository.SaveSession(sessionId, _configuration.ApplicationName, updatedSessionData, DateTime.UtcNow.AddMinutes(30), false);

            var sessionDocument = _table.GetItem(hashKey);

            Assert.Equal(updatedSessionData, sessionDocument["Data"].AsString());
        }

        [Fact]
        public void Should_Load_Session()
        {
            var repository = new DynamoDbSessionRepository(_configuration);
            var sessionId = Guid.NewGuid().ToString();
            var data = "session data here";
            var expires = DateTime.UtcNow.AddMinutes(130);

            repository.SaveSession(sessionId, _configuration.ApplicationName, data, expires, true);

            var savedSession = repository.LoadSession(sessionId, _configuration.ApplicationName);

            Assert.Equal(data, savedSession.Data);
            Assert.True(Math.Abs((expires - savedSession.Expires).Seconds) < 1);
        }
    }
}