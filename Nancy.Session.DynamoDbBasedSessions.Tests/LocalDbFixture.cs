using System;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Nancy.DynamoDbBasedSessions;

namespace Nancy.Session.Tests
{
    public class LocalDbFixture : IDisposable
    {
        private readonly DynamoDbBasedSessionsConfiguration _configuration;
        private readonly Table _table;

        public DynamoDbBasedSessionsConfiguration Configuration { get { return _configuration; } }
        public Table Table { get { return _table; } }

        public LocalDbFixture()
        {
            var name = GetType().Name;

            _configuration = new DynamoDbBasedSessionsConfiguration(name)
            {
                TableName = string.Format("{0}_Table", name),
                DynamoDbConfig = new AmazonDynamoDBConfig
                {
                    ServiceURL = "http://localhost:8000"
                }
            };

            var initializer = new DynamoDbTableInitializer(_configuration);
            initializer.Initialize();

            _table = Table.LoadTable(_configuration.DynamoDbClient, _configuration.TableName);
        }

        public void Dispose()
        {
            _configuration.DynamoDbClient.DeleteTable(_configuration.TableName);
            _configuration.Dispose();
        }


    }
}