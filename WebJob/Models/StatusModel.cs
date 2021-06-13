using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebJob.Models
{
    class StatusModel
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
