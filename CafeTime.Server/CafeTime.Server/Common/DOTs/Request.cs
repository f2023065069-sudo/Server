using System;
using System.Collections.Generic;
using System.Text;

namespace CafeTime.Server.Common.DOTs
{
    public class Request
    {
        public string Action { get; set; }
        public object Data { get; set; }
    }
}
