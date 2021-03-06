using System;

namespace smo_project.Models
{
    class Source
    {
        private static uint countOfCreatedSources = 0;

        private uint id = 0;
        private double nextRequestReadyTime = 0.0;
        private uint countOfCreatedRequests = 0;
        private double minRequestCreationTime = 0.0;
        private double maxRequestCreationTime = 0.0;
        private Request currentRequest;

        public Source(double minRequestCreationTime, double maxRequestCreationTime)
        {
            if (maxRequestCreationTime < minRequestCreationTime)
            {
                throw new ArgumentException("Source: Max creation time less than min creation time!");
            }

            this.minRequestCreationTime = minRequestCreationTime;
            this.maxRequestCreationTime = maxRequestCreationTime;

            id = countOfCreatedSources++;
        }

        public Request generateRequest()
        {
            if (isAvailable())
            {
                countOfCreatedRequests++;

                double delta = maxRequestCreationTime - minRequestCreationTime;
                nextRequestReadyTime += Managers.ModellingManager.rand.NextDouble() * delta + minRequestCreationTime;
            }
            else
            {
                throw new MethodAccessException("Source" + id + ": tried to create request, while creating another!");
            }
            currentRequest = new Request(nextRequestReadyTime, id);
            return currentRequest;
        }

        public bool isAvailable()
        {
            bool result = false;

            if (Managers.ModellingManager.currentTime >= nextRequestReadyTime)
            {
                result = true;
            }

            return result;
        }

        public Request getCurrentRequest()
        {
            Request temp = currentRequest;
            currentRequest = null;
            return temp;
        }

        public double NextRequestReadyTime { get => nextRequestReadyTime; }
        public Request CurrentRequest { get => currentRequest; }
        public uint CountOfCreatedRequests { get => countOfCreatedRequests; }
    }
}
