﻿using Nancy.Cryptography;
using Nancy.Session;

namespace Nancy.DynamoDbBasedSessions
{
    public class DynamoDbBasedSessionsConfiguration
    {
        private readonly string _tableName;

        public DynamoDbBasedSessionsConfiguration(string tableName)
        {
            _tableName = tableName;

            SessionIdCookieName = "__sid__";
            SessionTimeOutInMinutes = 30;
            CryptographyConfiguration = CryptographyConfiguration.Default;
            Repository = new DynamoDbSessionRepository("SessionId", tableName);
        }

        public string ApplicationName { get; set; }
        public CryptographyConfiguration CryptographyConfiguration { get; set; }
        public IObjectSerializer Serializer { get; set; }

        public string SessionIdCookieName { get; set; }

        public int SessionTimeOutInMinutes { get; set; }
        public IDynamoDbSessionRepository Repository { get; set; }

        public bool IsValid
        {
            get
            {
                if (string.IsNullOrEmpty(_tableName))
                {
                    return false;
                }

                if (Serializer == null)
                {
                    return false;
                }

                return true;
            }
        }
    }
}