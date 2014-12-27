using System.Collections.Generic;
using System.Threading;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Nancy.DynamoDbBasedSessions;

namespace Nancy.Session
{
    public class DynamoDbTableInitializer : IDynamoDbTableInitializer
    {
        private readonly DynamoDbBasedSessionsConfiguration _configuration;

        public DynamoDbTableInitializer(DynamoDbBasedSessionsConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void Initialize()
        {
            using (var client = _configuration.CreateClient())
            {
                try
                {
                    var table = Table.LoadTable(client, _configuration.TableName);
                    ValidateTable(table);
                }
                catch (ResourceNotFoundException e)
                {
                    var table = CreateTable(client);
                    ValidateTable(table);
                }
            }
        }

        private Table CreateTable(IAmazonDynamoDB client)
        {
            var request = new CreateTableRequest
            {
                TableName = _configuration.TableName,
                KeySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement
                    {
                        AttributeName = _configuration.SessionIdAttributeName,
                        KeyType = KeyType.HASH
                    }
                },
                AttributeDefinitions = new List<AttributeDefinition>
                {
                    new AttributeDefinition
                    {
                        AttributeName = _configuration.SessionIdAttributeName,
                        AttributeType = ScalarAttributeType.S
                    }
                },
                ProvisionedThroughput = new ProvisionedThroughput
                {
                    ReadCapacityUnits = _configuration.ReadCapacityUnits,
                    WriteCapacityUnits = _configuration.WriteCapacityUnits
                }
            };

            var response = client.CreateTable(request);

            var describeTableRequest = new DescribeTableRequest
            {
                TableName = _configuration.TableName
            };

            var isActive = false;

            while (!isActive)
            {
                Thread.Sleep(5000);
                var describeTableResponse = client.DescribeTable(describeTableRequest);
                var status = describeTableResponse.Table.TableStatus;

                if (status == TableStatus.ACTIVE)
                {
                    isActive = true;
                }
            }

            return Table.LoadTable(client, _configuration.TableName);
        }

        private void ValidateTable(Table table)
        {
            if (table.HashKeys.Count != 1)
            {
                throw new AmazonDynamoDBException(string.Format("Table {0} can't be used to store session data as it does not have a single hash key", _configuration.TableName));
            }

            var key = table.HashKeys[0];
            var keyDescription = table.Keys[key];

            if (keyDescription.Type != DynamoDBEntryType.String)
            {
                throw new AmazonDynamoDBException(string.Format("Table {0} can't be used to store session data because its hash key is not a string", _configuration.TableName));
            }

            if (table.RangeKeys.Count > 0)
            {
                throw new AmazonDynamoDBException(string.Format("Table {0} can't be used to store session data because it contains a range key", _configuration.TableName));
            }
        }
    }
}