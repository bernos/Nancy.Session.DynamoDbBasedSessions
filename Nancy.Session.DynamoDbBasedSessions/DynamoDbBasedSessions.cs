using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Nancy.Bootstrapper;
using Nancy.Cookies;
using Nancy.Cryptography;
using Nancy.DynamoDbBasedSessions;
using Nancy.Helpers;

namespace Nancy.Session
{
    public class DynamoDbBasedSessions : IObjectSerializerSelector
    {
        private readonly DynamoDbBasedSessionsConfiguration _configuration;

        public static IObjectSerializerSelector Enable(IPipelines pipelines, DynamoDbBasedSessionsConfiguration configuration)
        {
            if (pipelines == null)
            {
                throw new ArgumentNullException("pipelines");
            }

            var sessionStore = new DynamoDbBasedSessions(configuration);

            pipelines.BeforeRequest.AddItemToStartOfPipeline(ctx => LoadSession(ctx, sessionStore));
            pipelines.AfterRequest.AddItemToEndOfPipeline(ctx => SaveSession(ctx, sessionStore));

            return sessionStore;
        }

        public static Response LoadSession(NancyContext context, DynamoDbBasedSessions sessionStore)
        {
            if (context.Request == null)
            {
                return null;
            }

            sessionStore.Load(context.Request);

            return null;
        }

        public static void SaveSession(NancyContext context, DynamoDbBasedSessions sessionStore)
        {
            var cookieName = sessionStore.Configuration.SessionIdCookieName;
            var isNew = context.Request.Cookies.ContainsKey(cookieName);
            var sessionId = isNew ? context.Request.Cookies[cookieName] : Guid.NewGuid().ToString();

            sessionStore.Save(sessionId, context.Request.Session, context.Response, isNew);
        }

        public DynamoDbBasedSessions(DynamoDbBasedSessionsConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            if (!configuration.IsValid)
            {
                throw new ArgumentException("DynamoDbBasedSessionsConfiguration is invalid", "configuration");
            }

            _configuration = configuration;

            SetupTable();
        }

        public DynamoDbBasedSessionsConfiguration Configuration
        {
            get { return _configuration; }
        }

        public void Save(string sessionId, ISession session, Response response, bool isNew)
        {
            if (session == null)
            {
                return;
            }

            var data = Encrypt(Serialize(session));
            var expires = DateTime.UtcNow.AddMinutes(Configuration.SessionTimeOutInMinutes);
            
            Configuration.Repository.SaveSession(sessionId, Configuration.ApplicationName, data, expires, isNew);
            
            response.WithCookie(new NancyCookie(Configuration.SessionIdCookieName, sessionId)
            {
                Expires = expires.AddSeconds(10)
            });
        }

        public ISession Load(Request request)
        {
            if (request.Cookies.ContainsKey(Configuration.SessionIdCookieName))
            {
                var sessionId = request.Cookies[Configuration.SessionIdCookieName];

                // TODO: Load the session from dynamodb
                var document = Configuration.Repository.LoadSession(sessionId, Configuration.ApplicationName);

                // TODO: If the session has expired, delete it. Also delete the sessionId cookie in the request, to ensure we dont try to resave against the expired session id during the end request handler
                //request.Cookies.Remove(Configuration.SessionIdCookieName);

                

                return new Session(Deserialize(Decrypt(document.Data)));
            }

            return new Session(new Dictionary<string, object>());
        }

        public void WithSerializer(IObjectSerializer newSerializer)
        {
            _configuration.Serializer = newSerializer;
        }

        private string Serialize(ISession session)
        {
            var sb = new StringBuilder();

            foreach (var kvp in session)
            {
                var objectString = Configuration.Serializer.Serialize(kvp.Value);

                sb.Append(HttpUtility.UrlEncode(kvp.Key));
                sb.Append("=");
                sb.Append(objectString);
                sb.Append(";");
            }

            return sb.ToString();
        }

