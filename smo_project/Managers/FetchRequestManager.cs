using System;

namespace smo_project.Managers
{
    class FetchRequestManager
    {
        private Models.Buffer buffer;
        private Models.Device[] devices;
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
            Models.Request temp = null;

            for (uint i = 0; i < devices.Length; i++)
            {
                if (devices[pointer].isAvailable())
                {
                    temp = getRequestFromBuffer();
                    devices[pointer].setRequestToDevice(temp);
                    result = pointer;
                    pointer = (pointer + 1) % (uint)devices.Length;
                    break;
                }
                else
                {
                    pointer = (pointer + 1) % (uint)devices.Length;
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
            if (deviceID >= devices.Length)
            {
                throw new ArgumentOutOfRangeException("FetchRequestManager: out of range, processRequestOnDevice method!");
            }

            if (devices[deviceID].isAvailable())
            {
                throw new ArgumentException("FetchRequestManager: device is available, but tried to process request!");
            }

            devices[deviceID].processRequest();
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

        public uint Pointer { get => pointer; }
    }
}
