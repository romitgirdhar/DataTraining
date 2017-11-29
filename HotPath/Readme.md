# Hot Path #

## Overview ##

In this module, we will run through a sample stream processing job using Spark Streaming. We will be using PySpark to run the queries and using the Jupyter Notebook experience.

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

Run the following PowerShell commands (CLI cmds available too for non-Windows users) from the root directory of the repo.

```PowerShell
    Login-AzureRmAccount

    Select-AzureRmSubscription -SubscriptionName <yourSubscriptionName>

    New-AzureRmResourceGroup -Name ExampleResourceGroup -Location "East US"
    New-AzureRmResourceGroupDeployment -Name ExampleDeployment -ResourceGroupName ExampleResourceGroup `
    -TemplateFile .\arm-template\azuredeploy.json -Mode Incremental -Verbose
```

## Exercises ##

In this module, using a series of exercises, we will be executing a Spark Streaming job using PySpark and Jupyter notebook. We will read data from Azure EventHubs perform an aggregation over a 1-minute window and write data to Power BI. We will also write the raw data to Blob Storage for cold path processing and archival.

The data simulator that we will be using for this exercise contains data from a Greenhouse. This Greenhouse is made up of various rooms. Each room has different types of plants planted. The greenhouse maintains different type of conditions for each plant. These conditions include humidity, temperature and amount of light. These conditions are monitored using sensors planted alongside each plant.

We will use the Jupyter notebook to perform our exercises.

### Running the Simulator ###

Navigate to the **DataProducer** folder and execute the .exe file to start the data simulator.

### Navigating to Azure HDInsight Jupyter Notebook ###

Follow this guide to create a Jupyter Notebook for your HDInsight cluster: [https://docs.microsoft.com/en-us/azure/hdinsight/spark/apache-spark-jupyter-notebook-kernels#create-a-jupyter-notebook-on-spark-hdinsight](https://docs.microsoft.com/en-us/azure/hdinsight/spark/apache-spark-jupyter-notebook-kernels#create-a-jupyter-notebook-on-spark-hdinsight)

Use the **PySpark3** kernel. Once the notebook is created, it will automatically set the SparkContext(sc) for you, so, you don't have to set it yourself!

### Setting up the Notebook ###

1. Since we're running a streaming job in the notebook, it is important that we increase the number of cores. The rule of thumb is to use **2x of the number of partitions of your EventHubs**.

1. Let's use the *%%configure* magic to add more cores to our spark (jupyter) job. Additionally, we will also add our EventHubs-Spark jar.

```
%%configure -f
{"executorMemory": "1G", "numExecutors":8, "executorCores":1, "conf": {"spark.jars.packages": "com.microsoft.azure:azure-eventhubs-spark_2.11:2.1.6"}}
```

### Connecting to Azure Event Hubs ###

1. By using the following code, we can connect to Azure Event Hubs.

```Python
eventHubNamespace = "archivetesteh"
progressDir = "wasbs:///progressdir/"
policyName = "RootManageSharedAccessKey"
policyKey = "9DRN9B0PyHM9SqYbG9wrQwnOWDRdJTlhfZYjzDilbI0="
eventHubName = "trainingeh"

inputStream = (spark.readStream
.format("eventhubs")
.option("eventhubs.policyname", policyName)
.option("eventhubs.policykey", policyKey)
.option("eventhubs.namespace", eventHubNamespace)
.option("eventhubs.name", eventHubName)
.option("eventhubs.partition.count", "4")
.option("eventhubs.consumergroup", "sparkstr")
.option("eventhubs.progressTrackingDir", progressDir)
.load())
```

1. Next, let's define the schema of the data that we'll be working with.

```Python
from pyspark.sql.functions import *
from pyspark.sql.types import *

