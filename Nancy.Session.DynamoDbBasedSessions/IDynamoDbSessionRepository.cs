using System;

namespace Nancy.Session
{
    public interface IDynamoDbSessionRepository
    {
        DynamoDbSessionRecord LoadSession(string sessionId, string applicationName);
        DynamoDbSessionRecord SaveSession(string applicationName, ISession sessionData, DateTime expires);
        DynamoDbSessionRecord SaveSession(string sessionId, string applicationName, ISession sessionData, DateTime expires);
        void DeleteSession(string sessionId, string applicationName);
        string GetHashKey(string sessionId, string applicationName);
    }
}