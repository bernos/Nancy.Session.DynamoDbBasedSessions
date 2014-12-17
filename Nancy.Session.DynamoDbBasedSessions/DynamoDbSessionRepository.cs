using System;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.OpsWorks.Model;

namespace Nancy.Session
{
    public interface IDynamoDbSessionRepository
    {
        string GetSession(string sessionId, string applicationName);
        void SaveSession(string sessionId, string applicationName, string sessionData, DateTime expires, bool isNew);
        void DeleteSession(string sessionId, string applicationName);
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
        private const string SessionIdKey = "SessionIdKey";
        private const string CreateDateKey = "CreateDate";
        private const string ExpiresKey = "Expires";
        private const string SessionDataKey = "Data";
        private const string RecordFormatKey = "Ver";
        private const string RecordFormat = "1.0";

        private readonly string _tableName;

        public DynamoDbSessionRepository(string tableName)
        {
            _tableName = tableName;
        }
        
        public string GetSession(string sessionId, string applicationName)
        {
            throw new NotImplementedException();
        }

        public void SaveSession(string sessionId, string applicationName, string sessionData, DateTime expires, bool isNew)
        {
            throw new NotImplementedException();

            var sessionDocument = new Document();

            sessionDocument[SessionIdKey] = GetHashKey(sessionId, applicationName);
            sessionDocument[ExpiresKey] = expires;
            sessionDocument[SessionDataKey] = sessionData;
            sessionDocument[RecordFormatKey] = RecordFormat;

            if (isNew)
            {
                sessionDocument[CreateDateKey] = DateTime.UtcNow;
            }

            // Now go and save it


            
        }

        public void DeleteSession(string sessionId, string applicationName)
        {
            throw new NotImplementedException();
        }

        public static string GetHashKey(string sessionId, string applicationName)
        {
            return String.Format("{0}-{1}", applicationName, sessionId);
        }
    }
}