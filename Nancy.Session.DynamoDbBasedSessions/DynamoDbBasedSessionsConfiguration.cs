using System;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Nancy.Cryptography;
using Nancy.Session;

namespace Nancy.DynamoDbBasedSessions
{
    public class DynamoDbBasedSessionsConfiguration
    {
        private const string DefaultSessionIdCookieName = "__sid__";
        private const string DefaultSessionIdAttributeName = "SessionId";
        private const string DefaultTableName = "NancySessions";
        private const int DefaultSessionTimeoutInMinutes = 30;
        private const int DefaultReadCapacityUnits = 10;
        private const int DefaultWriteCapacityUnits = 5;

        public DynamoDbBasedSessionsConfiguration(string applicationName)
        {
            if (string.IsNullOrEmpty(applicationName))
            {
                throw new ArgumentNullException("applicationName");    
            }
            
            SessionSerializer = new EncryptedSessionSerializer();
            ApplicationName = applicationName;
            TableName = DefaultTableName;
            SessionIdCookieName = DefaultSessionIdCookieName;
            SessionTimeOutInMinutes = DefaultSessionTimeoutInMinutes;
            DynamoDbConfig = new AmazonDynamoDBConfig();
            ClientFactory = _defaultClientFactory;
            RepositoryFactory = _defaultRepositoryFactory;
            TableInitializerFactory = _defaultInitializerFactor;
            CreateTableIfNotExist = true;
            ReadCapacityUnits = DefaultReadCapacityUnits;
            WriteCapacityUnits = DefaultWriteCapacityUnits;
            SessionIdAttributeName = DefaultSessionIdAttributeName;
        }

        public Func<DynamoDbBasedSessionsConfiguration, IDynamoDbTableInitializer> TableInitializerFactory { get; set; } 
        public Func<DynamoDbBasedSessionsConfiguration, IAmazonDynamoDB> ClientFactory { get; set; }
        public Func<DynamoDbBasedSessionsConfiguration, IDynamoDbSessionRepository> RepositoryFactory { get; set; } 
        public RegionEndpoint RegionEndpoint { get; set; }
        public AmazonDynamoDBConfig DynamoDbConfig { get; set; }
        public string ApplicationName { get; set; }
        public ISessionSerializer SessionSerializer { get; set; }
        
        public string SessionIdCookieName { get; set; }
        public int SessionTimeOutInMinutes { get; set; }
        public string TableName { get; set; }
        public bool CreateTableIfNotExist { get; set; }
        public string SessionIdAttributeName { get; set; }
        public string ProfileName { get; set; }
        public string AccessKeyId { get; set; }
        public string SecretAccessKey { get; set; }
        public int ReadCapacityUnits { get; set; }
        public int WriteCapacityUnits { get; set; }

        private IDynamoDbSessionRepository _repository;

        public IDynamoDbSessionRepository Repository
        {
            get
            {
                if (_repository == null)
                {
                    _repository = RepositoryFactory(this);
                }
                return _repository;
            }
        }

        private IDynamoDbTableInitializer _tableInitializer;

        public IDynamoDbTableInitializer TableInitializer
        {
            get
            {
                if (_tableInitializer == null)
                {
                    _tableInitializer = TableInitializerFactory(this);
                }
                return _tableInitializer;
            }
        }
        
        public bool IsValid
        {
            get
            {
                if (string.IsNullOrEmpty(TableName))
                {
                    return false;
                }

                if (SessionSerializer == null)
                {
                    return false;
                }

                if (String.IsNullOrEmpty(ApplicationName))
                {
                    return false;
                }

                return true;
            }
        }
        
        public IAmazonDynamoDB CreateClient()
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

            return new AmazonDynamoDBClient(c.DynamoDbConfig);
        };

        private readonly Func<DynamoDbBasedSessionsConfiguration, IDynamoDbTableInitializer> _defaultInitializerFactor = c => new DynamoDbTableInitializer(c);

        private readonly Func<DynamoDbBasedSessionsConfiguration, IDynamoDbSessionRepository> _defaultRepositoryFactory = c => new DynamoDbSessionRepository(c);
    }
}