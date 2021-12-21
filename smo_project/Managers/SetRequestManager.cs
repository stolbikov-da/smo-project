using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smo_project.Managers
{
    class SetRequestManager
    {
        private Models.Buffer buffer;
        private Models.Source[] sources;
        private uint countOfGeneratedRequests = 0;

        public SetRequestManager(Models.Buffer buffer, uint countOfSources, double minRequestCreationTime, double maxRequestCreationTime)
        {
            if (countOfSources == 0)
            {
                throw new ArgumentNullException("SetRequestManager: count of sources == 0!");
            }

            if (buffer is null)
            {
                throw new ArgumentNullException("SetRequestManager: buffer is null!");
            }

            if (maxRequestCreationTime < minRequestCreationTime)
            {
                throw new ArgumentException("SetRequestManager: Max creation time less than min creation time!");
            }

            this.buffer = buffer;
            sources = new Models.Source[countOfSources];

            for (int i = 0; i < countOfSources; i++)
            {
                sources[i] = new Models.Source(minRequestCreationTime, maxRequestCreationTime);
            }
        }

        public void setRequestToBuffer(Models.Request request)
        {
            try
            {
                buffer.pushRequest(request);
            }
            catch (Exception)
            {
                throw;
            }
            
        }

        public Models.Request getRequestFromSource(uint sourceID)
        {
            Models.Request temp = sources[sourceID].CurrentRequest;
            sources[sourceID].CurrentRequest = null;
            return temp;
        }

        public Models.Request getNewRequest()
        {
            Models.Request request = null;

            foreach(Models.Source src in sources)
            {
                if (src.isAvailable())
                {
                    request = src.generateRequest();
                    break;
                }
            }

            return request;
        }

        public double getNextRequestReadyTimeForSource(uint sourceID)
        {
            if (sourceID >= sources.Length)
            {
                throw new ArgumentOutOfRangeException("SetRequestManager: sourceID is out of range!");
            }

            return sources[sourceID].NextRequestReadyTime;
        }

        public uint getSourceIdByNextReadyTime(double time)
        {
            for (uint i = 0; i < sources.Length; i++)
            {
                if (sources[i].NextRequestReadyTime == time)
                {
                    return i;
                }
            }

            throw new ArgumentOutOfRangeException("SetRequestManager: no sources with such next time = " + time);
        }

        public Models.Request getSourceCurrentRequest(uint sourceID)
        {
            return sources[sourceID].CurrentRequest;
        }

        public uint getSourceCountGeneratedRequests(uint sourceID)
        {
            return sources[sourceID].CountOfCreatedRequests;
        }
    }
}
