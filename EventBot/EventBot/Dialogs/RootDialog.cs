using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using EventBot.API;
using EventBot.Model;
using System.Collections.Generic;
using System.Linq;

namespace EventBot.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        private LuisResult luisResult;
        private const string RETURN_GREETING = "Do you want to ask another question? If you do, go ahead.";

        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);

            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            var greeting = "Hi, I'm Eve, your event bot. You can ask me questions about your event. Please state a question.";

            await context.PostAsync(greeting);

            context.Wait(WaitForLuisMessage);
        }

        private async Task WaitForLuisMessage(IDialogContext context, IAwaitable<object> result)
        {
            var activity = await result as Activity;

            var TextToLuis = activity.Text;

            luisResult = await LuisConnector.GetLuisResult(TextToLuis);

            await this.HandleLuisMessage(context);
        }

        private async Task HandleLuisMessage(IDialogContext context)
        {
            List<EventSpeaker> allEventInfos = SqlConnector.GetEventSpeakerInfo();

            var entity = (from l in luisResult.entities where l.type == "speaker" select l).FirstOrDefault();

            switch (luisResult.topScoringIntent.intent)
            {
                case "talk.speaker":
                    {
                        if (entity != null)
                        {
                            var speakerInfo = allEventInfos.Where(x => x.SpeakerName == entity.entity).FirstOrDefault();

                            // Filter for Session time
                            var resultText = $"The next session from {entity.entity} is at {speakerInfo.TalkTime.Hour}:{speakerInfo.TalkTime.Minute}.";

                            await context.PostAsync(resultText);
                            await context.PostAsync(RETURN_GREETING);
                            context.Wait(WaitForLuisMessage);
                        }
                        else
                        {
                            // Ask again
                            await context.PostAsync("I couldn't find the speaker you are looking for. Please include the speaker in your request and try again.");
                            context.Wait(WaitForLuisMessage);
                        }
                    }
                    break;
                case "talk.content":
                    {
                        if (entity != null)
                        {
                            var speakerInfo = allEventInfos.Where(x => x.SpeakerName == entity.entity).FirstOrDefault();

                            // Filter for Speaker Content
                            var resultText = $"{entity.entity} talks about the following topic: {speakerInfo.TalkDescription}";

                            await context.PostAsync(resultText);
                            await context.PostAsync(RETURN_GREETING);
                            context.Wait(WaitForLuisMessage);
                        }
                        else
                        {
                            // Ask again
                            await context.PostAsync("I couldn't find the speaker you are looking for. Please include the speaker in your request and try again.");
                            context.Wait(WaitForLuisMessage);
                        }
                    }
                    break;
                case "talk.next":
                    {
                        // Get current time
                        var currentTime = DateTime.Now;

                        // Filter for time
                        var allLaterSpeakers = allEventInfos.Where(x => x.TalkTime.CompareTo(currentTime) > 0).ToList();

                        if (allLaterSpeakers.Count == 0)
                        {
                            await context.PostAsync("There are no sessions left. Happy hacking :-)");
                            await this.StartAsync(context);
                        }
                        else
                        {
                            await DisplaySpeakerInformation(context, allLaterSpeakers);
                        }
                        
                        

                    }
                    break;
                default:
                    {
                        await context.PostAsync("I couldn't find the speaker you are looking for. Please include the speaker in your request and try again.");
                        context.Wait(WaitForLuisMessage);
                    }
                    break;
            }
        }

        private async Task DisplaySpeakerInformation(IDialogContext context, List<EventSpeaker> allLaterSpeakers)
        {
            var finalMessage = context.MakeMessage();
            finalMessage.Recipient = context.MakeMessage().From;
            finalMessage.Type = "message";
            finalMessage.Attachments = new List<Attachment>();
            finalMessage.AttachmentLayout = AttachmentLayoutTypes.Carousel;
            finalMessage.TextFormat = TextFormatTypes.Markdown;

            foreach (var speaker in allLaterSpeakers)
            {
                HeroCard resultCard = new HeroCard()
                {
                    Images = new List<CardImage> { new CardImage(speaker.SpeakerImageUrl) },
                    Title = speaker.SpeakerName,
                    Text = speaker.TalkDescription.Substring(0, 150),
                    Subtitle = String.Format("{0:t}", speaker.TalkTime)
            };

                Attachment cardAttachment = resultCard.ToAttachment();
                finalMessage.Attachments.Add(cardAttachment);
            }

            await context.PostAsync(finalMessage);
            await context.PostAsync(RETURN_GREETING);
            context.Wait(WaitForLuisMessage);

        }

    }
}