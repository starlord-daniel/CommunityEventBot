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
            // Optional greeting
            // context.Wait(MessageReceivedAsync);

            // Lead direclty to LUIS
            context.Wait(WaitForLuisMessage);

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

            var entity = (from l in luisResult.entities where l.type == "speakerName" select l).FirstOrDefault();

            switch (luisResult.topScoringIntent.intent)
            {
                case "talk.speaker":
                    {
                        if (entity != null)
                        {
                            var speakerInfo = allEventInfos.Where(x => x.SpeakerName.ToLower().Contains(entity.entity)).FirstOrDefault();

                            // Format the speakers info for output
                            await BuildSpeakerResult(context, speakerInfo);
                        }
                        else
                        {
                            await AskAgain(context);
                        }
                    }
                    break;
                case "talk.content":
                    {
                        if (entity != null)
                        {
                            var speakerInfo = allEventInfos.Where(x => x.SpeakerName.ToLower().Contains(entity.entity)).FirstOrDefault();

                            // Filter for Speaker Content
                            var resultText = $"{entity.entity} talks about the following topic: {speakerInfo.TalkDescription}";

                            await context.PostAsync(resultText);
                            await context.PostAsync(RETURN_GREETING);
                            context.Wait(WaitForLuisMessage);
                        }
                        else
                        {
                            await AskAgain(context);
                        }
                    }
                    break;
                case "talk.next":
                    {
                        // Get current time
                        var currentTime = DateTime.Now;

                        // Filter for time
                        var allLaterSpeakers = allEventInfos.Where(x => x.TalkTime.CompareTo(currentTime) > 0).OrderBy(x => x.TalkTime).ToList();

                        if (allLaterSpeakers.Count == 0)
                        {
                            IMessageActivity message = context.MakeMessage();

                            message.Speak = "There are no sessions left. Happy coding!";
                            message.InputHint = InputHints.AcceptingInput;

                            await context.PostAsync("There are no sessions left. Happy coding :-)");
                            context.Wait(WaitForLuisMessage);
                        }
                        else
                        {
                            await DisplaySpeakerInformation(context, allLaterSpeakers);
                        }
                        
                        

                    }
                    break;
                default:
                    {
                        await AskAgain(context);
                    }
                    break;
            }
        }

        private async Task AskAgain(IDialogContext context)
        {
            var message = context.MakeMessage();
            message.Speak = "I couldn't find the speaker you are looking for. Please include the speaker in your request and try again.";
            message.Text = "I couldn't find the speaker you are looking for. Please include the speaker in your request and try again.";
            message.InputHint = InputHints.AcceptingInput;

            await context.PostAsync(message);
            context.Wait(WaitForLuisMessage);
        }

        private async Task BuildSpeakerResult(IDialogContext context, EventSpeaker speakerInfo)
        {
            if (speakerInfo == null)
            {
                await AskAgain(context);
                return;
            }

            var message = context.MakeMessage();
            message.Recipient = context.MakeMessage().From;
            message.Type = "message";
            message.Attachments = new List<Attachment>();
            message.AttachmentLayout = AttachmentLayoutTypes.Carousel;

            HeroCard resultCard = new HeroCard()
            {
                Images = new List<CardImage> { new CardImage(speakerInfo.SpeakerImageUrl) },
                Title = speakerInfo.SpeakerName,
                Text = speakerInfo.TalkDescription,
                Subtitle = String.Format("{0:t}", speakerInfo.TalkTime)
            };

            Attachment cardAttachment = resultCard.ToAttachment();
            message.Attachments.Add(cardAttachment);

            var spoken = $"The talk from {speakerInfo.SpeakerName} is at {String.Format("{0:t}", speakerInfo.TalkTime)} about {speakerInfo.TalkTitle}";
            message.Speak = spoken;

            await context.PostAsync(message);
            //await context.PostAsync(RETURN_GREETING);
            context.Wait(WaitForLuisMessage);
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
                    Text = speaker.TalkDescription.Length < 150 ? speaker.TalkDescription : speaker.TalkDescription.Substring(0, 150) + "...",
                    Subtitle = String.Format("{0:t}", speaker.TalkTime)
                };

                Attachment cardAttachment = resultCard.ToAttachment();
                finalMessage.Attachments.Add(cardAttachment);
            }

            // Build up spoken response
            var spoken = $"I received {allLaterSpeakers.Count} results.";
            finalMessage.Speak = spoken;
            finalMessage.InputHint = InputHints.AcceptingInput;

            await context.PostAsync(finalMessage);
            //await context.PostAsync(RETURN_GREETING);
            context.Wait(WaitForLuisMessage);

        }

    }
}