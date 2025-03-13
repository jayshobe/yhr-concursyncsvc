using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Nucs.JsonSettings;
using System.Data;
using System.Text.RegularExpressions;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Diagnostics;
using System.Security.Policy;

namespace ConcurSyncLib
{
    public class ConcurData
    {
        ConcurSyncSettings settings;
        DataStore ds;

        private const string expenseUserSchema = "urn:ietf:params:scim:schemas:extension:spend:2.0:User";
        private const string travelUserSchema = "urn:ietf:params:scim:schemas:extension:travel:2.0:User";
        private const string userSchema = "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User";
        private const string expenseApproverSchema = "urn:ietf:params:scim:schemas:extension:spend:2.0:Approver";
        private const string roleSchema = "urn:ietf:params:scim:schemas:extension:spend:2.0:Role";
        public static string countryIn = " ('US', 'Canada', 'Netherlands', 'Germany', 'UK', 'Australia', 'Singapore', 'UAE', 'Hong Kong', 'France') ";

        public ConcurData()
        {
            settings = JsonSettings.Load<ConcurSyncSettings>();
            ds = new DataStore();
        }

        public async Task GetUsers(int maxCount)
        {
            
            RestAPI r = new RestAPI();
            string baseUri = "/profile/identity/v4/Users/";
            string response = await r.MakeRestCall("GET", baseUri, new System.Collections.Specialized.NameValueCollection());
            JObject data = JObject.Parse(response);

            int totalResults = Convert.ToInt32(data["totalResults"].ToString());
            if (totalResults > maxCount && maxCount > 0)
            {
                totalResults = maxCount;
            }

            int startIndex = Convert.ToInt32(data["startIndex"].ToString());
            int itemsPerPage = 100;
            int pageCount = totalResults / itemsPerPage;
            int userCount;
            int postedUserCount = 0;
            string uri;
            ConcurUser user;
            if (totalResults % 100 == 0)
            {
                pageCount = pageCount - 1;
            }
            for (int i = 0; i <= pageCount; i++)
            {
                startIndex = (i * itemsPerPage) + 1;
                uri = string.Format("{0}?count={1}&startIndex={2}", baseUri, itemsPerPage, startIndex);
                response = await r.MakeRestCall("GET", uri, new System.Collections.Specialized.NameValueCollection());
                if (response is null)
                {
                    Log.LogTrace("response is null: " + uri);
                    return;
                }

                data = JObject.Parse(response);
                if (settings.SaveFiles)
                {
                    Utils.LogJson("user_" + i + ".json", response, false);
                }
                userCount = data["Resources"].Count();
                JArray dataArray = JArray.Parse(data["Resources"].ToString());

                for (int j = 0; j < userCount; j++)
                {
                    user = ParseUser(JObject.Parse(dataArray[j].ToString()));
                    if (user.hrId != null)
                    {
                        if (settings.SaveFiles)
                        {
                            Utils.LogJson(user.hrId, "user_" + user.hrId + "_v1.json", dataArray[j].ToString(), false);
                        }

                        response = await r.MakeRestCall("GET", "/profile/spend/v4/Users/" + user.id, new System.Collections.Specialized.NameValueCollection(), false);
                        if (response != null)
                        {
                            user = ParseExense(user, JObject.Parse(response));
                            if (settings.SaveFiles)
                            {
                                Utils.LogJson(user.hrId, "user_expense_" + user.hrId + "_v1.json", response, false);
                            }
                        }

                        response = await r.MakeRestCall("GET", "/profile/travel/v4/Users/" + user.id, new System.Collections.Specialized.NameValueCollection());
                        if (response != null)
                        {
                            user = ParseTravel(user, JObject.Parse(response));
                            if (settings.SaveFiles)
                            {
                                Utils.LogJson(user.hrId, "user_travel_" + user.hrId + "_v1.json", response, false);
                            }
                        }
                        user.Post();
                        postedUserCount++;
                        if (maxCount > 0 && postedUserCount >= maxCount)
                        {
                            return;

                        }
                    }
                }
            }
        }

        public async Task GetUsers()
        {
            ds.ExecuteNonQuery(" delete from concuruser ");
            await GetUsers(-1);
        }


