using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQRpc.Artemis.Exceptions
{
    public class InternalServerException : Exception
    {
        public InternalServerException(string message) : base(message) { }
    }
}
