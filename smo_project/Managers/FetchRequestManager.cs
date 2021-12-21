using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smo_project.Managers
{
    class FetchRequestManager
    {
        private uint countOfCompletedRequests = 0;
        private Models.Buffer buffer;
        private Models.Device[] devices;
        private Models.Request requestWaitingDevice = null;
        private uint pointer = 0;

        public FetchRequestManager(Models.Buffer buffer, uint countOfDevices)
        {
            if (countOfDevices == 0)
            {
                throw new ArgumentNullException("FetchRequestManager: count of devices == 0!");
            }

            if (buffer is null)
            {
                throw new ArgumentNullException("FetchRequestManager: buffer is null!");
            }

            this.buffer = buffer;
            devices = new Models.Device[countOfDevices];

            for (int i = 0; i < countOfDevices; i++)
            {
                devices[i] = new Models.Device(ModellingManager.devicesProductivity);
            }
        }

        public uint setRequestToDevice()
        {
            uint result = 0;

            if (requestWaitingDevice is null)
            {
                requestWaitingDevice = getRequestFromBuffer();
            }

            for (uint i = 0; i < devices.Length; i++)
            {
                if (devices[pointer].isAvailable())
                {
                    devices[pointer].setRequestToDevice(requestWaitingDevice);
                    result = pointer;
                    pointer = (pointer + 1) % (uint)devices.Length;
                    requestWaitingDevice = null;
                    break;
                }
                else
                {
                    pointer = (pointer + 1) % (uint)devices.Length;
                }
            }

            if (requestWaitingDevice is not null)
            {
                throw new InvalidOperationException("FetchRequestManager: tried to set request but all devices are busy!");
            }

            return result;
        }

        public void processRequestOnDevice(uint deviceID)
        {
            devices[deviceID].processRequest();
            countOfCompletedRequests++;
        }

        private Models.Request getRequestFromBuffer()
        {
            if (requestWaitingDevice is not null)
            {
                throw new InvalidOperationException("FetchRequestManager: tried to get request from buffer,"
                    + "but there is a request that has not been sent to the device.");
            }

            Models.Request request = null;
            try
            {
                request = buffer.popRequest();
            }
            catch (Exception)
            {
                throw;
            }

            requestWaitingDevice = request;
            return request;
        }

        public double getNextRequestCompletedTimeForDevice(uint deviceID)
        {
            if (deviceID >= devices.Length)
            {
                throw new ArgumentOutOfRangeException("FetchRequestManager: deviceID is out of range!");
            }

            if (devices[deviceID].isAvailable())
            {
                throw new InvalidOperationException("FetchRequestManager: there is no request on this device!");
            }

            return devices[deviceID].NextRequestCompletedTime;
        }

        public bool isAvailableDevice()
        {
            bool result = false;

            for (uint i = 0; i < devices.Length; i++)
            {
                if (devices[i].isAvailable())
                {
                    result = true;
                    break;
                }
            }
            return result;
        }

        public bool isRequestWaitingDevice()
        {
            bool result = false;
            if (requestWaitingDevice is not null)
            {
                result = true;
            }

            return result;
        }

        public double getDeviceUsage(uint deviceID)
        {
            return devices[deviceID].UsageTime / ModellingManager.currentTime;
        }

        public double getDeviceCompletedRequests(uint deviceID)
        {
            return devices[deviceID].CountOfCompletedRequests;
        }

        public double getDeviceAverageProcessingTime(uint deviceID)
        {
            return devices[deviceID].UsageTime / devices[deviceID].CountOfCompletedRequests;
        }

        public Models.Request getDeviceCurrentRequest(uint deviceID)
        {
            return devices[deviceID].RequestOnDevice;
        }

        public uint CountOfCompletedRequests { get => countOfCompletedRequests; }
    }
}
