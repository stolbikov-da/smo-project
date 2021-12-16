using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smo_project.Managers
{
    class ModellingManager
    {
        public enum ModellingMode
        {
            auto = 0,
            step = 1
        }

        public static ModellingMode mode = ModellingMode.auto;
        public static double maxRequestProcessingTime = 1;
        public static uint devicesProductivity = 1;
        public static double maxRequestCreationTime = 5;
        public static double minRequestCreationTime = 1;
        public static uint countOfSources = 3;
        public static uint countOfDevices = 5;
        public static uint bufferSize = 5;
        public static uint countOfRequestsForModulation = 100000000;

        public static uint countOfDevicesRefusals = 0;
        public static Random rand = new Random();
        public static double currentTime = 0.0;

        private static double[] closestSourcesEvents;
        private static double[] closestDevicesEvents;
        public ModellingManager()
        {

        }

        public static void startModelling()
        {
            uint countOfCompletedRequests = 0;

            Models.Buffer buffer = new Models.Buffer(bufferSize);
            SetRequestManager srManager = new SetRequestManager(buffer, countOfSources, minRequestCreationTime, maxRequestCreationTime);
            FetchRequestManager frManager = new FetchRequestManager(buffer, countOfDevices);

            Models.Request[] requests = new Models.Request[countOfSources];

            closestSourcesEvents = new double[countOfSources];
            closestDevicesEvents = new double[countOfDevices];

            for (uint i = 0; i < countOfSources; i++)
            {
                requests[i] = srManager.getNewRequest();
                closestSourcesEvents[i] = srManager.getNextRequestReadyTimeForSource(i);
            }

            while (countOfCompletedRequests < countOfRequestsForModulation)
            {
                double nextTime = getNextClosestEventTime();

                if (closestSourcesEvents.Contains(nextTime))
                {
                    uint sourceID = 0;
                    for (uint i = 0; i < closestSourcesEvents.Length; i++)
                    {
                        if (closestSourcesEvents[i] == nextTime)
                        {
                            sourceID = i;
                            break;
                        }
                    }

                    srManager.setRequestToBuffer(requests[sourceID]);

                    try
                    {
                        uint deviceID = frManager.setRequestToDevice();
                        closestDevicesEvents[deviceID] = frManager.getNextRequestCompletedTimeForDevice(deviceID);
                    }
                    catch (Exception)
                    {
                        countOfDevicesRefusals++;
                    }

                    currentTime = nextTime;

                    requests[sourceID] = srManager.getNewRequest();
                    closestSourcesEvents[sourceID] = srManager.getNextRequestReadyTimeForSource(sourceID);

                }
                else if (closestDevicesEvents.Contains(nextTime))
                {
                    uint deviceID = 0;
                    for (uint i = 0; i < closestDevicesEvents.Length; i++)
                    {
                        if (closestDevicesEvents[i] == nextTime)
                        {
                            deviceID = i;
                            break;
                        }
                    }
                    frManager.processRequestOnDevice(deviceID);
                    countOfCompletedRequests++;

                    closestDevicesEvents[deviceID] = 0.0;

                    currentTime = nextTime;
                }
                else
                {
                    Console.WriteLine("ERROR: Даблы не хотят нормально сравниваться. Ну или getNextClosestEventTime работает неправильно, или массивы времен ивентов сломаны.");
                }
            }

            updateStatistics();
            if (mode == ModellingMode.auto)
            {
                printResults();
            }
        }

        private static double getNextClosestEventTime()
        {
            double result = 0.0;
            foreach (double time in closestDevicesEvents)
            {
                if (result == 0.0 || (time != 0.0 && result > time))
                {
                    result = time;
                }
            }

            foreach (double time in closestSourcesEvents)
            {
                if (result == 0.0 || (time != 0.0 && result > time))
                {
                    result = time;
                }
            }

            return result;
        }

        private static void updateStatistics()
        {

        }

        private static void printResults()
        {

        }
    }
}
