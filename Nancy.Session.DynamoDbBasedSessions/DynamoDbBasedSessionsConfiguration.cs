using System;
using System.Security.Permissions;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Amazon.Util;
using Nancy.Cryptography;
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
            DynamoDbConfig = new AmazonDynamoDBConfig();
            ClientFactory = _defaultClientFactory;
        }

        public Func<DynamoDbBasedSessionsConfiguration, AmazonDynamoDBClient> ClientFactory { get; set; } 
        public RegionEndpoint RegionEndpoint { get; set; }
        public AmazonDynamoDBConfig DynamoDbConfig { get; set; }
        public string ApplicationName { get; set; }
        public CryptographyConfiguration CryptographyConfiguration { get; set; }
        public IObjectSerializer Serializer { get; set; }
        public string SessionIdCookieName { get; set; }
        public int SessionTimeOutInMinutes { get; set; }
        public IDynamoDbSessionRepository Repository { get; set; }
        public string ProfileName { get; set; }
        public string AccessKeyId { get; set; }
        public string SecretAccessKey { get; set; }
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

        public AmazonDynamoDBClient CreateClient()
        {
            if (RegionEndpoint != null)
            {
                DynamoDbConfig.RegionEndpoint = RegionEndpoint;
            }

            return ClientFactory(this);
        }

        private readonly Func<DynamoDbBasedSessionsConfiguration, AmazonDynamoDBClient> _defaultClientFactory = c =>
        {
            if (!String.IsNullOrEmpty(c.AccessKeyId) && !String.IsNullOrEmpty(c.SecretAccessKey))
            {
                return new AmazonDynamoDBClient(c.AccessKeyId, c.SecretAccessKey, c.DynamoDbConfig);
            }

            if (!String.IsNullOrEmpty(c.ProfileName))
            {
                var credentials = new StoredProfileAWSCredentials(c.ProfileName);
                return new AmazonDynamoDBClient(credentials, c.DynamoDbConfig);
            }

            return new AmazonDynamoDBClient();
        };

    }
}