using EventBot.Model;
using EventBot.Security;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace EventBot.API
{
    [Serializable]
    public class LuisConnector
    {
        public static async Task<LuisResult> GetLuisResult(string query)
        {
            LuisResult luisResponse;
            
            string luisUrl = $"https://westus.api.cognitive.microsoft.com/luis/v2.0/apps/{Credentials.LUIS_MODEL_ID}?subscription-key={Credentials.LUIS_SUBSCRIPTION_KEY}&staging=true&verbose=true&q={HttpUtility.HtmlEncode(query)}";

            HttpClient client = new HttpClient();
            var response = await client.GetStringAsync(luisUrl);

            luisResponse = JsonConvert.DeserializeObject<LuisResult>(response);

            return luisResponse;
        }
    }
}