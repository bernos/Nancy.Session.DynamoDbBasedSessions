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
            var sessionId = context.Request.Cookies.ContainsKey(cookieName)
                ? context.Request.Cookies[cookieName]
                : String.Empty;

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

            _configuration.TableInitializer.Initialize();
        }

        public DynamoDbBasedSessionsConfiguration Configuration
        {
            get { return _configuration; }
        }

        public DynamoDbSessionRecord Save(string sessionId, ISession session, Response response)
        {
            if (session == null)
            {
                return null;
            }

            var expires = DateTime.UtcNow.AddMinutes(Configuration.SessionTimeOutInMinutes);
            var record = Configuration.Repository.SaveSession(sessionId, Configuration.ApplicationName, session, expires);
            
            response.WithCookie(new NancyCookie(Configuration.SessionIdCookieName, record.SessionId)
            {
                Expires = expires.AddSeconds(10)
            });

            return record;
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
            }

            return new Session(new Dictionary<string, object>());
        }


        
    }
}