using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Nucs.JsonSettings;

namespace ConcurSyncLib
{
    public class CreateUserUtil
    {

        DataStore ds;
        ConcurSyncSettings settings;
        string saskatoon = "saskatoo";

        public CreateUserUtil()
        {
            ds = new DataStore();
            settings = JsonSettings.Load<ConcurSyncSettings>();
        }

        //public async Task FixCanadaIds()
        //{
        //    System.Text.StringBuilder sql = new System.Text.StringBuilder();
        //    sql.Append(" select h.sempcode ");
        //    sql.Append(" from hr_person h  ");
        //    sql.Append(" inner join concuruser cu on cu.employeeNumber = h.sPayrollId and isnull(h.sPayrollId, '') <> ''  ");
        //    sql.Append(" where isnull(h.binactive, 0) = 0  ");
        //    sql.Append(" and h.STYPE = 'Employee'  ");
        //    sql.Append(" and h.SCOUNTRY = 'Canada' ");
        //    sql.Append(" order by h.SEMPCODE ");
        //    DataTable dt = ds.GetDataTable(sql.ToString());
        //    foreach (DataRow dr in dt.Rows)
        //    {
        //        String hrId = dr.ItemArray[0].ToString();
        //        HRUser hrUser = new HRUser();
        //        hrUser.ReadByHrId(hrId);

        //        ConcurData cd = new ConcurSyncLib.ConcurData();
        //        ConcurUser concurUser = await cd.GetUserByHrId(hrId);

        //        string template = File.ReadAllText(settings.TemplateDir + "patch_travel_custom_data_canada.json");
        //        template = template.Replace("#stateProvince#", getStateProvince2(hrUser));
        //        template = template.Replace("#payrollID#", hrUser.payrollId);
        //        template = template.Replace("#legalEntity#", getLegalEntityCode(hrUser));
        //        template = template.Replace("#department#", getDepartmentCode(hrUser));
        //        template = template.Replace("#countryCode#", getCountryCode(hrUser));

        //        PatchOps p = new PatchOps(concurUser);
        //        p.PatchCanada(template);
                
        //    }


        //}

        public async Task<bool> CreateUsers()
        {
            System.Text.StringBuilder sql = new System.Text.StringBuilder();
            sql.Append(" select h.sempcode ");
            sql.Append(" from hr_person h ");
            sql.Append(" inner JOIN hr_person expman ON H.sExpenseReportApprover = expman.sempcode  inner join property p on p.hmy = h.hProperty  ");
            sql.Append(" left join concuruser cu on cu.employeeNumber = h.sPayrollId and isnull(h.sPayrollId, '') <> '' ");
            sql.Append(" where isnull(h.binactive, 0) = 0 ");
            sql.Append(" and h.STYPE = 'Employee' ");
            sql.Append(" and cu.employeeNumber is null ");
            sql.Append(" and isnull(h.sPayrollId, '') <> '' ");
            sql.Append(" and isnull(h.sExpenseReportApprover, '') <> '' ");
            sql.Append(" and isnull(expman.sPayrollId, '') <> '' ");
            sql.AppendFormat(" and h.SCOUNTRY in {0} ", ConcurData.countryIn);
            sql.Append(" order by h.SEMPCODE");
            DataTable dt = ds.GetDataTable(sql.ToString());
            foreach (DataRow dr in dt.Rows)
            {
                await CreateUser(dr.ItemArray[0].ToString(), false, false);
            }

            //2nd pass for employees with no payroll id
            sql.Length = 0;
            sql.Append(" select h.sempcode ");
            sql.Append(" from hr_person h ");
            sql.Append(" inner JOIN hr_person expman ON H.sExpenseReportApprover = expman.sempcode  inner join property p on p.hmy = h.hProperty  ");
            sql.Append(" left join concuruser cu on cu.employeeNumber = h.sPayrollId and isnull(h.sPayrollId, '') <> '' ");
            sql.Append(" where isnull(h.binactive, 0) = 0 ");
            sql.Append(" and h.STYPE = 'Employee' ");
            sql.Append(" and cu.employeeNumber is null ");
            sql.Append(" and isnull(h.sPayrollId, '') = '' ");
            sql.Append(" and isnull(h.sExpenseReportApprover, '') <> '' ");
            sql.Append(" and isnull(expman.sPayrollId, '') <> '' ");
            sql.AppendFormat(" and h.SCOUNTRY in {0} ", ConcurData.countryIn);
            sql.Append(" order by h.SEMPCODE");
            dt = ds.GetDataTable(sql.ToString());
            foreach (DataRow dr in dt.Rows)
            {
                await CreateUser(dr.ItemArray[0].ToString(), false, true);
            }


            return true;





        }

