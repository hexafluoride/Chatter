using System;

namespace Chatter
{
    public class QuestionResponse
    {
        public LogLine Question { get; set; }
        public LogLine Response { get; set; }
        public bool Sanitized = false;

        public QuestionResponse(LogLine q, LogLine r)
        {
            Question = q;
            Response = r;
        }
    }
}

