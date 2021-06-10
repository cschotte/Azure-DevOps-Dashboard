using System;

namespace WebJob.Models
{
    class DataModel
    {
        public string ProjectId { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string Url { get; set; }

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
    }
}
