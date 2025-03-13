using System;
using Newtonsoft.Json.Linq;

namespace ConcurSyncLib
{
    public class AuthToken
    {
        public string expires_in;
        public string scope;
        public string token_type;
        public string refresh_token;
        public string access_token;
        public string id_token;
        public DateTime expire_timestamp;

        public AuthToken(JObject jsonResponse)
        {
            expires_in = jsonResponse["expires_in"].ToString();
            expire_timestamp = DateTime.Now.AddSeconds(Convert.ToInt32(expires_in) - 60);
            scope = jsonResponse["scope"].ToString();
            token_type = jsonResponse["token_type"].ToString();
            access_token = jsonResponse["access_token"].ToString();
            refresh_token = jsonResponse["refresh_token"].ToString();
            id_token = jsonResponse["id_token"].ToString();
        }
    }
}
