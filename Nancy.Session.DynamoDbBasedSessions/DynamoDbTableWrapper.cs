using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;

namespace Nancy.Session
{
    public interface IDynamoDbTable
    {
        Document GetItem(Primitive hashKey, GetItemOperationConfig config = null);
        Document DeleteItem(Primitive hashKey, DeleteItemOperationConfig config = null);
        Document PutItem(Document doc, PutItemOperationConfig config = null);
        Document UpdateItem(Document doc, UpdateItemOperationConfig config = null);
    }

    public class DynamoDbTableWrapper : IDynamoDbTable
    {
        private readonly Table _table;

        public DynamoDbTableWrapper(IAmazonDynamoDB client, string tableName)
        {
            _table = Table.LoadTable(client, tableName);
        }

        public Document GetItem(Primitive hashKey, GetItemOperationConfig config = null)
        {
            return _table.GetItem(hashKey, config);
        }

        public Document DeleteItem(Primitive hashKey, DeleteItemOperationConfig config = null)
        {
            return _table.DeleteItem(hashKey, config);
        }

        public Document PutItem(Document doc, PutItemOperationConfig config = null)
        {
            return _table.PutItem(doc, config);
        }

        public Document UpdateItem(Document doc, UpdateItemOperationConfig config = null)
        {
            return _table.UpdateItem(doc, config);
        }
    }
}