        public async Task<bool> CreateUser(String hrId, bool testUser, bool nullPayrollId)
        {
            DataTable dt;
            ConcurSyncLib.HRUser hrUser = new ConcurSyncLib.HRUser();
            if (nullPayrollId)
            {
                if (!hrUser.ReadByHrId(hrId))
                {
                    return false;
                }

            }
            else
            {
                dt = ds.GetDataTable("select sPayrollID from hr_person where sEmpCode = " + DataUtil.ToSqlString(hrId.ToString()));
                if (dt.Rows.Count == 0)
                {
                    return false;
                }
                String employeeNumber = dt.Rows[0].ItemArray[0].ToString();
                if (employeeNumber == "")
                {
                    return false;

                }
                if (!hrUser.ReadByPayrollId(employeeNumber))
                {
                    return false;
                }
            }
            if (hrUser.expenseManagerNumber == null)
            {
                return false;

            }

            dt = ds.GetDataTable("select username from concuruser where username = " + DataUtil.ToSqlString(hrUser.email));
            if (dt.Rows.Count > 0) {
                //await fixDuplicate(hrUser);
                return false;
            }

            dt = ds.GetDataTable("select cu.id from hr_person h inner join concuruser cu on cu.employeeNumber = h.sPayrollID where h.sPayrollID = " + DataUtil.ToSqlString(hrUser.expenseManagerNumber.ToString()));
            if (dt.Rows.Count == 0)
            {
                Log.LogError("cannot read expense approver: " + hrId);
                return false;

            }
            String expApproverId = dt.Rows[0].ItemArray[0].ToString();

            string template = File.ReadAllText(settings.TemplateDir + "post_new_user.json");
            if (testUser)
            {
                Random r = new Random();
                int randomNumber = r.Next(500000, 599999);
                template = template.Replace("#email#", randomNumber + "@yardi.com.uat");
            }
            else if (settings.IsUat)
            {
                template = template.Replace("#email#", hrUser.firstName + "." + hrUser.lastName + "@yardi.com.uat");
            }
            else
            {
                template = template.Replace("#email#", hrUser.email);
            }


            template = template.Replace("#firstName#", hrUser.firstName);
            template = template.Replace("#lastName#", hrUser.lastName);
            template = template.Replace("#middleName#", hrUser.middleName.Trim());
            template = template.Replace("#startDate#", hrUser.startDate.ToString("yyyy-MM-dd") + "T01:01:01Z");
            if (nullPayrollId)
            {
                template = template.Replace("#employeeNumber#", "T" + hrUser.hrId);
                template = template.Replace("#payrollID#", "T" + hrUser.hrId);
            }
            else
            {
                template = template.Replace("#employeeNumber#", hrUser.payrollId);
                template = template.Replace("#payrollID#", hrUser.payrollId);
            }   
            
            template = template.Replace("#expenseApprover#", expApproverId);
            template = template.Replace("#countryCode#", getCountryCode(hrUser));
            template = template.Replace("#stateProvince#", getStateProvince(hrUser));
            template = template.Replace("#stateProvince2#", getStateProvince2(hrUser));
            template = template.Replace("#legalEntity#", getLegalEntityCode(hrUser));
            template = template.Replace("#department#", getDepartmentCode(hrUser));
            template = template.Replace("#cashAdvanceAccountCode#", "1402-1420");

            if (settings.IsUat)
            {
                template = template.Replace("#companyId#", settings.UatCompanyId);
            }
            else
            {
                template = template.Replace("#companyId#", settings.CompanyId);
            }

            switch (hrUser.country)
            {
                case "US":
                    template = template.Replace("#country#", "US");
                    template = template.Replace("#currency#", "USD");
                    template = template.Replace("#payrollBatchID#", "1");
                    template = template.Replace("#payrollDeductionCode#", "J");
                    template = template.Replace("#payrollCompany#", "5-X");
                    template = template.Replace("#travelClass#", "General-US-S");
                    template = template.Replace("#orgUnit5#", "Yardi US");
                    break;
                case "Canada":
                    template = template.Replace("#country#", "CA");
                    template = template.Replace("#currency#", "CAD");
                    template = template.Replace("#payrollBatchID#", "2");
                    template = template.Replace("#payrollDeductionCode#", "E");
                    template = template.Replace("#payrollCompany#", "REF4");
                    template = template.Replace("#travelClass#", "General-CA-S");
                    if (hrUser.location == saskatoon)
                    {
                        template = template.Replace("#orgUnit5#", "Yardi SAK");
                    }
                    else
                    {
                        template = template.Replace("#orgUnit5#", "Yardi TOR");
                    }
                    break;
                case "Australia":
                    template = template.Replace("#country#", "AU");
                    template = template.Replace("#currency#", "AUD");
                    template = template.Replace("#payrollBatchID#", "3");
                    template = template.Replace("#payrollDeductionCode#", "E");
                    template = template.Replace("#payrollCompany#", "AU");
                    template = template.Replace("#travelClass#", "General-AU");
                    template = template.Replace("#orgUnit5#", "Yardi AU");
                    break;
                case "Germany":
                    template = template.Replace("#country#", "DE");
                    template = template.Replace("#currency#", "EUR");
                    template = template.Replace("#payrollBatchID#", "4");
                    template = template.Replace("#payrollDeductionCode#", "E");
                    template = template.Replace("#payrollCompany#", "DE");
                    template = template.Replace("#travelClass#", "Expense Only");
                    template = template.Replace("#orgUnit5#", "");
                    break;
                case "Netherlands":
                    template = template.Replace("#country#", "NL");
                    template = template.Replace("#currency#", "EUR");
                    template = template.Replace("#payrollBatchID#", "2");
                    template = template.Replace("#payrollDeductionCode#", "E");
                    template = template.Replace("#payrollCompany#", "NL");
                    template = template.Replace("#travelClass#", "General-NL");
                    template = template.Replace("#orgUnit5#", "Yardi NL");
                    break;
                case "Singapore":
                    template = template.Replace("#country#", "SG");
                    template = template.Replace("#currency#", "SGD");
                    template = template.Replace("#payrollBatchID#", "5");
                    template = template.Replace("#payrollDeductionCode#", "E");
                    template = template.Replace("#payrollCompany#", "SG");
                    template = template.Replace("#travelClass#", "Expense Only");
                    template = template.Replace("#orgUnit5#", "");
                    break;
                case "UAE":
                    template = template.Replace("#country#", "AE");
                    template = template.Replace("#currency#", "AED");
                    template = template.Replace("#payrollBatchID#", "7");
                    template = template.Replace("#payrollDeductionCode#", "E");
                    template = template.Replace("#payrollCompany#", "AE");
                    template = template.Replace("#travelClass#", "Expense Only");
                    template = template.Replace("#orgUnit5#", "");
                    break;
                case "UK":
                    template = template.Replace("#country#", "GB");
                    template = template.Replace("#currency#", "GBP");
                    template = template.Replace("#payrollBatchID#", "1");
                    template = template.Replace("#payrollDeductionCode#", "E");
                    template = template.Replace("#payrollCompany#", "UK");
                    template = template.Replace("#travelClass#", "General-UK");
                    template = template.Replace("#orgUnit5#", "Yardi UK");
                    break;
            }

            if (settings.SaveFiles)
            {
                Utils.LogJson("new_user_body_" + hrId + ".json", template, false);
            }


            RestAPI rest = new RestAPI();
            string response = await rest.MakeRestCall("/profile/v4/Users/", "POST", template);

            if (response == null)
            {
                Log.LogError("post user returned null: " + hrId);
                Log.LogError("request body: " + template);
                return false;
            }
            Log.LogTrace("CreateUser: " + hrId);
            


            //JObject data = JObject.Parse(response);
            //ConcurData cd = new ConcurData();
            //ConcurUser newUser = cd.ParseUser(data);
            //string provisionId = data["meta"]["provisionId"].ToString();

            //bool pending = true;
            //string uri;
            //Int32 i = 0;
            //while (pending)
            //{
            //    // wait for concur to finish
            //    System.Threading.Thread.Sleep(1000);
            //    uri = String.Format("/profile/v4/provisions/{0}/status", provisionId);
            //    Log.LogTrace("status: " + uri);
            //    //response = await rest.MakeRestCall(uri, "GET", "");
            //    response = await rest.MakeRestCall(uri, "GET", new System.Collections.Specialized.NameValueCollection());
            //    data = JObject.Parse(response);
            //    string success = data["status"]["success"].ToString();
            //    Int32 pendingCount = Convert.ToInt32(data["operationsCount"]["pending"].ToString());
            //    if (pendingCount == 0)
            //    {
            //        pending = false;
            //        Int32 failedCount = Convert.ToInt32(data["operationsCount"]["failed"].ToString());
            //        if (failedCount != 0)
            //        {
            //            Log.LogError("post user failed: " + hrId);
            //            return false;
            //        }
            //    }
            //    i++;
            //    if (i > 30)
            //    {
            //        Log.LogError("post user never finished: " + hrId);
            //        return false;
            //    }
            //}

            //Log.LogTrace("CreateUser success: " + hrId);
            return true;
        }

