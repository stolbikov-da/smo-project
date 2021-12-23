using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smo_project.Managers
{
    class ModellingManager
    {
        //Modelling parametres
        public static double processingTimeLambda = 0.5;
        public static uint devicesProductivity = 1;
        public static double maxRequestCreationTime = 2;
        public static double minRequestCreationTime = 1;
        public static uint countOfSources = 5;
        public static uint countOfDevices = 2;
        public static uint bufferSize = 3;
        public static uint countOfRequestsForModulation = 100000;

        public enum ModellingMode
        {
            auto = 0,
            step = 1
        }

        public static ModellingMode mode = ModellingMode.auto;

        public static Random rand = new Random();
        public static double currentTime = 0.0;
        public static bool exitFlag = false;
        public static List<Models.Request> refusedRequests = new List<Models.Request>();
        public static List<Models.Request> completedRequests = new List<Models.Request>();
        private static double[] closestSourcesEvents;
        private static double[] closestDevicesEvents;
        private static Models.Buffer buffer = new Models.Buffer(bufferSize);
        private static SetRequestManager srManager = new SetRequestManager(buffer, countOfSources, minRequestCreationTime, maxRequestCreationTime);
        private static FetchRequestManager frManager = new FetchRequestManager(buffer, countOfDevices);

        //Statistics
        public static uint[] countOfRefusalsBySource = new uint[countOfSources];
        public static uint[] countOfCompletedRequestsBySource = new uint[countOfSources];
        public static double[] averageRequestInSystemTime = new double[countOfSources];
        public static double[] averageRequestWaitingTime = new double[countOfSources];
        public static double[] averageRequestProcessingTime = new double[countOfSources];
        public static double[] waitingDispersion = new double[countOfSources];
        public static double[] processingDispersion = new double[countOfSources];

        public static void startModelling()
        {
            chooseModellingMode(ref mode);
            printModellingParameters();
            Console.WriteLine();

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

            double nextTime;

            while (completedRequests.Count + refusedRequests.Count < countOfRequestsForModulation)
            {
                nextTime = getNextClosestEventTime();

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
            }

            updateStatistics();

            if (mode == ModellingMode.auto)
            {
                double devicesUsage = 0;
                for (uint i = 0; i < countOfDevices; i++)
                {
                    devicesUsage += frManager.getDeviceUsage(i);
                }
                printAdditionalInfo((uint) completedRequests.Count, (uint) refusedRequests.Count, devicesUsage / countOfDevices, currentTime);
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
            countRefusals();
            countCompletedRequests();
            countAverageInSystemTime();
            countAverageWaitingTime();
            countAverageProcessingTime();
            countWaitingDispersion();
            countProcessingDispersion();
        }

        private static void printResults()
        {
            printDevicesResults();
            printSourcesResults();
        }

        private static void printDevicesResults()
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

        private static void printSourcesResults()
        {
            uint fieldGeneratedRequests = 11;
            uint fieldAverage = 15;
            uint fieldNumberLength = (uint)(countOfSources - 1).ToString().Length + 2;
            uint fieldRefusalProbability = 11;
            uint fieldInSystemTime = 13;
            uint fieldWaitingTime = 8;
            uint fieldProcessingTime = 8;
            uint fieldDispersionProcessing = 8;
            uint fieldDispersionWaiting = 8;
            uint fieldsCount = 9;

            uint sum = fieldGeneratedRequests + fieldNumberLength + fieldsCount + fieldAverage + fieldRefusalProbability
                + fieldInSystemTime + fieldWaitingTime + fieldProcessingTime + fieldDispersionProcessing + fieldDispersionWaiting;

            Console.WriteLine(" Sources ");
            printSeparator(sum, '-');
            printField(fieldNumberLength, "N");
            Console.Write('|');
            printField(fieldGeneratedRequests, "Gen. Req.");
            Console.Write('|');
            printField(fieldAverage, "Av. Gen. Time");
            Console.Write('|');
            printField(fieldRefusalProbability, "P refusal");
            Console.Write('|');
            printField(fieldInSystemTime, "T in system");
            Console.Write('|');
            printField(fieldWaitingTime, "T wait");
            Console.Write('|');
            printField(fieldProcessingTime, "T proc");
            Console.Write('|');
            printField(fieldDispersionProcessing, "D proc");
            Console.Write('|');
            printField(fieldDispersionWaiting, "D wait");
            Console.Write("|\n");
            printSeparator(sum, '-');

            for (uint i = 0; i < countOfSources; i++)
            {
                Console.Write(" " + i + " |");
                printField(fieldGeneratedRequests, srManager.getSourceCountGeneratedRequests(i).ToString());
                Console.Write('|');
                printField(fieldAverage, (currentTime / srManager.getSourceCountGeneratedRequests(i)).ToString());
                Console.Write('|');
                printField(fieldRefusalProbability, ((double)countOfRefusalsBySource[i] / srManager.getSourceCountGeneratedRequests(i)).ToString());
                Console.Write('|');
                printField(fieldInSystemTime, averageRequestInSystemTime[i].ToString());
                Console.Write('|');
                printField(fieldWaitingTime, averageRequestWaitingTime[i].ToString());
                Console.Write('|');
                printField(fieldProcessingTime, averageRequestProcessingTime[i].ToString());
                Console.Write('|');
                printField(fieldDispersionProcessing, processingDispersion[i].ToString());
                Console.Write('|');
                printField(fieldDispersionWaiting, waitingDispersion[i].ToString());
                Console.Write("|\n");
            }
            printSeparator(sum, '-');
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

        private static void printSourcesState()
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

        private static void printDevicesState()
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
                if (i == frManager.Pointer)
                {
                    Console.Write("| * \n");
                }
                else
                {
                    Console.Write("|\n");
                }
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
                else if (temp == "end")
                {
                    mode = ModellingMode.auto;
                    return;
                }
                printSourcesState();
                printBufferState();
                printDevicesState();
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

        private static void countRefusals()
        {
            Models.Request temp;
            uint sourceID = 0;
            uint stackSize = (uint)refusedRequests.Count;
            for (uint i = 0; i < stackSize; i++)
            {
                temp = refusedRequests[(int)i];
                sourceID = temp.SourceID;
                countOfRefusalsBySource[sourceID]++;
            }
        }

        private static void countCompletedRequests()
        {
            Models.Request temp;
            uint sourceID = 0;
            uint[] countOfRequestsBySource = new uint[countOfSources];
            uint stackSize = (uint)completedRequests.Count;

            for (uint i = 0; i < stackSize; i++)
            {
                temp = completedRequests[(int)i];
                sourceID = temp.SourceID;
                countOfCompletedRequestsBySource[sourceID]++;
            }
        }

        private static void countAverageInSystemTime()
        {
            foreach (Models.Request req in completedRequests)
            {
                averageRequestInSystemTime[req.SourceID] += req.CompletionTime - req.CreationTime;
            }

            //foreach (Models.Request req in refusedRequests)
            //{
            //    averageRequestInSystemTime[req.SourceID] += req.CompletionTime - req.CreationTime;
            //}

            for (uint i = 0; i < countOfSources; i++)
            {
                averageRequestInSystemTime[i] = averageRequestInSystemTime[i] / countOfCompletedRequestsBySource[i]; //(countOfRefusalsBySource[i] + countOfCompletedRequestsBySource[i]);
            }
        }

        private static void countAverageWaitingTime()
        {
            foreach (Models.Request req in completedRequests)
            {
                averageRequestWaitingTime[req.SourceID] += (req.CompletionTime - req.CreationTime) - req.ProcessingTime;
            }

            //foreach (Models.Request req in refusedRequests)
            //{
            //    averageRequestWaitingTime[req.SourceID] += req.CompletionTime - req.CreationTime;
            //}

            for (uint i = 0; i < countOfSources; i++)
            {
                averageRequestWaitingTime[i] = averageRequestWaitingTime[i] / countOfCompletedRequestsBySource[i];//(countOfRefusalsBySource[i] + countOfCompletedRequestsBySource[i]);
            }
        }

        private static void countAverageProcessingTime()
        {
            foreach (Models.Request req in completedRequests)
            {
                averageRequestProcessingTime[req.SourceID] += req.ProcessingTime;
            }

            for (uint i = 0; i < countOfSources; i++)
            {
                averageRequestProcessingTime[i] = averageRequestProcessingTime[i] / countOfCompletedRequestsBySource[i];
            }
        }

        private static void countWaitingDispersion()
        {
            foreach (Models.Request req in completedRequests)
            {
                waitingDispersion[req.SourceID] += Math.Pow((averageRequestWaitingTime[req.SourceID]
                    - (req.CompletionTime - req.CreationTime - req.ProcessingTime)), 2);
            }

            foreach (Models.Request req in refusedRequests)
            {
                waitingDispersion[req.SourceID] += Math.Pow((averageRequestWaitingTime[req.SourceID]
                    - (req.CompletionTime - req.CreationTime - req.ProcessingTime)), 2);
            }

            for (uint i = 0; i < countOfSources; i++)
            {
                waitingDispersion[i] = waitingDispersion[i] / (countOfRefusalsBySource[i] + countOfCompletedRequestsBySource[i]);
            }
        }

        private static void countProcessingDispersion()
        {
            foreach (Models.Request req in completedRequests)
            {
                processingDispersion[req.SourceID] += Math.Pow((averageRequestProcessingTime[req.SourceID] - req.ProcessingTime), 2);
            }

            for (uint i = 0; i < countOfSources; i++)
            {
                processingDispersion[i] = processingDispersion[i] / countOfCompletedRequestsBySource[i];
            }
        }
    }
}