        public async Task<ConcurUser> GetUserByPayrollId(string payrollId)
        {
            System.Collections.Specialized.NameValueCollection nvc = new System.Collections.Specialized.NameValueCollection();
            nvc.Add("filter", String.Format("employeeNumber eq \"{0}\"", payrollId));
            RestAPI r = new RestAPI();
            string response = await r.MakeRestCall("GET", "/profile/identity/v4/Users", nvc);
            if (response == null)
            {
                return null;
            }

            JObject data = JObject.Parse(response);
            if (data["totalResults"].ToString() != "1")
            {
                return null;
            }
            JArray dataArray = JArray.Parse(data["Resources"].ToString());
            ConcurUser user = ParseUser(JObject.Parse(dataArray[0].ToString()));
            if (settings.SaveFiles)
            {
                Utils.LogJson(user.hrId, "user_sync_" + user.hrId + "_v1.json", response, false);
            }

            string uri = "/profile/spend/v4/Users/" + user.id;
            response = await r.MakeRestCall("GET", uri, new System.Collections.Specialized.NameValueCollection(), false);
            if (response != null)
            {
                data = JObject.Parse(response);
                user = ParseExense(user, data);
                if (settings.SaveFiles)
                {
                    Utils.LogJson(user.hrId, "user_expense_" + user.hrId + "_v1.json", response, false);
                }
            }

            uri = "/profile/travel/v4/Users/" + user.id;
            response = await r.MakeRestCall("GET", uri, new System.Collections.Specialized.NameValueCollection());
            if (response != null)
            {
                user = ParseTravel(user, JObject.Parse(response));
                if (settings.SaveFiles)
                {
                    Utils.LogJson(user.hrId, "user_travel_" + user.hrId + "_v1.json", response, false);
                }
            }

            return user;
        }

        public async Task<ConcurUser> GetUserByHrId(string hrId)
        {
            DataTable dt = ds.GetDataTable("select c.id from hr_person h inner join concuruser c on c.employeenumber = h.spayrollid where h.sEmpCode = '" + hrId + "'");
            if (dt.Rows.Count == 1)
            {
                string concurId = dt.Rows[0].ItemArray[0].ToString();
                return await GetUserByConcurId(concurId);
            }

            dt = ds.GetDataTable("select h.sPayrollId, h.sOfficeLocation from hr_person h where h.sEmpCode = '" + hrId + "'");
            if (dt.Rows.Count == 0)
            {
                return null;
            }
            string payrollId = dt.Rows[0].ItemArray[0].ToString();
            if (String.IsNullOrEmpty(payrollId))
            {
                payrollId = "T" + hrId;
            }
            string location = dt.Rows[0].ItemArray[1].ToString();
            ConcurUser user = await GetUserByPayrollId(payrollId);
            if (user != null) { return user; }

            if (location == "saskatoo")
            {
                payrollId = payrollId.Substring(2, payrollId.Length - 2) + "SK";
            }
            else if (location == "toronto")
            {
                payrollId = payrollId.Substring(2, payrollId.Length - 2) + "TOR";
            }

            return await GetUserByPayrollId(payrollId);
        }

        public async Task<ConcurUser> GetUserByConcurId(string id)
        {
            RestAPI r = new RestAPI();
            string uri = "/profile/identity/v4/Users/" + id;
            string response = await r.MakeRestCall("GET", uri, new System.Collections.Specialized.NameValueCollection());
            if (response is null)
            {
                return null;
            }
            JObject data = JObject.Parse(response);
            ConcurUser user = ParseUser(data);
            if (settings.SaveFiles)
            {
                Utils.LogJson(user.hrId, "user_" + user.hrId + "_v1.json", response, false);
            }

            uri = "/profile/spend/v4/Users/" + id;
            response = await r.MakeRestCall("GET", uri, new System.Collections.Specialized.NameValueCollection(), false);
            if (response != null)
            {
                data = JObject.Parse(response);
                user = ParseExense(user, data);
                if (settings.SaveFiles)
                {
                    Utils.LogJson(user.hrId, "user_expense_" + user.hrId + "_v1.json", response, false);
                }
            }

            uri = "/profile/travel/v4/Users/" + id;
            response = await r.MakeRestCall("GET", uri, new System.Collections.Specialized.NameValueCollection());
            if (response != null)
            {
                user = ParseTravel(user, JObject.Parse(response));
                if (settings.SaveFiles)
                {
                    Utils.LogJson(user.hrId, "user_travel_" + user.hrId + "_v1.json", response, false);
                }
            }

            return user;
        }

