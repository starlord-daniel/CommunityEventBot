using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EventBot.Model
{
    [Serializable]
    public class EventSpeaker
    {
        public int Id { get; set; }

        public string SpeakerName { get; set; }

        public string SpeakerDescription { get; set; }

        public string SpeakerImageUrl { get; set; }

        public string TalkTitle { get; set; }

        public string TalkDescription { get; set; }

        public DateTime TalkTime { get; set; }  

        public string TalkTrack { get; set; }
    }
}