        private IDictionary<string, object> Deserialize(string data)
        {
            var dictionary = new Dictionary<string, object>();

            if (!String.IsNullOrEmpty(data))
            {
                var tokens = data.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries);

                foreach (var token in tokens)
                {
                    var rawKey = token.Substring(0, token.IndexOf('='));
                    var rawValue = token.Substring(1 + token.IndexOf('='));

                    var value = Configuration.Serializer.Deserialize(HttpUtility.UrlDecode(rawValue));
                    dictionary[HttpUtility.UrlDecode(rawKey)] = value;
                }
                /*
                foreach (var pair in tokens.Select(t => t.Split('=')).Where(p => p.Length == 2))
                {
                    var value = Configuration.Serializer.Deserialize(HttpUtility.UrlDecode(pair[1]));
                    dictionary[HttpUtility.UrlDecode(pair[0])] = value;
                }*/
            }

            return dictionary;
        } 

        private string Encrypt(string clearText)
        {
            var cryptographyConfiguration = Configuration.CryptographyConfiguration;
            var encryptedData = cryptographyConfiguration.EncryptionProvider.Encrypt(clearText);
            var hmacBytes = cryptographyConfiguration.HmacProvider.GenerateHmac(encryptedData);

            return string.Format("{0}{1}", Convert.ToBase64String(hmacBytes), encryptedData);
        }

        private string Decrypt(string cypherText)
        {
            var cryptographyConfiguration = Configuration.CryptographyConfiguration;
            var hmacLength = Base64Helpers.GetBase64Length(cryptographyConfiguration.HmacProvider.HmacLength);
            var hmacString = cypherText.Substring(0, hmacLength);
            var encryptedData = cypherText.Substring(hmacLength);
            var hmacBytes = Convert.FromBase64String(hmacString);
            var newHmac = cryptographyConfiguration.HmacProvider.GenerateHmac(encryptedData);
            var isHmacValid = HmacComparer.Compare(newHmac, hmacBytes, cryptographyConfiguration.HmacProvider.HmacLength);

            return isHmacValid ? cryptographyConfiguration.EncryptionProvider.Decrypt(encryptedData) : string.Empty;
        }

        private void SetupTable()
        {
            using (var client = Configuration.CreateClient())
            {
                try
                {
                    var table = Table.LoadTable(client, Configuration.TableName);
                    ValidateTable(table);
                }
                catch (ResourceNotFoundException e)
                {
                    var table = CreateTable(client);
                    ValidateTable(table);
                }
            }
        }

        private Table CreateTable(AmazonDynamoDBClient client)
        {
            var request = new CreateTableRequest
            {
                TableName = Configuration.TableName,
                KeySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement
                    {
                        AttributeName = Configuration.SessionIdAttributeName,
                        KeyType = KeyType.HASH
                    }
                },
                AttributeDefinitions = new List<AttributeDefinition>
                {
                    new AttributeDefinition
                    {
                        AttributeName = Configuration.SessionIdAttributeName,
                        AttributeType = ScalarAttributeType.S
                    }
                },
                ProvisionedThroughput = new ProvisionedThroughput
                {
                    ReadCapacityUnits = Configuration.ReadCapacityUnits,
                    WriteCapacityUnits = Configuration.WriteCapacityUnits
                }
            };

            var response = client.CreateTable(request);

            var describeTableRequest = new DescribeTableRequest
            {
                TableName = Configuration.TableName
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

            return Table.LoadTable(client, Configuration.TableName);
        }

        private void ValidateTable(Table table)
        {
            if (table.HashKeys.Count != 1)
            {
                throw new AmazonDynamoDBException(string.Format("Table {0} can't be used to store session data as it does not have a single hash key", Configuration.TableName));
            }

            var key = table.HashKeys[0];
            var keyDescription = table.Keys[key];

            if (keyDescription.Type != DynamoDBEntryType.String)
            {
                throw new AmazonDynamoDBException(string.Format("Table {0} can't be used to store session data because its hash key is not a string", Configuration.TableName));
            }

            if (table.RangeKeys.Count > 0)
            {
                throw new AmazonDynamoDBException(string.Format("Table {0} can't be used to store session data because it contains a range key"));
            }
        }
    }
}