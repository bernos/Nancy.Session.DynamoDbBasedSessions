using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Nancy.DynamoDbBasedSessions;
using System;
using System.Globalization;

namespace Nancy.Session
{
    public class DynamoDbSessionRepository : IDynamoDbSessionRepository, IDisposable
    {
        private const string CreateDateKey = "CreateDate";
        private const string ExpiresKey = "Expires";
        private const string SessionDataKey = "Data";
        private const string RecordFormatKey = "Ver";
        private const string RecordFormat = "1.0";

        private readonly DynamoDbBasedSessionsConfiguration _configuration;
        private readonly IAmazonDynamoDB _client;
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

            if (document == null)
            {
                return null;
            }

            // Need to be explicit about date time parsing
            var expires = DateTime.SpecifyKind(DateTime.Parse(document[ExpiresKey].AsString(), null, DateTimeStyles.RoundtripKind), DateTimeKind.Utc);
            var created = DateTime.SpecifyKind(DateTime.Parse(document[CreateDateKey].AsString(), null, DateTimeStyles.RoundtripKind), DateTimeKind.Utc);

            var session = _configuration.SessionSerializer.Deserialize(document[SessionDataKey].AsString());

            return new DynamoDbSessionRecord(sessionId, applicationName, expires, session, created);
        }

        public void SaveSession(string sessionId, string applicationName, ISession sessionData, DateTime expires, bool isNew)
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
            }
            else
            {
                _table.UpdateItem(sessionDocument);
            }
        }

        public void DeleteSession(string sessionId, string applicationName)
        {
            var hashKey = GetHashKey(sessionId, applicationName);
            _table.DeleteItem(hashKey);
        }

        public string GetHashKey(string sessionId, string applicationName)
        {
            return String.Format("{0}-{1}", applicationName, sessionId);
        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}