namespace Nancy.DynamoDbBasedSessions
{
    public class DynamoDbBasedSessionsConfiguration
    {
        private readonly string _tableName;

        public DynamoDbBasedSessionsConfiguration(string tableName)
        {
            _tableName = tableName;
        }

        public IObjectSerializer Serializer { get; set; }

        public bool IsValid
        {
            get
            {
                if (string.IsNullOrEmpty(_tableName))
                {
                    return false;
                }

                if (Serializer == null)
                {
                    return false;
                }

                return true;
            }
        }
    }
}