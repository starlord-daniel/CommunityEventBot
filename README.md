# CommunityEventBot
A Bot to get information on your community event. The bot is written in C#.

[![Cortana Demo](images/Cortana_Demo.gif)](https://channel9.msdn.com/Shows/Misc-Videos/Cortana-Skill-Demo-Event-Bot)

## Initial Bot Setup ###

To get started with bot, you first have to install the Bot Builder SDK, which is available via Nuget. A [template of a sample bot](http://aka.ms/bf-bc-vstemplate) to start with is also available on the [Bot Framework documentation website](https://docs.microsoft.com/en-us/bot-framework/).

Set the bot up as explained on the [Create a bot for .NET website](https://docs.microsoft.com/en-us/bot-framework/)

## Database setup ##

To get the information about the event, any kind of storage can be used that is connectable to the C# bot environment. In this example, we use a SQL database. A sample SQL query looks like this:

```sql
SELECT [id]
      ,[speakerName]
      ,[speakerDescription]
      ,[speakerImage]
      ,[talkTitle]
      ,[talkDescription]
      ,[talkTime]
      ,[talkTrack]
  FROM [dbo].[eventinfoextended]
```

Fill the database with the information needed for the event bot. To do this, use a tool of your choice. I recommend using the [SQL Management Studio](https://docs.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms).

## LUIS setup ##

After the data is included in the database we have to figure out, what kinds of questions the user wants to ask the bot. In this simple scenario we want to get answers to the following questions:

1. What can I do next?
1. What is \<speaker> talking about?
1. When is the session from \<speaker>?

These questions will be mapped to 3 **intents**:

1. talk.next
1. talk.content
1. talk.speaker

The value of \<speaker> is stored in an **entity** called: **speakerName**

To provide the functionality to recognize these intents from users questions, the Cognitive Service LUIS is used. To learn more about LUIS, you can visit the [LUIS web portal](https://www.luis.ai/home/index).

We train each intent with 5-10 phrases that we expect the user to say, when he wants the specific information. 

### LUIS Training ###

To train and publish you LUIS model, follow the steps as described:

1. **Create a new app on [luis.ai]**(https://www.luis.ai/applications)

    ![Create App](images/1_Create_App.png)

1. **Go to intents and create a new intent**

    ![Create App](images/2_Intent.png)

1. **Add phrases to the intent**

    Add the phrases the service should understand, but don't worry, because LUIS is an intelligent service, it can adapt to changes in the query, so if a word is missing or added or the word order is changed, it will still trigger the intent as intended - or should I say: **intented** :laughing:

    **Sorry!** 

    <img src="https://media.giphy.com/media/d7fTn7iSd2ivS/giphy.gif" width=400 />

1. **Create a new entity to recognize properties**

    To add a entity, select the values that will be dynamic by clicking on them. Type a name and click "Create entity".

    ![Create App](images/3_Add_Entity.png)

1. **Training the service**

    ![Create App](images/4_Train_Test.png)

1. **Publish the service**

    ![Create App](images/5_Publish.png)


# Bot development #

## Integrating LUIS ##

For LUIS integration, you have 2 options: Either you use the built-in dialogs, as described in [Enable LUIS](https://docs.microsoft.com/en-us/bot-framework/dotnet/bot-builder-dotnet-luis-dialogs). The second option is to include the REST call manually. This offers more flexibility over the LUIS results and is used in this implementation. The implemented LuisConnector takes just a few lines of code:

```csharp
[Serializable]
public class LuisConnector
{
    public static async Task<LuisResult> GetLuisResult(string query)
    {
        LuisResult luisResponse;
        
        string luisUrl = $"https://westus.api.cognitive.microsoft.com/luis/v2.0/apps/{Credentials.LUIS_MODEL_ID}?subscription-key={Credentials.LUIS_SUBSCRIPTION_KEY}&verbose=true&q={HttpUtility.HtmlEncode(query)}";

        HttpClient client = new HttpClient();
        var response = await client.GetStringAsync(luisUrl);

        luisResponse = JsonConvert.DeserializeObject<LuisResult>(response);

        return luisResponse;
    }
}
```

The LuisResult mentioned above, looks like this:

```csharp
[Serializable]
public class LuisResult
{
    public string query { get; set; }
    public Topscoringintent topScoringIntent { get; set; }
    public Intent[] intents { get; set; }
    public Entity[] entities { get; set; }
}

[Serializable]
public class Topscoringintent
{
    public string intent { get; set; }
    public float score { get; set; }
}

[Serializable]
public class Intent
{
    public string intent { get; set; }
    public float score { get; set; }
}

[Serializable]
public class Entity
{
    public string entity { get; set; }
    public string type { get; set; }
    public int startIndex { get; set; }
    public int endIndex { get; set; }
    public float score { get; set; }
}
```

## Integrating the database ##

For database integration, the namespace **System.Data.SqlClient** is used, which provides the SQLConnection object to automate connection to the database and SqlCommand to send queries to the database.

The resulting SQLConnector which is used in the implementation, looks like this: 

```csharp
[Serializable]
public static class SqlConnector
{
    /// <summary>
    /// Call the database to get all dishes 
    /// </summary>
    /// <param name="project">All available dishes.</param>
    internal static List<EventSpeaker> GetEventSpeakerInfo()
    {
        List<EventSpeaker> speakerInfo = new List<EventSpeaker>();
        using (SqlConnection connection = new SqlConnection(Credentials.SQL_CONNECTION_STRING))
        {
            var query = String.Format("SELECT * FROM eventinfoextended;");

            SqlCommand command = new SqlCommand(query, connection);
            connection.Open();

            using (SqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var id = Convert.ToDecimal(reader["ID"]);
                    var speakerName = reader["SPEAKERNAME"].ToString();
                    var speakerDescription = reader["SPEAKERDESCRIPTION"].ToString();
                    var speakerImage = reader["SPEAKERIMAGE"].ToString();
                    var talkTitle = reader["TALKTITLE"].ToString();
                    var talkDescription = reader["TALKDESCRIPTION"].ToString();
                    var talkTime = Convert.ToDateTime(reader["TALKTIME"]);
                    var talkTrack = reader["TALKTRACK"].ToString();


                    speakerInfo.Add(new EventSpeaker
                    {
                        Id = (int)id,
                        SpeakerDescription = speakerDescription,
                        SpeakerImageUrl = speakerImage,
                        SpeakerName = speakerName,
                        TalkDescription = talkDescription,
                        TalkTime = talkTime,
                        TalkTitle = talkTitle,
                        TalkTrack = talkTrack
                    });
                }

            }
            connection.Close();
        }

        return speakerInfo;
    }

}
```

## Adding speech capabilities ##

![Cortana Image](http://www.technodoze.com/wp-content/uploads/2015/11/microsoft-cortana.jpg)

For Cortana integration, take a look at the following links: 

1. [Test Cortana Skills](https://docs.microsoft.com/en-us/bot-framework/debug-bots-cortana-skill-invoke#test-your-cortana-skill)

2. [Cortana Developer Center](https://developer.microsoft.com/en-us/cortana)

3. [Connect a bot to Cortana](https://docs.microsoft.com/en-us/bot-framework/channel-connect-cortana)

4. [Teach your bot to speak](https://docs.microsoft.com/en-us/cortana/tutorials/bot-skills/teach-your-bot-to-speak)
