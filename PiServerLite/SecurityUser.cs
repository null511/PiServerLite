using PiServerLite.Http;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PiServerLite
{
    class SecurityUser : ISecurityUser
    {
        public string Username {get; set;}
        public string Password {get; set;}
        public List<string> Roles {get; set;}


        public SecurityUser()
        {
            Roles = new List<string>();
        }

        public bool HasRole(string role)
        {
            return Roles.Any(x => string.Equals(x, role, StringComparison.OrdinalIgnoreCase));
        }
    }
}
