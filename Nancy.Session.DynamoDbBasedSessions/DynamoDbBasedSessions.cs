using Nancy.Bootstrapper;
using Nancy.Cookies;
using Nancy.DynamoDbBasedSessions;
using System;
using System.Collections.Generic;

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
            sessionStore.Save(context.Request, context.Response);
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

        public DynamoDbSessionRecord Save(Request request, Response response)
        {
            var cookieName = Configuration.SessionIdCookieName;
            var sessionId = request.Cookies.ContainsKey(cookieName)
                ? Guid.Parse(request.Cookies[cookieName])
                : Guid.Empty;

            if (request.Session == null)
            {
                return null;
            }

            var expires = DateTime.UtcNow.AddMinutes(Configuration.SessionTimeOutInMinutes);
            var record = Configuration.Repository.SaveSession(sessionId, Configuration.ApplicationName, request.Session, expires);

            response.WithCookie(new NancyCookie(Configuration.SessionIdCookieName, record.SessionId.ToString())
            {
                Expires = expires.AddSeconds(10)
            });

            return record;
        }
        
        public ISession Load(Request request)
        {
            if (request.Cookies.ContainsKey(Configuration.SessionIdCookieName))
            {
                var sessionId = Guid.Parse(request.Cookies[Configuration.SessionIdCookieName]);

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