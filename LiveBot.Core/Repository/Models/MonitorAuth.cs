﻿using LiveBot.Core.Repository.Static;

namespace LiveBot.Core.Repository.Models
{
    public class MonitorAuth : BaseModel<MonitorAuth>
    {
        public ServiceEnum ServiceType { get; set; }
        public bool Expired { get; set; }
        public string ClientId { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }
}