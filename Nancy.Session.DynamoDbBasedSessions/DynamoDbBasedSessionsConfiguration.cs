using Amazon;
using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Nancy.Session;
using System;

namespace Nancy.DynamoDbBasedSessions
{
    public class DynamoDbBasedSessionsConfiguration : IDisposable
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
            ReadCapacityUnits = DefaultReadCapacityUnits;
            WriteCapacityUnits = DefaultWriteCapacityUnits;
            SessionIdAttributeName = DefaultSessionIdAttributeName;
        }

        /// <summary>
        /// The name of our application
        /// </summary>
        public string ApplicationName { get; set; }

        /// <summary>
        /// Factory method that creates an instance of an IDynamodDbTableInitializer to initialize the
        /// dynamodb table when the application starts up. The default implementation creates an instance
        /// of the DynamoDbTableInitializer class, which will attempt to create the table if it does not
        /// exist
        /// </summary>
        public Func<DynamoDbBasedSessionsConfiguration, IDynamoDbTableInitializer> TableInitializerFactory { private get; set; } 
        
        /// <summary>
        /// Factory method that creates our dynamodb client
        /// </summary>
        public Func<DynamoDbBasedSessionsConfiguration, IAmazonDynamoDB> ClientFactory { private get; set; }
        
        /// <summary>
        /// Factory method that creates an IDynamoDbSessionRepository instance. The default implementation
        /// will create an instance of the DynamoDbSessionRepository class.
        /// </summary>
        public Func<DynamoDbBasedSessionsConfiguration, IDynamoDbSessionRepository> RepositoryFactory { private get; set; } 
        
        /// <summary>
        /// The AWS region that the DynamoDb resides within
        /// </summary>
        public RegionEndpoint RegionEndpoint { get; set; }

        /// <summary>
        /// Use this to provide a custom configuration to the DynamoDb client
        /// </summary>
        public AmazonDynamoDBConfig DynamoDbConfig { get; set; }
        
        /// <summary>
        /// Serializer class used for serializing and deserializing the session to and from the dynamo
        /// db table
        /// </summary>
        public ISessionSerializer SessionSerializer { get; set; }
        
        /// <summary>
        /// The name of the http cookie that will contain the session id
        /// </summary>
        public string SessionIdCookieName { get; set; }

        /// <summary>
        /// Length of sessions in minutes
        /// </summary>
        public int SessionTimeOutInMinutes { get; set; }

        /// <summary>
        /// The name of the dynamo db table to store session data in
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// Name of the table attribute to store session id in
        /// </summary>
        public string SessionIdAttributeName { get; set; }

        /// <summary>
        /// Name of the AWS profile to use. In the default ClienFactory implementation this will not be used
        /// if either AccessKeyId or SecretAccessKey configuration options are set.
        /// </summary>
        public string ProfileName { get; set; }

        /// <summary>
        /// The AWS access key to use. Note that it is more preferable to use either AWS profiles or EC2 instance profiles
        /// than hard coded access keys.
        /// </summary>
        public string AccessKeyId { get; set; }

        /// <summary>
        /// The AWS secret access key to use. Note that it is more preferable to use either AWS profiles or EC2 instance profiles
        /// than hard coded access keys.
        /// </summary>
        public string SecretAccessKey { get; set; }

        /// <summary>
        /// The number of read capacity units to specify when creating the dynamo db table
        /// </summary>
        public int ReadCapacityUnits { get; set; }

        /// <summary>
        /// The number of write capacity units to specify when creating the dynamo db table
        /// </summary>
        public int WriteCapacityUnits { get; set; }

        private IDynamoDbSessionRepository _repository;

        /// <summary>
        /// Repository for accessing and persisting session data
        /// </summary>
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

        /// <summary>
        /// The TableInitializer is responsible for initializing the dynamodb table when the application starts up
        /// </summary>
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

        private IAmazonDynamoDB _client;

        /// <summary>
        /// The DynamoDbClient used when interacting with Dynamo Db
        /// </summary>
        public IAmazonDynamoDB DynamoDbClient
        {
            get
            {
                if (_client == null)
                {
                    if (RegionEndpoint != null)
                    {
                        DynamoDbConfig.RegionEndpoint = RegionEndpoint;
                    }

                    _client = ClientFactory(this);    
                }
                return _client;
            }
        }
        
        /// <summary>
        /// Determines whether the current state of the DynamoDbBasedSessionsConfiguration is valid
        /// </summary>
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

        private readonly Func<DynamoDbBasedSessionsConfiguration, IDynamoDbSessionRepository> _defaultRepositoryFactory = c => new DynamoDbSessionRepository(new DynamoDbTableWrapper(c.DynamoDbClient, c.TableName), c);
        public void Dispose()
        {
            DynamoDbClient.Dispose();
        }
    }
}