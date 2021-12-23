
namespace smo_project.Models
{
    class Request
    {
        private static uint countOfCreatedRequests = 0;

        private uint id = 0;
        private double creationTime = 0.0;
        private double completionTime = 0.0;
        private double processingTime = 0.0;
        private uint sourceID = 0;
        private uint deviceID = 0;
        private bool isRefused = false;

        public Request(double creationTime, uint sourceID)
        {
            this.creationTime = creationTime;
            this.sourceID = sourceID;

            id = countOfCreatedRequests++;
        }

        public void close(double completionTime, double processingTime, uint deviceID)
        {
            this.completionTime = completionTime;
            this.processingTime = processingTime;
            this.deviceID = deviceID;
        }

        public void refuse(double currentTime)
        {
            completionTime = currentTime;
            isRefused = true;
        }

        public double CreationTime { get => creationTime; }

        public double CompletionTime { get => completionTime; set => completionTime = value; }

        public uint Id { get => id; }
        public uint SourceID { get => sourceID; }
        public double ProcessingTime { get => processingTime; }
    }
}
