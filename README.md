# Consignment Processing - Windows Service

Made for a client and is a paid task. This Windows service, built on C# using the .NET framework, is designed to process consignment details from a database and interact with a customs API to create consignments. It offers a convenient way to automate the consignment creation process, reducing manual effort and ensuring efficient operations.

## Configuration

To use the service, make sure to configure the following app settings in the `app.config` or `web.config` file:

```xml
<appSettings>
    <add key="StoragePath" value="E:\test" />
    <add key="BackupPath" value="E:\test" />
    <add key="Interval" value="1" />
    <add key="Username" value="WSM" />
    <add key="clientIdentCode" value="WORLDTECH" />
    <add key="clientSystemId" value="EXCEL" />
    <add key="url" value="https://worldtech.dc.aeb.com/worldtechprod1ici/servlet/bf/InternationalCustomsBF" />
    <add key="password" value="crVJ#Ud&amp;oSFde5#nu((6" />
    <add key="FinishStatus" value="FIN" />
    <add key="logPath" value="E:\test\logs" />
    <add key="dbConnectionString" value="Data Source=10.105.9.82;Initial Catalog=Translima;User ID=sa;Password=Password11;" />
    <add key="ResourceFolderPath" value="C:\Users\muneeb.urrehman\Desktop\CreateConsignment\CreateConsignment" />
    <add key="AttachmentsFolderPath" value="E:\test\Attachments" />
</appSettings>
```

Refer to the comments within the configuration file to understand the purpose of each setting and provide appropriate values.

## Functionality
The service connects to the configured database and retrieves consignment details from the respective table.
For each consignment record, the service initiates an API call to the customs system's create consignment API using the provided credentials.
The API response includes an XML file containing the consignment information.
The service updates the appropriate database tables with the received consignment details.
Completed consignments are moved to the configured backup path for reconciliation purposes.
The service operates based on the specified interval, periodically checking for new consignments and processing them accordingly.
## Usage
To use this Windows service:

Configure the necessary app settings in the configuration file, ensuring accurate values for database connection, API details, file paths, and other parameters.
Build the solution and deploy the service to the target environment.
Start the service using the appropriate method (e.g., Windows Services Manager).
The service will automatically process consignments based on the specified interval, calling the customs API and updating the database accordingly.
Monitor the service logs located at the defined logPath for any relevant information or errors.
## Contribution
Contributions to this project are welcome. If you encounter any issues, have suggestions, or would like to add new features, please open an issue or submit a pull request on the project's GitHub repository.

## Acknowledgements
  This project was developed by <b><u>Muneeb Ur Rehman</u></b>. Special thanks to the open-source community for their contributions and support.

  For any questions or inquiries, please contact muneeb110@live.com.
