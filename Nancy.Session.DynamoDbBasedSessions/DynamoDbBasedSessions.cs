using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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
            var sessionId = context.Request.Cookies.ContainsKey(cookieName)
                                ? context.Request.Cookies[cookieName]
                                : Guid.NewGuid().ToString();

            sessionStore.Save(sessionId, context.Request.Session, context.Response);
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
        }

        public DynamoDbBasedSessionsConfiguration Configuration
        {
            get { return _configuration; }
        }

        public void Save(string sessionId, ISession session, Response response)
        {
            if (session == null || !session.HasChanged)
            {
                return;
            }

            var data = Encrypt(Serialize(session));

            // TODO: Save the data to dynamodb
            
            response.WithCookie(new NancyCookie(Configuration.SessionIdCookieName, sessionId)
            {
                Expires = DateTime.UtcNow.AddMinutes(Configuration.SessionTimeOutInMinutes)
            });
        }

        public ISession Load(Request request)
        {
            if (request.Cookies.ContainsKey(Configuration.SessionIdCookieName))
            {
                var sessionId = request.Cookies[Configuration.SessionIdCookieName];

                // TODO: Load the session from dynamodb
                string value = "";

                return new Session(Deserialize(Decrypt(value)));
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

                foreach (var pair in tokens.Select(t => t.Split('=')).Where(p => p.Length == 2))
                {
                    var value = Configuration.Serializer.Deserialize(HttpUtility.UrlDecode(pair[1]));
                    dictionary[HttpUtility.UrlDecode(pair[0])] = value;
                }
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
    }
}