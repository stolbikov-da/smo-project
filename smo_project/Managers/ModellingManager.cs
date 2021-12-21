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

        private static Models.Request[] requests = new Models.Request[countOfSources];
        private static Models.Buffer buffer = new Models.Buffer(bufferSize);

        public ModellingManager()
        {

        }

        public static void startModelling()
        {
            printModelingParameters();
            Console.WriteLine();

            uint countOfCompletedRequests = 0;

            SetRequestManager srManager = new SetRequestManager(buffer, countOfSources, minRequestCreationTime, maxRequestCreationTime);
            FetchRequestManager frManager = new FetchRequestManager(buffer, countOfDevices);

            closestSourcesEvents = new double[countOfSources];
            closestDevicesEvents = new double[countOfDevices];

            for (uint i = 0; i < countOfSources; i++)
            {
                requests[i] = srManager.getNewRequest();
                closestSourcesEvents[i] = srManager.getNextRequestReadyTimeForSource(i);
            }

            printSourcesState(srManager);
            printDevicesState(frManager);
            printBufferState();

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

        private static void printModelingParameters()
        {
            Console.WriteLine("-----------------------------------------");
            Console.WriteLine("Mode: " + getModeString());
            Console.WriteLine("Devices: " + countOfDevices);
            Console.WriteLine("Sources: " + countOfSources);
            Console.WriteLine("Buffer size: " + bufferSize);
            Console.WriteLine("-----------------------------------------");
        }

        private static string getModeString()
        {
            string result = "";
            if (mode == ModellingMode.auto)
            {
                result = "auto";
            }
            else if (mode == ModellingMode.step)
            {
                result = "step";
            }
            return result;
        }

        private static void printSourcesState(SetRequestManager srManager)
        {
            uint fieldRequestIdLength = 12;
            uint fieldGenTimeLength = 17;
            uint fieldNumberLength = (uint)(countOfSources - 1).ToString().Length + 2;

            uint fieldsCount = 3;

            Console.WriteLine(" Sources ");
            printSeparator(fieldRequestIdLength + fieldGenTimeLength + fieldNumberLength + fieldsCount, '-');
            printField(fieldNumberLength, "N");
            Console.Write('|');
            printField(fieldRequestIdLength, "Request ID");
            Console.Write('|');
            printField(fieldGenTimeLength, "Generation Time");
            Console.Write("|\n");
            printSeparator(fieldRequestIdLength + fieldGenTimeLength + fieldNumberLength + fieldsCount, '-');

            for (uint i = 0; i < countOfSources; i++)
            {
                Console.Write(" " + i + " |");
                printField(fieldRequestIdLength, srManager.getSourceCurrentRequest(i).Id.ToString());
                Console.Write('|');
                printField(fieldGenTimeLength, srManager.getSourceCurrentRequest(i).CreationTime.ToString());
                Console.Write("|\n");
            }
            printSeparator(fieldRequestIdLength + fieldGenTimeLength + fieldNumberLength + fieldsCount, '-');
        }

        private static void printDevicesState(FetchRequestManager frManager)
        {
            uint fieldRequestIdLength = 12;
            uint fieldCompletionTimeLength = 17;
            uint fieldNumberLength = (uint)(countOfDevices - 1).ToString().Length + 2;

            uint fieldsCount = 3;

            Models.Request tempRequest = null;

            Console.WriteLine(" Devices ");
            printSeparator(fieldRequestIdLength + fieldCompletionTimeLength + fieldNumberLength + fieldsCount, '-');
            printField(fieldNumberLength, "N");
            Console.Write('|');
            printField(fieldRequestIdLength, "Request ID");
            Console.Write('|');
            printField(fieldCompletionTimeLength, "Completion Time");
            Console.Write("|\n");
            printSeparator(fieldRequestIdLength + fieldCompletionTimeLength + fieldNumberLength + fieldsCount, '-');

            for (uint i = 0; i < countOfDevices; i++)
            {
                Console.Write(" " + i + " |");
                tempRequest = frManager.getDeviceCurrentRequest(i);
                if (tempRequest != null)
                {
                    printField(fieldRequestIdLength, tempRequest.Id.ToString());
                    Console.Write('|');
                    printField(fieldCompletionTimeLength, tempRequest.CreationTime.ToString());
                }
                else
                {
                    printField(fieldRequestIdLength, "empty");
                    Console.Write('|');
                    printField(fieldCompletionTimeLength, "-");
                }
                Console.Write("|\n");
            }
            printSeparator(fieldRequestIdLength + fieldCompletionTimeLength + fieldNumberLength + fieldsCount, '-');
        }

        private static void printBufferState()
        {
            uint fieldRequestIdLength = 12;
            uint fieldNumberLength = (uint)(bufferSize - 1).ToString().Length + 2;

            uint fieldsCount = 2;

            Console.WriteLine(" Buffer ");
            printSeparator(fieldRequestIdLength + fieldNumberLength + fieldsCount, '-');
            printField(fieldNumberLength, "N");
            Console.Write('|');
            printField(fieldRequestIdLength, "Request ID");
            Console.Write("|\n");
            printSeparator(fieldRequestIdLength + fieldNumberLength + fieldsCount, '-');

            for (uint i = 0; i < bufferSize; i++)
            {
                Models.Request temp = buffer.getRequest(i);

                printField(fieldNumberLength, i.ToString());
                Console.Write('|');

                if (temp == null)
                {
                    printField(fieldRequestIdLength, "empty");
                }
                else
                {
                    printField(fieldRequestIdLength, temp.Id.ToString());
                }

                Console.Write("|\n");
            }
            printSeparator(fieldRequestIdLength + fieldNumberLength + 2, '-');
        }

        private static void printAdditionalInfo()
        { }

        private static void printField(uint fieldLength, string data)
        {
            if (fieldLength < 3)
            {
                throw new ArgumentException("ModellingManager: printField gets incorrect arguements!");
            }

            if (data.Length > fieldLength - 2)
            {
                data = data.Substring(0, (int)fieldLength - 2);
            }

            int indent = (int)(fieldLength - data.Length) / 2;

            for (int j = 0; j < indent; j++)
            {
                Console.Write(' ');
            }
            
            Console.Write(data);
            for (int j = 0; j < fieldLength - data.Length - indent; j++)
            {
                Console.Write(' ');
            }
        }

        private static void printSeparator(uint length, char character)
        {
            for (uint i = 0; i < length; i++)
            {
                Console.Write(character);
            }
            Console.Write('\n');
        }
    }
}
