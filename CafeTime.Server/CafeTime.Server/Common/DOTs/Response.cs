using System;
using System.Collections.Generic;
using System.Text;

namespace CafeTime.Server.Common.DOTs
{
    public class Response
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
    }
}
