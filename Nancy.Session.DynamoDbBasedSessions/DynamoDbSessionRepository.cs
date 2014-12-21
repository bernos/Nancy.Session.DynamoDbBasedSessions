﻿using System;
using System.Collections.Generic;
using System.Globalization;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.OpsWorks.Model;
using Nancy.DynamoDbBasedSessions;

namespace Nancy.Session
{
    public interface IDynamoDbSessionRepository
    {
        DynamoDbSessionRecord LoadSession(string sessionId, string applicationName);
        void SaveSession(string sessionId, string applicationName, string sessionData, DateTime expires, bool isNew);
        void DeleteSession(string sessionId, string applicationName);
        string GetHashKey(string sessionId, string applicationName);
    }

    public class DynamoDbSessionRecord
    {
        public string ApplicationName { get; private set; }
        public string SessionId { get; private set; }
        public string Data { get; private set; }
        public DateTime CreateDate { get; private set; }
        public DateTime Expires { get; private set; }
        public String RecordFormatVersion { get; private set; }

        public DynamoDbSessionRecord(string sessionId, string applicationName, DateTime expires, string data, DateTime createDate)
        {
            SessionId = sessionId;
            ApplicationName = applicationName;
            Expires = expires;
            Data = data;
            CreateDate = createDate;
        }
    }

    public class DynamoDbSessionRepository : IDynamoDbSessionRepository
    {
        private const string CreateDateKey = "CreateDate";
        private const string ExpiresKey = "Expires";
        private const string SessionDataKey = "Data";
        private const string RecordFormatKey = "Ver";
        private const string RecordFormat = "1.0";

        private readonly DynamoDbBasedSessionsConfiguration _configuration;
        private readonly AmazonDynamoDBClient _client;
        private readonly Table _table;

        public DynamoDbSessionRepository(DynamoDbBasedSessionsConfiguration configuration)
        {
            _configuration = configuration;
            _client = configuration.CreateClient();
            _table = Table.LoadTable(_client, _configuration.TableName);
        }
        
        public DynamoDbSessionRecord LoadSession(string sessionId, string applicationName)
        {
            var hashKey = GetHashKey(sessionId, applicationName);
            var document = _table.GetItem(hashKey);

            // Need to be explicit about date time parsing
            var expires = DateTime.SpecifyKind(DateTime.Parse(document[ExpiresKey].AsString(), null, DateTimeStyles.RoundtripKind), DateTimeKind.Utc);
            var created = DateTime.SpecifyKind(DateTime.Parse(document[CreateDateKey].AsString(), null, DateTimeStyles.RoundtripKind), DateTimeKind.Utc);

            return new DynamoDbSessionRecord(sessionId, applicationName, expires, document[SessionDataKey].AsString(), created);
        }

        public void SaveSession(string sessionId, string applicationName, string sessionData, DateTime expires, bool isNew)
        {
            var sessionDocument = new Document();
            var hashKey = GetHashKey(sessionId, applicationName);

            sessionDocument[ExpiresKey] = expires;
            sessionDocument[SessionDataKey] = sessionData;
            sessionDocument[RecordFormatKey] = RecordFormat;
            sessionDocument[_configuration.SessionIdAttributeName] = hashKey;

            if (isNew)
            {
                sessionDocument[CreateDateKey] = DateTime.UtcNow;

                _table.PutItem(sessionDocument);
            }
            else
            {
                _table.UpdateItem(sessionDocument);
            }
        }

        public void DeleteSession(string sessionId, string applicationName)
        {
            throw new NotImplementedException();
        }

        public string GetHashKey(string sessionId, string applicationName)
        {
            return String.Format("{0}-{1}", applicationName, sessionId);
        }
    }
}