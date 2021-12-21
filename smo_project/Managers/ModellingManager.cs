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
        public static double processingTimeLambda = 0.5;
        public static uint devicesProductivity = 1;
        public static double maxRequestCreationTime = 2;
        public static double minRequestCreationTime = 1;
        public static uint countOfSources = 3;
        public static uint countOfDevices = 5;
        public static uint bufferSize = 5;
        public static uint countOfRequestsForModulation = 1000;

        public static uint countOfDevicesRefusals = 0;
        public static Random rand = new Random();
        public static double currentTime = 0.0;

        public static bool exitFlag = false;

        private static double[] closestSourcesEvents;
        private static double[] closestDevicesEvents;

        private static Models.Buffer buffer = new Models.Buffer(bufferSize);

        public static void startModelling()
        {
            chooseModellingMode(ref mode);
            printModellingParameters();
            Console.WriteLine();

            uint countOfCompletedRequests = 0;
            uint countOfRefusedRequests = 0;

            SetRequestManager srManager = new SetRequestManager(buffer, countOfSources, minRequestCreationTime, maxRequestCreationTime);
            FetchRequestManager frManager = new FetchRequestManager(buffer, countOfDevices);

            closestSourcesEvents = new double[countOfSources];
            closestDevicesEvents = new double[countOfDevices];

            for (uint i = 0; i < countOfSources; i++)
            {
                srManager.getNewRequest();
                closestSourcesEvents[i] = srManager.getNextRequestReadyTimeForSource(i);

                step(srManager, frManager);
                if (exitFlag)
                {
                    return;
                }
            }

            while (countOfCompletedRequests + countOfRefusedRequests < countOfRequestsForModulation)
            {
                double nextTime = getNextClosestEventTime();

                if (closestSourcesEvents.Contains(nextTime))
                {
                    currentTime = nextTime;
                    uint sourceID = 0;
                    for (uint i = 0; i < closestSourcesEvents.Length; i++)
                    {
                        if (closestSourcesEvents[i] == nextTime)
                        {
                            sourceID = i;
                            break;
                        }
                    }

                    Models.Request temp = srManager.getRequestFromSource(sourceID);
                    srManager.setRequestToBuffer(temp);

                    step(srManager, frManager);
                    if (exitFlag)
                    {
                        return;
                    }

                    if (frManager.isAvailableDevice())
                    {
                        uint deviceID = frManager.setRequestToDevice();
                        closestDevicesEvents[deviceID] = frManager.getNextRequestCompletedTimeForDevice(deviceID);

                        step(srManager, frManager);
                        if (exitFlag)
                        {
                            return;
                        }
                    }

                    srManager.getNewRequest();
                    closestSourcesEvents[sourceID] = srManager.getNextRequestReadyTimeForSource(sourceID);

                    step(srManager, frManager);
                    if (exitFlag)
                    {
                        return;
                    }
                }
                else if (closestDevicesEvents.Contains(nextTime))
                {
                    currentTime = nextTime;
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

                    closestDevicesEvents[deviceID] = 0.0;

                    step(srManager, frManager);
                    if (exitFlag)
                    {
                        return;
                    }

                    if (frManager.isAvailableDevice() && (buffer.CountOfRequests > 0))
                    {
                        deviceID = frManager.setRequestToDevice();
                        closestDevicesEvents[deviceID] = frManager.getNextRequestCompletedTimeForDevice(deviceID);

                        step(srManager, frManager);
                        if (exitFlag)
                        {
                            return;
                        }
                    }
                }
                else
                {
                    Console.WriteLine("ERROR: Даблы не хотят нормально сравниваться. Ну или getNextClosestEventTime работает неправильно, или массивы времен ивентов сломаны.");
                }
                updateStatistics(ref countOfRefusedRequests, ref countOfCompletedRequests, frManager);
            }

            if (mode == ModellingMode.auto)
            {
                double devicesUsage = 0;
                for (uint i = 0; i < countOfDevices; i++)
                {
                    devicesUsage += frManager.getDeviceUsage(i);
                }
                printAdditionalInfo(countOfCompletedRequests, countOfRefusedRequests, devicesUsage/countOfDevices, currentTime);
                printResults(frManager, srManager);
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

        private static void updateStatistics(ref uint countOfRefusedRequests, ref uint countOfCompletedRequests, FetchRequestManager frManager)
        {
            countOfRefusedRequests = buffer.CountOfRefusals;
            countOfCompletedRequests = frManager.CountOfCompletedRequests;
        }

        private static void printResults(FetchRequestManager frManager, SetRequestManager srManager)
        {
            printDevicesResults(frManager);
            printSourcesResults(srManager);
            printBufferResults();
        }

        private static void printDevicesResults(FetchRequestManager frManager)
        {
            uint fieldProcessedRequests = 12;
            uint fieldUsage = 7;
            uint fieldAverage = 16;
            uint fieldNumberLength = (uint)(countOfDevices - 1).ToString().Length + 2;
            uint fieldsCount = 4;

            uint sum = fieldProcessedRequests + fieldUsage + fieldNumberLength + fieldsCount + fieldAverage;

            Console.WriteLine(" Devices ");
            printSeparator(sum, '-');
            printField(fieldNumberLength, "N");
            Console.Write('|');
            printField(fieldProcessedRequests, "Proc. Req.");
            Console.Write('|');
            printField(fieldUsage, "Usage");
            Console.Write('|');
            printField(fieldAverage, "Av. Proc. Time");
            Console.Write("|\n");
            printSeparator(sum, '-');

            for (uint i = 0; i < countOfDevices; i++)
            {
                Console.Write(" " + i + " |");
                printField(fieldProcessedRequests, frManager.getDeviceCompletedRequests(i).ToString());
                Console.Write('|');
                printField(fieldUsage, frManager.getDeviceUsage(i).ToString());
                Console.Write('|');
                printField(fieldAverage, frManager.getDeviceAverageProcessingTime(i).ToString());
                Console.Write("|\n");
            }
            printSeparator(sum, '-');
        }

        private static void printSourcesResults(SetRequestManager srManager)
        {
            uint fieldGeneratedRequests = 11;
            uint fieldAverage = 15;
            uint fieldNumberLength = (uint)(countOfSources - 1).ToString().Length + 2;
            uint fieldsCount = 3;

            uint sum = fieldGeneratedRequests + fieldNumberLength + fieldsCount + fieldAverage;

            Console.WriteLine(" Sources ");
            printSeparator(sum, '-');
            printField(fieldNumberLength, "N");
            Console.Write('|');
            printField(fieldGeneratedRequests, "Gen. Req.");
            Console.Write('|');
            printField(fieldAverage, "Av. Gen. Time");
            Console.Write("|\n");
            printSeparator(sum, '-');

            for (uint i = 0; i < countOfSources; i++)
            {
                Console.Write(" " + i + " |");
                printField(fieldGeneratedRequests, srManager.getSourceCountGeneratedRequests(i).ToString());
                Console.Write('|');
                printField(fieldAverage, (currentTime / srManager.getSourceCountGeneratedRequests(i)).ToString());
                Console.Write("|\n");
            }
            printSeparator(sum, '-');
        }

        private static void printBufferResults()
        {
        }

        private static void printModellingParameters()
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

            Models.Request tempRequest = null;

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
                tempRequest = srManager.getSourceCurrentRequest(i);
                if (tempRequest != null)
                {
                    Console.Write(" " + i + " |");
                    printField(fieldRequestIdLength, tempRequest.Id.ToString());
                    Console.Write('|');
                    printField(fieldGenTimeLength, tempRequest.CreationTime.ToString());
                }
                else
                {
                    Console.Write(" " + i + " |");
                    printField(fieldRequestIdLength, "empty");
                    Console.Write('|');
                    printField(fieldGenTimeLength, "-");
                }
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
                    printField(fieldCompletionTimeLength, tempRequest.CompletionTime.ToString());
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
                if (i == buffer.Pointer)
                {
                    Console.Write("| * \n");
                }
                else
                {
                    Console.Write("|\n");
                }
            }
            printSeparator(fieldRequestIdLength + fieldNumberLength + 2, '-');
        }

        private static void printAdditionalInfo(uint countOfCompletedRequests, uint countOfRefusedRequests, double commonDevicesUsage, double timeOfModulation)
        {
            Console.WriteLine("Completed Requests: " + countOfCompletedRequests);
            Console.WriteLine("Refused Requests: " + countOfRefusedRequests);
            Console.WriteLine("Devices Usage: " + commonDevicesUsage);
            Console.WriteLine("Modulation Time: " + timeOfModulation);
        }

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

        private static void step(SetRequestManager srManager, FetchRequestManager frManager)
        {
            if (mode == ModellingMode.step)
            {
                string temp = Console.ReadLine();
                if (temp == "quit" || temp == "q")
                {
                    exitFlag = true;
                    return;
                }
                printSourcesState(srManager);
                printBufferState();
                printDevicesState(frManager);
            }
        }

        private static void chooseModellingMode(ref ModellingMode mode)
        {
            Console.Write("Modelling Mode (auto/step): ");
            string answer = Console.ReadLine();
            if (answer == "auto")
            {
                mode = ModellingMode.auto;
                Console.WriteLine("Your choice is auto");
            }
            else if (answer == "step")
            {
                mode = ModellingMode.step;
                Console.WriteLine("Your choice is step");
            }
            else
            {
                mode = ModellingMode.auto;
                Console.WriteLine("Incorrect input. Default value is auto");
            }
        }
    }
}
