using System;
using System.Collections.Generic;

namespace smo_project.Managers
{
    class FetchRequestManager
    {
        private Models.Buffer buffer;
        private List<Models.Device> devices;
        private uint pointer = 0;

        public FetchRequestManager(Models.Buffer buffer)
        {
            if (buffer is null)
            {
                throw new ArgumentNullException("FetchRequestManager: buffer is null!");
            }

            this.buffer = buffer;
            devices = new List<Models.Device>();
        }

        public void addDevice(uint productivity)
        {
            devices.Add(new Models.Device(productivity));
        }

        public uint setRequestToDevice()
        {
            uint result = 0;
            Models.Request temp = null;

            for (uint i = 0; i < devices.Count; i++)
            {
                if (devices[(int)pointer].isAvailable())
                {
                    temp = getRequestFromBuffer();
                    devices[(int)pointer].setRequestToDevice(temp);
                    result = pointer;
                    pointer = (pointer + 1) % (uint)devices.Count;
                    break;
                }
                else
                {
                    pointer = (pointer + 1) % (uint)devices.Count;
                }
            }

            if (temp is null)
            {
                throw new InvalidOperationException("FetchRequestManager: tried to set request but all devices are busy!");
            }

            return result;
        }

        public void processRequestOnDevice(uint deviceID)
        {
            if (deviceID >= devices.Count)
            {
                throw new ArgumentOutOfRangeException("FetchRequestManager: out of range, processRequestOnDevice method!");
            }

            if (devices[(int)deviceID].isAvailable())
            {
                throw new ArgumentException("FetchRequestManager: device is available, but tried to process request!");
            }

            devices[(int)deviceID].processRequest();
        }

        private Models.Request getRequestFromBuffer()
        {
            Models.Request request = null;
            try
            {
                request = buffer.popRequest();
            }
            catch (Exception)
            {
                throw;
            }

            return request;
        }

        public bool isAvailableDevice()
        {
            bool result = false;

            for (uint i = 0; i < devices.Count; i++)
            {
                if (devices[(int)i].isAvailable())
                {
                    result = true;
                    break;
                }
            }
            return result;
        }

        public double getNextRequestCompletedTimeForDevice(uint deviceID)
        {
            if (deviceID >= devices.Count)
            {
                throw new ArgumentOutOfRangeException("FetchRequestManager: deviceID is out of range!");
            }

            if (devices[(int)deviceID].isAvailable())
            {
                throw new InvalidOperationException("FetchRequestManager: there is no request on this device!");
            }

            return devices[(int)deviceID].NextRequestCompletedTime;
        }

        public double getDeviceUsage(uint deviceID)
        {
            return devices[(int)deviceID].UsageTime / ModellingManager.currentTime;
        }

        public double getDeviceCompletedRequests(uint deviceID)
        {
            return devices[(int)deviceID].CountOfCompletedRequests;
        }

        public double getDeviceAverageProcessingTime(uint deviceID)
        {
            return devices[(int)deviceID].UsageTime / devices[(int)deviceID].CountOfCompletedRequests;
        }

        public Models.Request getDeviceCurrentRequest(uint deviceID)
        {
            return devices[(int)deviceID].RequestOnDevice;
        }

        public uint Pointer { get => pointer; }
    }
}
