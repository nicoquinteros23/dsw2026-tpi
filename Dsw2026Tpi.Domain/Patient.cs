using System;

namespace Dsw2026Tpi.Domain
{
    public class Patient : EntintyBase
    {
        public string Dni { get; set;}
        public string  FullName { get; set;}
        public string Email {get; set;}
        public string Telefono {get; set;}
        public Guid ApplicationUserId {get; set;}
        public bool  Deleted {get; set;}
    }


}
