using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smo_project.Models
{
    class Device
    {
        private static uint countOfCreatedDevices = 0;

        private uint id = 0;
        private uint countOfCompletedRequests = 0;
        private double nextRequestCompletedTime = 0.0;
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
                throw new ArgumentNullException("Device" + id + ": buffer is null!");
            }

            if (isAvailable())
            {
                double A = Managers.ModellingManager.processingTimeLambda;
                double lnA = Math.Log(A);
                double B = A * (1 - Managers.ModellingManager.rand.NextDouble());
                double lnB = Math.Log(B);

                requestOnDevice = request;
                double processingTime = (lnA - lnB) / A / productivity;
                usageTime += processingTime;
                nextRequestCompletedTime = Managers.ModellingManager.currentTime + processingTime;
                requestOnDevice.CompletionTime = nextRequestCompletedTime;
                requestOnDevice.ProcessingTime = processingTime;
            }
        }

        public void processRequest()
        {
            if (!isAvailable())
            {
                requestOnDevice.closeRequest();
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
