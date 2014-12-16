namespace Nancy.Session
{
    public class DynamoDbSessionStore
    {
        private const string CreateDateKey = "CreateDate";
        private const string ExpiresKey = "Expires";
        private const string SessionDataKey = "SessionData";
        private const string RecordFormatKey = "Ver";

        private readonly string _sessionIdKey;
        private readonly string _tableName;

        public DynamoDbSessionStore(string sessionIdKey, string tableName)
        {
            _sessionIdKey = sessionIdKey;
            _tableName = tableName;
        }


    }
}