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
            if (countOfRequests > 0)
            {
                countOfRequests--;
                if (pointer != 0)
                {
                    pointer--;
                    return requests[pointer];
                }
                else
                {
                    pointer = size - 1;
                    return requests[pointer];
                }
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

            requests[pointer] = request;

            if (countOfRequests != size)
            {
                countOfRequests++;
            }
            else
            {
                countOfRefusals++;
            }

            if (pointer != size - 1)
            {
                pointer++;
            }
            else
            {
                pointer = 0;
            }
        }
    }
}