        private string getStateProvince2(HRUser user)
        {
            switch (user.country)
            {
                case "Canada":
                    if (user.location == saskatoon)
                    {
                        return "SK";
                    }
                    else
                    {
                        return "TOR";
                    }
                case "UAE":
                    return "AE";
                case "UK":
                    return "UK";
                case "US":
                    return "US";
                case "Australia":
                    return "AU";
                case "Germany":
                    return "DE";
                case "Netherlands":
                    return "NL";
                case "Singapore":
                    return "SG";
                default:
                    return "";
            }
        }

        private string getStateProvince(HRUser user)
        {
            switch (user.country)
            {
                case "Canada":
                    if (user.location == saskatoon)
                    {
                        return "SK";
                    }
                    else
                    {
                        return "ON";
                    }
                default:
                    return "";
            }
        }

        private string getCountryCode(HRUser user)
        {
            switch (user.country)
            {
                case "Canada":
                    if (user.location == saskatoon)
                    {
                        return "47";
                    }
                    else
                    {
                        return "12";
                    }
                case "Australia":
                    return "22";
                case "Germany":
                    return "27";
                case "Hong Kong":
                    return "24";
                case "Netherlands":
                    return "21";
                case "Singapore":
                    return "49";
                case "UAE":
                    return "57";
                case "UK":
                    return "20";
                case "US":
                    switch (user.legalEntity)
                    {
                        case "SiteStuff":
                            return "25";
                        case "KUBE":
                            return "0008";
                        case "MSG":
                        case "YES Energy Management":
                        case "YES Multifamily (MSG)":
                            return "37";
                        case "Peak":
                            return "52";
                        case "Pierce-Eislen, Inc.":
                            return "59";
                        case "RightSource Compliance, LLC":
                            return "0010";
                        case "Rentgrow":
                            return "41";
                        default:
                            return "00";
                    }
                default:
                    return "00";
            }
        }

