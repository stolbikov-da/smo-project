using System;

namespace smo_project.Models
{
    class Device
    {
        private static uint countOfCreatedDevices = 0;

        private uint id = 0;
        private uint countOfCompletedRequests = 0;
        private double nextRequestCompletedTime = 0.0;
        private double processingTime = 0.0;
        private uint productivity = 1;
        private Request requestOnDevice = null;
        private double usageTime = 0.0;

        public Device(uint productivity)
        {
            this.productivity = productivity;
            id = countOfCreatedDevices++;
        }

        public void setRequestToDevice(Request request)
        {
            if (request is null)
            {
                throw new ArgumentNullException("Device" + id + ": request is null!");
            }

            if (isAvailable())
            {
                double A = Managers.ModellingManager.processingTimeLambda;
                double lnA = Math.Log(A);
                double B = A * (1 - Managers.ModellingManager.rand.NextDouble());
                double lnB = Math.Log(B);
                double temp = (lnA - lnB) / A / productivity;
                if (temp > 2)
                {
                    temp = 2;
                }
                processingTime = temp * Managers.ModellingManager.processingTimeMultiplier / 2 + Managers.ModellingManager.minProcessingTime;                
                nextRequestCompletedTime = Managers.ModellingManager.currentTime + processingTime;

                requestOnDevice = request;
                requestOnDevice.CompletionTime = nextRequestCompletedTime;
                usageTime += processingTime;
            }
            else
            {
                throw new InvalidOperationException("Device" + id + ": device is busy, but tried to set request on this device!");
            }
        }

        public void processRequest()
        {
            if (!isAvailable())
            {
                requestOnDevice.close(nextRequestCompletedTime, processingTime, id);
                Managers.ModellingManager.completedRequests.Add(requestOnDevice);
                requestOnDevice = null;
                countOfCompletedRequests++;
            }
            else
            {
                throw new MethodAccessException("Device" + id + ": tried to process request, but there is no request on device!");
            }
        }

        public bool isAvailable()
        {
            bool result = false;

            if (requestOnDevice is null)
            {
                result = true;
            }

            return result;
        }

        public double NextRequestCompletedTime { get => nextRequestCompletedTime; }
        public Request RequestOnDevice { get => requestOnDevice; }
        public double UsageTime { get => usageTime; }
        public uint CountOfCompletedRequests { get => countOfCompletedRequests; }
    }
}
