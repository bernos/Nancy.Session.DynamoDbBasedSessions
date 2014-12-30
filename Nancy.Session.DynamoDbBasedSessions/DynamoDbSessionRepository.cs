using Amazon.DynamoDBv2.DocumentModel;
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
        
        public DynamoDbSessionRecord LoadSession(Guid sessionId, string applicationName)
        {
            var hashKey = GetHashKey(sessionId, applicationName);
            var document = _table.GetItem(hashKey);

            return document == null ? null : MapDocumentToSessionRecord(document);
        }

        public DynamoDbSessionRecord SaveSession(string applicationName, ISession sessionData, DateTime expires)
        {
            return SaveSession(Guid.Empty, applicationName, sessionData, expires);
        }

        public DynamoDbSessionRecord SaveSession(Guid sessionId, string applicationName, ISession sessionData, DateTime expires)
        {
            var isNew = sessionId == Guid.Empty;
            var sessionDocument = new Document();
            
            sessionDocument[ExpiresKey] = expires;
            sessionDocument[SessionDataKey] = _configuration.SessionSerializer.Serialize(sessionData);
            sessionDocument[RecordFormatKey] = RecordFormat;
            
            return isNew ? AddSession(applicationName, sessionDocument) : UpdateSession(sessionId, applicationName, sessionDocument);
        }

        public void DeleteSession(Guid sessionId, string applicationName)
        {
            var hashKey = GetHashKey(sessionId, applicationName);
            _table.DeleteItem(hashKey);
        }

        public string GetHashKey(Guid sessionId, string applicationName)
        {
            return new HashKeyInfo(sessionId, applicationName).HashKey;
        }

        private DynamoDbSessionRecord AddSession(string applicationName, Document session)
        {
            var sessionId = Guid.NewGuid();

            session[CreateDateKey] = DateTime.UtcNow;
            session[_configuration.SessionIdAttributeName] = GetHashKey(sessionId, applicationName);

            _table.PutItem(session);
            return MapDocumentToSessionRecord(session, false);
        }

        private DynamoDbSessionRecord UpdateSession(Guid sessionId, string applicationName, Document session)
        {
            session[_configuration.SessionIdAttributeName] = GetHashKey(sessionId, applicationName);

            var updatedDoc = _table.UpdateItem(session, new UpdateItemOperationConfig
            {
                ReturnValues = ReturnValues.AllOldAttributes
            });

            session[CreateDateKey] = ParseDateTimeFromDynamoDbEntry(updatedDoc[CreateDateKey]);

            return MapDocumentToSessionRecord(session, false);
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
            public HashKeyInfo(Guid sessionId, string applicationName)
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
                SessionId = Guid.Parse(tokens[1]);
                HashKey = hashKey;
            }

            public string HashKey { get; private set; }
            public Guid SessionId { get; private set; }
            public string ApplicationName { get; private set; }
        }
    }
}