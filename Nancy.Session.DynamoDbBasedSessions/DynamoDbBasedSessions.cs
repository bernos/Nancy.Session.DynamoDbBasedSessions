using System;
using System.Collections.Generic;
using Nancy.Bootstrapper;
using Nancy.DynamoDbBasedSessions;

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

        public void Save(ISession session, Response response)
        {
            
        }

        public ISession Load(Request request)
        {
            return new Session(new Dictionary<string, object>());
        }

        public void WithSerializer(IObjectSerializer newSerializer)
        {
            _configuration.Serializer = newSerializer;
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
            sessionStore.Save(context.Request.Session, context.Response);
        }
    }
}