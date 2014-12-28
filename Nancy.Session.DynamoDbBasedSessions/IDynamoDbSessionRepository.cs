using System;

namespace Nancy.Session
{
    public interface IDynamoDbSessionRepository
    {
        DynamoDbSessionRecord LoadSession(string sessionId, string applicationName);
        void SaveSession(string sessionId, string applicationName, ISession sessionData, DateTime expires, bool isNew);
        void DeleteSession(string sessionId, string applicationName);
        string GetHashKey(string sessionId, string applicationName);
    }
}