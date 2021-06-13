using System;
using System.Collections.Generic;

namespace WebJob.Models
{
    class DataModel
    {
        public DataModel()
        {
            Owners = new List<UserModel>();
        }

        public Guid ProjectId { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public Uri Url { get; set; }

        public List<UserModel> Owners { get; set; }

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
}
