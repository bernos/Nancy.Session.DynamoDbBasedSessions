using System;

namespace Nancy.Session
{
    public class DynamoDbSessionRecord
    {
        public string ApplicationName { get; private set; }
        public Guid SessionId { get; private set; }
        public ISession Data { get; private set; }
        public DateTime CreateDate { get; private set; }
        public DateTime Expires { get; private set; }
        public String RecordFormatVersion { get; private set; }

        public bool HasExpired
        {
            get { return DateTime.Compare(Expires, DateTime.UtcNow) == -1; }
        }

        public DynamoDbSessionRecord(Guid sessionId, string applicationName, DateTime expires, ISession data, DateTime createDate)
        {
            if (sessionId == Guid.Empty)
            {
                throw new ArgumentNullException("sessionId");
            }

            if (string.IsNullOrEmpty(applicationName))
            {
                throw new ArgumentNullException("applicationName");
            }

            SessionId = sessionId;
            ApplicationName = applicationName;
            Expires = expires;
            Data = data;
            CreateDate = createDate;
        }
    }
}