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
    public class DynamoDbBasedSessions
    {
        private readonly DynamoDbBasedSessionsConfiguration _configuration;

        public static void Enable(IPipelines pipelines, DynamoDbBasedSessionsConfiguration configuration)
        {
            if (pipelines == null)
            {
                throw new ArgumentNullException("pipelines");
            }

            var sessionStore = new DynamoDbBasedSessions(configuration);

            pipelines.BeforeRequest.AddItemToStartOfPipeline(ctx => LoadSession(ctx, sessionStore));
            pipelines.AfterRequest.AddItemToEndOfPipeline(ctx => SaveSession(ctx, sessionStore));
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

            _configuration.TableInitializer.Initialize();
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

            //var data = Encrypt(Serialize(session));
            //var data = Configuration.SessionSerializer.Serialize(session);
            var expires = DateTime.UtcNow.AddMinutes(Configuration.SessionTimeOutInMinutes);
            
            Configuration.Repository.SaveSession(sessionId, Configuration.ApplicationName, session, expires, isNew);
            
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

                var session = Configuration.Repository.LoadSession(sessionId, Configuration.ApplicationName);

                if (session.HasExpired)
                {
                    Configuration.Repository.DeleteSession(sessionId, Configuration.ApplicationName);
                    request.Cookies.Remove(Configuration.SessionIdCookieName);

                    return new Session(new Dictionary<string, object>());
                }

                return session.Data;
                //return Configuration.SessionSerializer.Deserialize(session.Data);
                //return new Session(Deserialize(Decrypt(session.Data)));
            }

            return new Session(new Dictionary<string, object>());
        }


        
    }
}