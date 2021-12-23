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
        private double processingTime = 0.0;
        private uint sourceID = 0;

        public Request(double creationTime, uint sourceID)
        {
            this.creationTime = creationTime;
            this.sourceID = sourceID;

            id = countOfCreatedRequests++;
        }

        public void closeRequest()
        {
            countOfCompletedRequests++;
        }

        public double CreationTime { get => creationTime; }

        public double CompletionTime { get => completionTime; set => completionTime = value; }

        public uint Id { get => id; }
        public uint SourceID { get => sourceID; }
        public double ProcessingTime { get => processingTime; set => processingTime = value; }
    }
}
