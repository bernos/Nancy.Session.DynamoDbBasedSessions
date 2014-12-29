using System.Security.Policy;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.OpsWorks.Model;
using Nancy.DynamoDbBasedSessions;
using System;
using System.Globalization;

namespace Nancy.Session
{
    public class DynamoDbSessionRepository : IDynamoDbSessionRepository
    {
        private const string CreateDateKey = "CreateDate";
        private const string ExpiresKey = "Expires";
        private const string SessionDataKey = "Data";
        private const string RecordFormatKey = "Ver";
        private const string RecordFormat = "1.0";

        private readonly DynamoDbBasedSessionsConfiguration _configuration;
        private readonly Table _table;
        
        public DynamoDbSessionRepository(DynamoDbBasedSessionsConfiguration configuration)
        {
            _configuration = configuration;
            _table = Table.LoadTable(_configuration.DynamoDbClient, _configuration.TableName);
        }
        
        public DynamoDbSessionRecord LoadSession(string sessionId, string applicationName)
        {
            var hashKey = GetHashKey(sessionId, applicationName);
            var document = _table.GetItem(hashKey);

            if (document == null)
            {
                return null;
            }

            return MapDocumentToSessionRecord(document);
        }

        public DynamoDbSessionRecord SaveSession(string sessionId, string applicationName, ISession sessionData, DateTime expires, bool isNew)
        {
            var sessionDocument = new Document();
            var hashKey = GetHashKey(sessionId, applicationName);

            sessionDocument[ExpiresKey] = expires;
            sessionDocument[SessionDataKey] = _configuration.SessionSerializer.Serialize(sessionData);
            sessionDocument[RecordFormatKey] = RecordFormat;
            sessionDocument[_configuration.SessionIdAttributeName] = hashKey;

            if (isNew)
            {
                sessionDocument[CreateDateKey] = DateTime.UtcNow;
                _table.PutItem(sessionDocument);
                return MapDocumentToSessionRecord(sessionDocument, false);
            }

            var updatedDoc = _table.UpdateItem(sessionDocument, new UpdateItemOperationConfig
            {
                ReturnValues = ReturnValues.AllOldAttributes
            });

            sessionDocument[CreateDateKey] = ParseDateTimeFromDynamoDbEntry(updatedDoc[CreateDateKey]);

            return MapDocumentToSessionRecord(sessionDocument, false);
        }

        public void DeleteSession(string sessionId, string applicationName)
        {
            var hashKey = GetHashKey(sessionId, applicationName);
            _table.DeleteItem(hashKey);
        }

        public string GetHashKey(string sessionId, string applicationName)
        {
            return new HashKeyInfo(sessionId, applicationName).HashKey;
        }

        private DynamoDbSessionRecord MapDocumentToSessionRecord(Document document, bool handleDatesFromDynamo = true)
        {
            var expires = document[ExpiresKey].AsDateTime();
            var created = document[CreateDateKey].AsDateTime();
            
            if (handleDatesFromDynamo)
            {
                expires = ParseDateTimeFromDynamoDbEntry(document[ExpiresKey]);
                created = ParseDateTimeFromDynamoDbEntry(document[CreateDateKey]);
            }

            // Need to be explicit about date time parsing
            
            var hashKeyInfo = new HashKeyInfo(document[_configuration.SessionIdAttributeName].AsString());
            var session = _configuration.SessionSerializer.Deserialize(document[SessionDataKey].AsString());

            return new DynamoDbSessionRecord(hashKeyInfo.SessionId, hashKeyInfo.ApplicationName, expires, session, created);
        }

        private DateTime ParseDateTimeFromDynamoDbEntry(DynamoDBEntry entry)
        {
            return DateTime.SpecifyKind(DateTime.Parse(entry.AsString(), null, DateTimeStyles.RoundtripKind), DateTimeKind.Utc);
        }

        private class HashKeyInfo
        {
            public HashKeyInfo(string sessionId, string applicationName)
            {
                ApplicationName = applicationName;
                SessionId = sessionId;
                HashKey = String.Format("{0}_{1}", applicationName, sessionId);
            }
            public HashKeyInfo(string hashKey)
            {
                var tokens = hashKey.Split('_');

                if (tokens.Length != 2)
                {
                    throw new ArgumentException("Hashkey has invalid format", "hashKey");
                }

                ApplicationName = tokens[0];
                SessionId = tokens[1];
                HashKey = hashKey;
            }

            public string HashKey { get; private set; }
            public string SessionId { get; private set; }
            public string ApplicationName { get; private set; }
        }
    }
}