        public ConcurUser ParseUser(JObject data)
        {
            ConcurUser user = new ConcurUser();

            user.id = data["id"].ToString();
            user.displayName = data["displayName"].ToString();
            user.userName = data["userName"].ToString();
            user.employeeNumber = data[userSchema]["employeeNumber"].ToString();
            user.employeeNumberRaw = data[userSchema]["employeeNumber"].ToString();

            string pattern = @"([a-zA-Z]+)";
            Regex rg = new Regex(pattern);
            Match m = rg.Match(user.employeeNumber);
            if (m.Success)
            {
                switch (m.Groups[1].Value)
                {
                    case "TOR":
                        user.employeeNumber = user.employeeNumber.Replace("TOR", "");
                        user.employeeNumber = "90" + user.employeeNumber;
                        break;
                    case "SAK":
                        user.employeeNumber = user.employeeNumber.Replace("SAK", "");
                        user.employeeNumber = "90" + user.employeeNumber;
                        break;
                }
            }

            if (data[userSchema]["manager"] != null && data[userSchema]["manager"].ToString() != "")
            {
                user.managerId = data[userSchema]["manager"]["employeeNumber"].ToString();
            }

            string dtVal = data[userSchema]["startDate"].ToString();
            if (dtVal != "")
            {
                user.startDate = DateTime.Parse(dtVal);
            }
            if (data[userSchema]["terminationDate"] != null)
            {
                dtVal = data[userSchema]["terminationDate"].ToString();
                if (dtVal != "")
                {
                    user.terminationDate = DateTime.Parse(dtVal);
                }
            }
            user.isActive = Boolean.Parse(data["active"].ToString());

            if (!String.IsNullOrEmpty(user.employeeNumber))
            {
                if (user.employeeNumber.StartsWith("T"))
                {
                    string sql = "select sEmpCode from hr_person where sEmpCode = " + DataUtil.ToSqlString(user.employeeNumber.Replace("T", ""));
                    DataTable dt = ds.GetDataTable(sql);
                    if (dt.Rows.Count != 0)
                    {
                        user.hrId = dt.Rows[0].Field<string>("sEmpCode");
                    }
                }
                else
                {
                    string sql = "select sEmpCode from hr_person where sPayrollId = " + DataUtil.ToSqlString(user.employeeNumber);
                    DataTable dt = ds.GetDataTable(sql);
                    if (dt.Rows.Count != 0)
                    {
                        user.hrId = dt.Rows[0].Field<string>("sEmpCode");
                    }
                }
            }
            return user;
        }

        public ConcurUser ParseTravel(ConcurUser user, JObject data)
        {
            if (data[travelUserSchema]["manager"].ToString() != "")
            {
                user.travelManagerNumber = data[travelUserSchema]["manager"]["employeeNumber"].ToString();
                user.travelManagerId = data[travelUserSchema]["manager"]["value"].ToString();
            }

            int count = data[travelUserSchema]["customFields"].Count();
            string type;
            for (int i = 0; i < count; i++)
            {
                type = data[travelUserSchema]["customFields"][i]["name"].ToString();
                if (type == "Yardi-Business Unit")
                {
                    if (data[travelUserSchema]["customFields"][i]["value"] != null)
                    {
                        user.travelBU = data[travelUserSchema]["customFields"][i]["value"].ToString();
                    }
                }
                else if (type == "Yardi-Department")
                {
                    if (data[travelUserSchema]["customFields"][i]["value"] != null)
                    {
                        user.travelDepartment = data[travelUserSchema]["customFields"][i]["value"].ToString();
                    }
                }
            }
            user.ruleClass = data[travelUserSchema]["ruleClass"]["name"].ToString();
            return user;
        }

        public ConcurUser ParseExense(ConcurUser user, JObject data)
        {
            user.expenseCurrency = data[expenseUserSchema]["reimbursementCurrency"].ToString();
            user.expenseCountry = data[expenseUserSchema]["country"].ToString();
            if (data[expenseUserSchema]["biManager"].Count() > 0)
            {
                user.expenseManagerId = data[expenseUserSchema]["biManager"]["value"].ToString();
            }
            int count = data[expenseUserSchema]["customData"].Count();
            string type;
            for (int i = 0; i < count; i++)
            {
                type = data[expenseUserSchema]["customData"][i]["id"].ToString();
                if (type == "orgunit1")
                {
                    user.expenseCountry = data[expenseUserSchema]["customData"][i]["value"].ToString();
                }
                else if (type == "orgunit2")
                {
                    user.expenseSubsidiary = data[expenseUserSchema]["customData"][i]["value"].ToString();
                }
                else if (type == "orgunit3")
                {
                    user.expenseDepartment = data[expenseUserSchema]["customData"][i]["value"].ToString();
                }
            }

            if (data[expenseApproverSchema]["report"] != null)
            {
                user.expenseReportApproverId = data[expenseApproverSchema]["report"][0]["approver"]["value"].ToString();
            }

            if (data[expenseApproverSchema]["cashAdvance"] != null)
            {
                user.expenseCashApproverId = data[expenseApproverSchema]["cashAdvance"][0]["approver"]["value"].ToString();
            }

            count = data[roleSchema]["roles"].Count();
            for (int i = 0; i < count; i++)
            {
                if (data[roleSchema]["roles"][i]["roleName"].ToString() == "EXP_APPROVER")
                {
                    user.isApprover = true;
                }
            }

            return user;
        }
    }
}
