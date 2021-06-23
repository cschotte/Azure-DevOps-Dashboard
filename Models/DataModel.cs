using System;
using System.Collections.Generic;

namespace Dashboard.Models
{
    public class DataModel
    {
        public DataModel()
        {
            Owners = new List<User>();
        }

        public Guid ProjectId { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public Uri Url { get; set; }

        public List<User> Owners { get; set; }

        public string ProcessTemplate { get; set; }

        public DateTime LastProjectUpdateTime { get; set; }

        public DateTime LastCommitDate { get; set; }

        public DateTime LastWorkItemDate { get; set; }

        public DateTime LastKnownActivity
        {
            get
            {
                var date = LastProjectUpdateTime;
                if (date < LastCommitDate) date = LastCommitDate;
                if (date < LastWorkItemDate) date = LastWorkItemDate;

                return date;
            }
        }

        public double ProjectAge
        {
            get
            {
                return (DateTime.Now - LastKnownActivity).TotalDays;
            }
        }
    }

    public class User
    {
        public string DisplayName { get; set; }

        public string MailAddress { get; set; }
    }
}
