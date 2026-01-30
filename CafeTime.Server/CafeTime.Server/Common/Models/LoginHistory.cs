using System;
using System.Collections.Generic;
using System.Text;

namespace CafeTime.Server.Common.Models
{
    public class LoginHistory
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public DateTime LoginTime { get; set; }
    }

}
