Nancy.Session.DynamoDbBasedSessions
===================================

AWS DynamoDB backing for nancyfx

## Installation

Via nuget
  
  ```
  Install-Package Nancy.Session.DynamoDbBasedSessions
  ```
  
## Usage

### Enabling

The simplest way to enable DynamoDb based session storage is to call the static DynamoDbBasedSessions.Enable method from the ApplicationStartup lifecycle method of your Nancy Bootstrapper.

  ```c#
  protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
	{
		base.ApplicationStartup(container, pipelines);

		// ...
		Nancy.Session.DynamoDbBasedSessions.Enable(pipelines, new Nancy.Session.DynamoDbBasedSessionsConfiguration("MyApplication"));
	}
  ```
  
  The single argument to the `Nancy.Session.DynamoDbBasedSessionsConfiguration` is the name of your application. This will be combined with a guid representing the session id when constructing hashkey values to be written to Dynamo DB. This means that you can easily use one table to store session data for multiple applications.

### Configuring

Behaviour can be configured via the `Nancy.Session.DynamoDbBasedSessionsConfiguration` instance passed to `Nancy.Session.DynamoDbBasedSessions.Enable`. See below for configuration options

  ```c#
  var config = new Nancy.DynamoDbBasedSessions.DynamoDbBasedSessionsConfiguration("MyApplication");

  // The AWS region that the DynamoDb instance is hosted in. Default value will be determined by the DynamoDb client if no value is provided here
  config.RegionEndpoint = RegionEndpoint.USWest1;
  
  // Custom AmazonDynamoDbConfig instance to use when creating the DynamoDb client. For example, we can connect to the local dynamodb dev server during development
  config.DynamoDbConfig = new AmazonDynamoDBConfig { ServiceURL = "http://localhost:8000" };
  
  // Custom session serializer class used to serialize and deserialize session data as it is being read and written from and to Dynamo DB. 
  // Default value is an instance of the Nancy.Session.EncryptedSessionSerializer which serializes and encrypts data to be stored.
  config.SessionSerializer = new CustomSessionSerializer();
  
  // Name of the cookie to store the current session id in. Default value is "__sid__"
  config.SessionIdCookieName = "_sid_";
  
  // Session timeout in minutes. Default value is 30 minutes
  config.SessionTimeOutInMinutes = 15;
  
  // Name of the DynamoDB table to store session data in. Default value is "NancySessions"
  config.TableName = "NancySessions";
  
  // Name of the primary hashkey attribute in the DynamoDB table. Default value is "SessionId"
  // Hashkey values are created by concatenating ApplicationName + "_" + GUID
  config.SessionIdAttributeName = "SessionId";
  
  // Name of the AWS SDK profile to use when authenticating with the DynamoDB service. This will only
  // be used if neither the AccessKeyId or SecretAccessKey configuration properties are provided.
  config.ProfileName = "MyAwsSDKProfile";
  
  // AWS access key to use when authenticating with DynamoDB service.
  config.AccessKeyId = "asdfakljaf;lsdfjkla;asd";
  
  // AWS secret access key to use when authenticating with DynamoDB service.
  config.SecretAccessKey = "78934hjkfwd7892345hjkfed890wer";
  
  // Number of read capacity units to specify when creating dynamodb table. Default value is 10
  config.ReadCapacityUnits = 10;
  
  // Number of write capacity units to specify when creating dynamodb table. Default value is 5
  config.WriteCapacityUnits = 5;
  
  // Allows you to provide a custom implementation of IDynamoDbTableInitializer. The table initializer is responsible
  // for setting up the DynamoDB table when the application starts up. The default implementation will create the table
  // if it does not already exist
  config.TableInitializerFactory = cfg => { return new CustomTableInitializer(cfg.TableName); };
  
  // Allows you provide custom DynamoDB client construction logic.
  config.ClientFactory = cfg => { return new CustomAmazonDynamoDbClient(cfg.DynamoDbConfig); };
  
  // Allows you to provide a custom implementation of IDynamoDbSessionRepository
  config.RepositoryFactory = cfg => { return new CustomDynamoDbSessionRepository(cfg.DynamoDbClient); };
  ```

#### A Note On AWS Credentials

While you can easily specify access key id and access key secret via the `DynamoDbBasedSessionsConfiguration` instance, it is far more preferable to use SDK profiles during development, and EC2 instance roles once your application has been deployed to AWS. See http://docs.aws.amazon.com/AWSSdkDocsNET/latest/DeveloperGuide/net-dg-config-creds.html for details.
