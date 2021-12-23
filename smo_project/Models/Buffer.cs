using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smo_project.Models
{
    class Buffer
    {
        private uint countOfRefusals = 0;
        private Request[] requests;
        private uint countOfRequests = 0;
        private uint size;
        private uint pointer = 0;

        public Buffer(uint size)
        {
            if (size == 0)
            {
                throw new ArgumentException("Buffer: tried to create buffer with 0 size!");
            }

            this.size = size;
            requests = new Request[size];
        }

        public Request popRequest()
        {
            Request temp = null;

            if (countOfRequests > 0)
            {
                countOfRequests--;
                if (pointer != 0)
                {
                    pointer--;
                }
                else
                {
                    pointer = size - 1;
                }

                temp = requests[pointer];
                requests[pointer] = null;
                return temp;
            }
            else
            {
                throw new InvalidOperationException("Buffer: tried to get request from empty buffer!");
            }
        }

        public void pushRequest(Request request)
        {
            if (request is null)
            {
                throw new ArgumentNullException("Buffer: tried to push null instead of request!");
            }

            if (countOfRequests != size)
            {
                countOfRequests++;
            }
            else
            {
                requests[pointer].CompletionTime = Managers.ModellingManager.currentTime;
                Managers.ModellingManager.refusedRequests.Add(requests[pointer]);
                countOfRefusals++;
            }

            requests[pointer] = request;

            if (pointer != size - 1)
            {
                pointer++;
            }
            else
            {
                pointer = 0;
            }
        }

        public Request getRequest(uint index)
        {
            if (index >= size)
            {
                throw new ArgumentOutOfRangeException("Buffer: index is out of range!");
            }

            return requests[index];
        }

        public uint CountOfRefusals { get => countOfRefusals; }

        public uint CountOfRequests { get => countOfRequests; }

        public uint Pointer { get => pointer; }
    }
}
