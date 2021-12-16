using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smo_project.Models
{
    class Request
    {
        private static uint countOfCreatedRequests = 0;
        private static uint countOfCompletedRequests = 0;

        private uint id = 0;
        private double creationTime = 0.0;
        private double completionTime = 0.0;

        public Request(double creationTime)
        {
            this.creationTime = creationTime;

            id = countOfCreatedRequests++;
        }

        public void closeRequest(double completionTime)
        {
            this.completionTime = completionTime;
            countOfCompletedRequests++;
        }

        public double CreationTime { get => creationTime; }

        public double CompletionTime { get => completionTime; }
    }
}