schema = StructType() \
        .add("PlantType", StringType()) \
        .add("Temp", DoubleType()) \
        .add("Light", DoubleType()) \
        .add("Humidity", DoubleType()) \
        .add("Room", IntegerType()) \
        .add("PlantPosition", IntegerType()) \
        .add("SensorDateTime", StringType())
```

Now we're ready to query our data!

### Run a query on your data ###

Let's run a simple query on our data. Light is an important part of a plant's wellbeing. Too little light could mean that the plant isn't receiving enough nutrition. So, we will filter the records received to figure out whether light falls below a certain threshold. If it does, ideally we need to raise an alert. However, for this exercise, let's just run in debug mode and get a count of how many low light events we've had. We can extend this by triggering an alarm every time we receive a low light event by sending an event to a Service Bus Queue.

```Python
# Filter the data by Temperature
lowlight = (inputStream
            .selectExpr("CAST(body as STRING)")
            .select(from_json(col("body"), schema).alias("data"))
            .where("data.Light < 300"))

# Defining a sink as required for every Structured Streaming job
consoleoutput = (lowlight.writeStream.queryName("LowTemperature").outputMode("append").format("memory").start())

# Counting how many low light events we've had for each PlantType
spark.sql("SELECT data.PlantType, count(data.PlantType) as CountOfPlants FROM LowTemperature GROUP BY data.PlantType").show()

# To Stop the Query
consoleoutput.stop()
```

### Write your data to Blob Storage ###

Let's write the raw *lines* RDD to Blob Storage for archival and warm/cold path processing.

```Python
# Creating Partitions for the Data
archivedata = inputStream.selectExpr("CAST(body as STRING)", "enqueuedTime").select(from_json(col("body"), schema).alias("data"))
archivedata_w_partitions = (archivedata
.select("data.PlantType", "data.Temp", "data.Light", "data.Humidity", "data.Room", "data.PlantPosition", archivedata.data.SensorDateTime.cast("timestamp").alias("SensorDateTime"))
.withColumn("year", year(col("SensorDateTime").cast("timestamp")))
.withColumn("month", month(col("SensorDateTime").cast("timestamp")))
.withColumn("day", dayofmonth(col("SensorDateTime").cast("timestamp")))
.where("year is not null"))

# Writing the data to Blob Storage as Parquet files.
archivestream = (archivedata_w_partitions.writeStream
.outputMode("append")
.option("checkpointLocation", "wasbs:///outputprogressdir/")
.format("parquet").option("path", "wasbs:///archivedata/")
.partitionBy("year", "month", "day").start())

# To stop the query
archivestream.stop()
```

### Write your data to Power BI ###

For this use case, let's filter data by LowHumidity and send that to PowerBI so that we can visualize the data overtime.

1. First, let's create a PowerBI Streaming dataset. Follow the instructions [here](https://docs.microsoft.com/en-us/power-bi/service-real-time-streaming#create-your-streaming-dataset-with-the-option-you-like-best) for details.
>*Note*: We will use the **API** type dataset here.

1. Let's add two attributes here viz. **PlantType** (Text) and **CountOfLowHumidity** (Number).

1. Once we've created the dataset, we can now send our data to PowerBI using a REST API call. Note down the URL of the PowerBI dataset.

```Python
var pbiUrl = ""

def sendDataToPowerBI(str: String):
      val body = new StringEntity(str)
      val req = new HttpPost(pbiUrl)
      req.addHeader("Content-Type","application/json")
      req.setEntity(body)
      val httpClient = HttpClientBuilder.create().build()
      val resp = httpClient.execute(req)
      if(resp.getStatusLine.getStatusCode != 200){
        print("Error sending data to PowerBI. Reson: "+ resp.getStatusLine.getReasonPhrase)
      }

windowedStream.foreachRDD { lambda rdd =>
    if(!rdd.isEmpty()){
        lines = rdd.map(eventData => new String(eventData.getBody))
        lowlight = rdd.filter(lightFilterFunc).foreach(sendDataToPowerBI)
    }
}
```