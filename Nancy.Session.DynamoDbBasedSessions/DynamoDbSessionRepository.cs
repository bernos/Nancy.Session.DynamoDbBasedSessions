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
        public static string GetHashKey(string sessionId, string applicationName)
        {
            return string.Format("{0}-{1}", applicationName, sessionId);
        }

        public string ApplicationName { get; private set; }
        public string SessionId { get; private set; }
        public string Data { get; private set; }
        public DateTime CreateDate { get; private set; }
        public DateTime Expires { get; private set; }
        public String RecordFormatVersion { get; private set; }

        public DynamoDbSessionRecord(   string sessionId, 
                                        string applicationName, 
                                        DateTime expires, 
                                        string data) : this(sessionId, applicationName, expires, data, DateTime.UtcNow)
        {
            
        }

        public DynamoDbSessionRecord(string sessionId, string applicationName, DateTime expires, string data, DateTime createDate)
        {
            SessionId = sessionId;
            ApplicationName = applicationName;
            Expires = expires;
            Data = data;
            CreateDate = createDate;
        }

        public DynamoDbSessionRecord(Document document)
        {
            // TODO: populate from document
        }

        public Document AsDocument()
        {
            var document = new Document();

            return document;
        }
    }

    public class DynamoDbSessionRepository : IDynamoDbSessionRepository
    {
        private const string CreateDateKey = "CreateDate";
        private const string ExpiresKey = "Expires";
        private const string SessionDataKey = "Data";
        private const string RecordFormatKey = "Ver";

        private readonly string _sessionIdKey;
        private readonly string _tableName;

        public DynamoDbSessionRepository(string sessionIdKey, string tableName)
        {
            _sessionIdKey = sessionIdKey;
            _tableName = tableName;
        }
        
        public string GetSession(string sessionId, string applicationName)
        {
            throw new System.NotImplementedException();
        }

        public void SaveSession(string sessionId, string applicationName, string sessionData, DateTime expires, bool isNew)
        {
            throw new System.NotImplementedException();

            // TODO: Deal with creation times properly. At this point we dont know whether the session we are dealing with is new or already exists,
            // so it is ambiguous whether to set create time now, or leave it as is in the db
            var document = new DynamoDbSessionRecord(sessionId, applicationName, expires, sessionData).AsDocument();

            if (isNew)
            {
                document.Remove("CreateDate");
            }

            
        }

        public void DeleteSession(string sessionId, string applicationName)
        {
            throw new NotImplementedException();
        }
    }
}