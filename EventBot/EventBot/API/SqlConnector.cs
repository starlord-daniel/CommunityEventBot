using EventBot.Model;
using EventBot.Security;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace EventBot.API
{
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
}