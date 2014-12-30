using System;

namespace Nancy.Session
{
    public interface IDynamoDbSessionRepository
    {
        DynamoDbSessionRecord LoadSession(Guid sessionId, string applicationName);
        DynamoDbSessionRecord SaveSession(string applicationName, ISession sessionData, DateTime expires);
        DynamoDbSessionRecord SaveSession(Guid sessionId, string applicationName, ISession sessionData, DateTime expires);
        void DeleteSession(Guid sessionId, string applicationName);
        string GetHashKey(Guid sessionId, string applicationName);
    }
}