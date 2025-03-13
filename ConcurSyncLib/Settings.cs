using Nucs.JsonSettings;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Dynamic;
using System;

namespace ConcurSyncLib
{
    public class ConcurSyncSettings : JsonSettings
    {
        public override string FileName { get; set; } = "concur_sync_config.json";

        public string ConnStr { get; set; }

        public string UatConnStr { get; set; }

        public bool IsUat{get; set; }

        public bool LogOnly { get; set; }

        public string UatClientId { get; set; }

        public string UatClientSecret { get; set; }

        public string UatCompanyId { get; set; }

        public string UatRefreshToken { get; set; }

        public string ClientId { get; set; }

        public string ClientSecret { get; set; }

        public string CompanyId { get; set; }

        public string RefreshToken { get; set; }

        public bool SaveFiles { get; set; }

        public string LogDir { get; set; }

        public string TemplateDir { get; set; }

        public string ScheduledTime { get; set; } 



        public ConcurSyncSettings() { }
        public ConcurSyncSettings(string fileName) : base(fileName) { }

    }
}