        private string getLegalEntityCode(HRUser user)
        {
            switch (user.country)
            {
                case "Canada":
                    if (user.location == saskatoon)
                    {
                        return "1834";
                    }
                    else
                    {
                        return "1811";
                    }
                case "Australia":
                    return "1814";
                case "Germany":
                    return "1825";
                case "Hong Kong":
                    return "1816";
                case "Netherlands":
                    return "1813";
                case "Singapore":
                    return "1835";
                case "UAE":
                    return "1839";
                case "UK":
                    return "1812";
                case "US":
                    switch (user.legalEntity)
                    {
                        case "SiteStuff":
                            return "1817";
                        case "KUBE":
                            return "0008";
                        case "MSG":
                            return "1838";
                        case "YES Energy Management":
                        case "YES Multifamily (MSG)":
                            return "1829";
                        case "Peak":
                            return "1836";
                        case "Pierce-Eislen, Inc.":
                            return "1839";
                        case "RightSource Compliance, LLC":
                            return "0010";
                        case "Rentgrow":
                            return "1831";
                        default:
                            return "1000";
                    }
                default:
                    return "1000";
            }
        }

        private string getDepartmentCode(HRUser user)
        {
            switch (user.country)
            {
                case "Canada":
                case "US":
                    switch (user.legalEntity)
                    {
                        case "SiteStuff":
                            return "025";
                        case "KUBE":
                            return "0008";
                        case "MSG":
                            return "041";
                        case "YES Energy Management":
                        case "YES Multifamily (MSG)":
                            return "024";
                        case "Peak":
                            return "038";
                        case "Pierce-Eislen, Inc.":
                            return "62";
                        case "RightSource Compliance, LLC":
                            return "0010";
                        case "Rentgrow":
                            return "032)";
                        default:
                            switch (user.department)
                            {
                                case "Cloud Services":
                                    return "014";
                                case "Consulting Practices":
                                    return "063";
                                case "CSD":
                                    return "008";
                                case "Finance":
                                    return "026";
                                case "G&A":
                                    return "002";
                                case "Human Resources":
                                    return "036";
                                case "IT":
                                    return "005";
                                case "Legal":
                                    return "035";
                                case "Marketing":
                                    return "037";
                                case "Operations":
                                    return "051";
                                case "Programming":
                                    return "010";
                                case "PSG":
                                    return "007";
                                case "Sales":
                                    return "001";
                                default:
                                    return "002";
                            }


                    }
                case "Australia":
                    return "22";
                case "Germany":
                    return "27";
                case "Hong Kong":
                    return "24";
                case "Netherlands":
                    return "21";
                case "Singapore":
                    return "49";
                case "UAE":
                    return "57";
                case "UK":
                    return "20";
                default:
                    return "002";
            }
        }


    }
}
