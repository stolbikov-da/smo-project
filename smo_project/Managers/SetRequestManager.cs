using System;
using System.Collections.Generic;

namespace smo_project.Managers
{
    class SetRequestManager
    {
        private Models.Buffer buffer;
        private List<Models.Source> sources;

        public SetRequestManager(Models.Buffer buffer)
        {
            if (buffer is null)
            {
                throw new ArgumentNullException("SetRequestManager: buffer is null!");
            }

            this.buffer = buffer;
            sources = new List<Models.Source>();
        }

        public void addSource(double minRequestCreationTime, double maxRequestCreationTime)
        {
            sources.Add(new Models.Source(minRequestCreationTime, maxRequestCreationTime));
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
            return sources[(int)sourceID].getCurrentRequest();
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
            if (sourceID >= sources.Count)
            {
                throw new ArgumentOutOfRangeException("SetRequestManager: sourceID is out of range!");
            }

            return sources[(int)sourceID].NextRequestReadyTime;
        }

        public uint getSourceIdByNextReadyTime(double time)
        {
            for (uint i = 0; i < sources.Count; i++)
            {
                if (sources[(int)i].NextRequestReadyTime == time)
                {
                    return i;
                }
            }

            throw new ArgumentOutOfRangeException("SetRequestManager: no sources with such next time = " + time);
        }

        public Models.Request getSourceCurrentRequest(uint sourceID)
        {
            return sources[(int)sourceID].CurrentRequest;
        }

        public uint getSourceCountGeneratedRequests(uint sourceID)
        {
            return sources[(int)sourceID].CountOfCreatedRequests;
        }
    }
}
