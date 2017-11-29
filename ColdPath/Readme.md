# Cold Path #

## Overview ##

In this module, we will perform trend analysis over a period of time. This period can be flexible. This will help us find any patterns in our data over a period of time.

## Prerequisites ##

1. An Azure Subscription
1. [PowerBI.com Account][1]
1. Azure PowerShell or CLI

[1]: https://app.powerbi.com/signupredirect?pbi_source=web

## ARM Template ##

If you haven't already, execute the ARM template and setup the resources in your Azure subscription

Executing the ARM template will deploy three resources for you:
1. Azure Storage Account
1. Azure Event Hubs
1. Azure HDInsight Spark Cluster

```PowerShell
    Login-AzureRmAccount

    Select-AzureRmSubscription -SubscriptionName <yourSubscriptionName>

    New-AzureRmResourceGroup -Name ExampleResourceGroup -Location "East US"
    New-AzureRmResourceGroupDeployment -Name ExampleDeployment -ResourceGroupName ExampleResourceGroup `
    -TemplateFile <YourLocation>\arm-template\azuredeploy.json -Mode Incremental -Verbose
```

## Exercises ##

In this module, using a series of exercises, we will use the data stored in Blob Storage by our streaming job and perform trend analysis per month. The dataset to be used will be the same as the previous HotPath exercise. Since, our dataset is limited, it will perform the analysis over all the data.

We will use the Jupyter notebook to perform our exercises.

### Navigating to Azure HDInsight Jupyter Notebook ###

Follow this guide to create a Jupyter Notebook for your HDInsight cluster: [https://docs.microsoft.com/en-us/azure/hdinsight/spark/apache-spark-jupyter-notebook-kernels#create-a-jupyter-notebook-on-spark-hdinsight](https://docs.microsoft.com/en-us/azure/hdinsight/spark/apache-spark-jupyter-notebook-kernels#create-a-jupyter-notebook-on-spark-hdinsight)

Use the **PySpark3** kernel. Once the notebook is created, it will automatically set the SparkContext(sc) for you, so, you don't have to set it yourself!

### Reading & Aggregating the Data ###

We will use SparkSQL to perform the operations on our data. Here, we will **calculate the min, max and avg temperature of each plant type per month per year**. This will help us understand trends to see if the weather pattern and seasons have anything to do with the temperature trends.

```Python
filePath = "wasbs:///archivedata/"

# Reading from Parquet files and setting *basePath* as an option so that it considers *year*,*month*,*day* as partitions
greenhouseData = spark.read.option("basePath",filePath).parquet(filePath)

# Running a printschema to make sure it returns our schema correctly, along with the partitions
greenhouseData.printSchema()

# Calculating Avg, min and max temperature per month per year
import pyspark.sql.functions as func

avgTempDF = (greenhouseData
             .groupBy("PlantType", "year", "month")
             .agg(func.avg("Temp").alias("AvgTemp"), func.max("Temp").alias("MaxTemp"), func.min("Temp").alias("MinTemp"))
             .where("PlantType is not null and year is not null and month is not null")
             .where((greenhouseData["year"] == func.year(func.current_timestamp())) & (greenhouseData["month"] == func.month(func.current_timestamp()))))

avgTempDF.show()
```

### Writing the data to a Staging Environment ###

Often times, when reporting on large amounts of aggregated data, it becomes important to write data to a staging database before reporting on that data. We will use **Azure SQL DB** as a staging database for our aggregated data.

1. First, we need to create a table within our Azure SQL DB. This can be done using the sqlcmd cmdline utility. More information [here](https://docs.microsoft.com/en-us/sql/relational-databases/scripting/sqlcmd-use-the-utility).

```SQL
CREATE TABLE dbo.temptrends (
[PlantType] nvarchar(100) NOT NULL,
[year] int,
[month] int,
[AvgTemp] float,
[MaxTemp] float,
[MinTemp] float);
```

1. Next, let's write our PySpark code to write our Dataframe to SQL DB.

```Python
# Build the parameters into a JDBC url to pass into the DataFrame APIs
jdbcUsername = "msadmin"
jdbcPassword = "TestTraining123!"

jdbcHostname = "<YourServerHere>.database.windows.net"
jdbcPort = "1433"
jdbcDatabase ="stagingdata"
jdbcUrl = "jdbc:sqlserver://"+jdbcHostname+":"+jdbcPort+";database="+jdbcDatabase+";encrypt=true;trustServerCertificate=false;hostNameInCertificate=*.database.windows.net;loginTimeout=30;"

sqlTableName = "temptrends"


# Writing the data to SQL DB
tempTrendsDF.write.format('jdbc').options(url=jdbcUrl driver="com.microsoft.sqlserver.jdbc.SQLServerDriver", dbtable=sqlTableName, user=jdbcUsername, password=jdbcPassword).mode('append').save()
```

1. To verify that our data has been successfully written, we can run the following SQL command on the DB:

```SQL
SELECT * from dbo.temptrends
```

### Visualize using PowerBI ###

Once the data in written to SQL DB, we can now setup a PowerBI dataset in DirectQuery mode to read from the SQL DB table.

More information on how to achieve this can be found here:
1. [https://docs.microsoft.com/en-us/power-bi/service-azure-sql-database-with-direct-connect](https://docs.microsoft.com/en-us/power-bi/service-azure-sql-database-with-direct-connect)

1. [https://docs.microsoft.com/en-us/power-bi/desktop-use-directquery](https://docs.microsoft.com/en-us/power-bi/desktop-use-directquery)

>*Note*: Power BI Desktop is currently only supported on Windows

### Other Exercises ###

Here is a list of other exercises that you may find useful.

>*Note*: Even though some of the resources below are specific to IntelliJ, it is possible (and fully supported) to develop your Spark jobs in other IDEs such as VS Code, Eclipse, etc.

1. Creating a Spark Streaming Job in IntelliJ: [https://docs.microsoft.com/en-us/azure/hdinsight/spark/apache-spark-eventhub-streaming](https://docs.microsoft.com/en-us/azure/hdinsight/spark/apache-spark-eventhub-streaming)

1. Create a Spark Job for IntelliJ: [https://docs.microsoft.com/en-us/azure/hdinsight/spark/apache-spark-intellij-tool-plugin](https://docs.microsoft.com/en-us/azure/hdinsight/spark/apache-spark-intellij-tool-plugin)

1. Use Azure Data Factory (ADF) to schedule and orchestrate your Spark Job: [https://docs.microsoft.com/en-us/azure/data-factory/tutorial-transform-data-spark-powershell](https://docs.microsoft.com/en-us/azure/data-factory/tutorial-transform-data-spark-powershell)