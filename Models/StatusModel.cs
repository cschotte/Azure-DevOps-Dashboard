using System;

namespace Dashboard.Models
{
    public class StatusModel
    {
        public StatusModel()
        {
            Date = DateTime.Now;
            Error = false;
        }

        public DateTime Date { get; set; }

        public bool Error { get; set; }

        public string Message { get; set; }
    }
}
