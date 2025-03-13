using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Runtime;
using Nucs.JsonSettings;
using System.IO;
using System.CodeDom;
using Newtonsoft.Json.Linq;
using System.Diagnostics.Eventing.Reader;
using System.Reflection;
using System.Runtime.InteropServices;
using Newtonsoft.Json.Bson;


namespace ConcurSyncLib
{
    public class SyncUserUtil
    {
        private DataStore ds;
        private DataTable dtIdMapping;
        private System.Text.StringBuilder sql = new System.Text.StringBuilder();
        private PatchOps p;
        private string HrId = "";
        ConcurSyncSettings settings;


        public SyncUserUtil()
        {
            ds = new DataStore();

            settings = JsonSettings.Load<ConcurSyncSettings>();
            dtIdMapping = ds.GetDataTable(" select id, employeeNumber from concuruser ");
        }


        public async Task<bool> SyncUser(string HrId)
        {
            this.HrId = HrId;
            await compareTermination();
            await compareRole();
            await compareApprovers();
            await compareFlightClass();
            await compareTravelCustomFields();
            await ComparePayrollId();
            return true;

        }

        public async Task<bool> SyncUsers()
        {
            try
            {
                Log.LogTrace("compareTermination start");
                await compareTermination();
                Log.LogTrace("compareTermination end");

                Log.LogTrace("compareRole start");
                await compareRole();
                Log.LogTrace("compareRole end");

                Log.LogTrace("compareApprovers start");
                await compareApprovers();
                Log.LogTrace("compareApprovers end");

                Log.LogTrace("compareFlightClass start");
                await compareFlightClass();
                Log.LogTrace("compareFlightClass end");

                Log.LogTrace("compareTravelCustomFields start");
                await compareTravelCustomFields();
                Log.LogTrace("compareTravelCustomFields end");

                Log.LogTrace("comparePayrollId start");
                await ComparePayrollId();
                Log.LogTrace("comparePayrollId end");
            }
            catch (Exception ex)
            {
                Log.LogTrace("SyncUsers Exception: " + ex.Message);
                Log.LogTrace("SyncUsers Exception: " + ex.StackTrace);
            }
            return true;
        }

        private async Task compareRole()
        {
            sql.Length = 0;
            sql.Append(" select cu.id ");
            sql.Append(" from hr_person h ");
            sql.Append(" inner join concuruser cu on cu.employeeNumber = h.sPayrollID ");
            sql.Append(" where h.sempcode in ( ");
            sql.Append(" select distinct h.sExpenseReportApprover ");
            sql.Append(" from hr_person h ");
            sql.Append(" where h.STYPE = 'Employee'   ");
            sql.Append(" and h.SCOUNTRY in ('US', 'Canada', 'Netherlands', 'Germany', 'UK', 'Australia', 'Singapore', 'UAE', 'Hong Kong', 'France')  ");
            sql.Append(" and isnull(h.bInactive, 0) = 0 ");
            sql.Append(" ) ");
            sql.Append(" and cu.isapprover = 0 ");
            sql.Append(" and isnull(h.bInactive, 0) = 0 ");
            if (HrId != "")
            {
                sql.AppendFormat(" and h.sEmpCode = {0} ", DataUtil.ToSqlString(HrId));
            }
            sql.Append(" order by h.sEmpCode ");
            DataTable dt = ds.GetDataTable(sql.ToString());
            foreach (DataRow dr in dt.Rows)
            {
                ConcurData cd = new ConcurSyncLib.ConcurData();
                ConcurUser concurUser = await cd.GetUserByConcurId(dr.ItemArray[0].ToString());

                if (concurUser != null)
                {
                    p = new PatchOps(concurUser);
                    await p.PatchRole();

                }
            }



        }

        private async Task ComparePayrollId()
        {
            sql.Length = 0;
            sql.Append(" select cu.id, cu.employeeNumber  ");
            sql.Append(" from concuruser cu ");
            sql.Append(" where cu.employeeNumber like 'T%' ");
            sql.Append(" order by cu.employeeNumber ");
            DataTable dt = ds.GetDataTable(sql.ToString());
            foreach (DataRow dr in dt.Rows)
            {
                ConcurData cd = new ConcurSyncLib.ConcurData();
                ConcurUser concurUser = await cd.GetUserByConcurId(dr.ItemArray[0].ToString());

                string payrollIdT = dr.ItemArray[1].ToString();
                string hrId = payrollIdT.Replace("T", "");

                sql.Length = 0;
                sql.Append(" select h.sPayrollId ");
                sql.Append(" from hr_person h ");
                sql.AppendFormat("where h.sEmpCode = '{0}' ", hrId);
                DataTable dt2 = ds.GetDataTable(sql.ToString());
                string payrollId = dt2.Rows[0].ItemArray[0].ToString();
                if (!string.IsNullOrEmpty(payrollId))
                {
                    p = new PatchOps(concurUser);
                    await p.PatchEmployeeNumber(payrollId);

                }
            }


        }

        private async Task compareTravelCustomFields()
        {
            sql.Length = 0;
            sql.Append(" select cu.id, h.sCountry  ");
            sql.Append(" from hr_person h ");
            sql.Append(" inner join concuruser cu on cu.employeenumber = h.sPayrollID ");
            sql.Append(" where h.STYPE = 'Employee' ");
            sql.Append(" and isnull(h.bInactive, 0) = 0 ");
            sql.AppendFormat(" and h.SCOUNTRY in {0} ", ConcurData.countryIn);
            sql.Append(" and (cu.travelDepartment = '' or cu.travelBusinessUnit = '') ");
            if (HrId != "")
            {
                sql.AppendFormat(" and h.sEmpCode = {0} ", DataUtil.ToSqlString(HrId));
            }
            sql.Append(" order by h.sempcode ");
            DataTable dt = ds.GetDataTable(sql.ToString());
            foreach (DataRow dr in dt.Rows)
            {
                ConcurData cd = new ConcurSyncLib.ConcurData();
                ConcurUser concurUser = await cd.GetUserByConcurId(dr.ItemArray[0].ToString());
                if (concurUser != null)
                {
                    p = new PatchOps(concurUser);
                    await p.PatchTravelCustomData("Yardi-Business Unit", "Corporate 053");

                    p = new PatchOps(concurUser);
                    await p.PatchTravelCustomData("Yardi-Department", "GA 013");
                }


            }

        }


        private async Task compareFlightClass()
        {
            sql.Length = 0;
            sql.Append(" select cu.id, h.sPayrollId, h.sCountry  ");
            sql.Append(" from hr_person h ");
            sql.Append(" inner join concuruser cu on cu.employeenumber = h.sPayrollID ");
            sql.Append(" where h.STYPE = 'Employee' ");
            sql.Append(" and isnull(h.bInactive, 0) = 0 ");
            sql.Append(" and cu.ruleClass not like 'VIP%' ");
            sql.Append(" and cu.ruleClass <> 'President' ");
            sql.Append(" and h.sJobTitle in ('Director - Global Solutions','Director','Senior Director','Industry Principal','Associate Director','Senior Counsel','Deputy General Counsel','Controller', ");
            sql.Append(" 'Creative Director','Editorial Director','President','CEO','Senior Vice President','Vice President','General Manager','Vice President/General Manager','Head HR, Finance & Administration') ");
            sql.Append(" and cu.ruleClass <> 'Expense Only' ");
            sql.AppendFormat(" and h.SCOUNTRY in {0} ", ConcurData.countryIn);
            sql.Append(" order by h.sempcode ");
            DataTable dt = ds.GetDataTable(sql.ToString());
            foreach (DataRow dr in dt.Rows)
            {
                ConcurData cd = new ConcurSyncLib.ConcurData();
                ConcurUser concurUser = await cd.GetUserByConcurId(dr.ItemArray[0].ToString());

                if (concurUser != null)
                {


                    string travelClass = "";
                    switch (dr.ItemArray[2].ToString())
                    {
                        case "US":
                            travelClass = "VIP-US";
                            break;
                        case "Canada":
                            travelClass = "VIP-CA";
                            break;
                        case "UK":
                            travelClass = "VIP-UK";
                            break;
                        case "Netherlands":
                            travelClass = "VIP-NL";
                            break;
                        case "Australia":
                            travelClass = "VIP-AU";
                            break;
                    }
                    if (travelClass != "")
                    {
                        p = new PatchOps(concurUser);
                        await p.PatchTravelClass(travelClass);
                    }
                }
            }
        }



        private async Task compareTermination()
        {
            System.Text.StringBuilder sql = new System.Text.StringBuilder();


            sql.Append(" select cu.id, h.sPayrollId, h.dtTerminationDate terminationDate ");
            sql.Append(" from hr_person h ");
            sql.Append(" inner join concuruser cu on cu.employeeNumber = h.sPayrollID ");
            sql.Append(" where ");
            sql.Append(" (cu.isActive = 1  and isnull(h.bInactive, 0) = -1  OR ");
            sql.Append(" cu.isActive = 0  and isnull(h.bInactive, 0) = 0)  ");
            sql.Append(" and h.STYPE = 'Employee' ");
            if (HrId != "")
            {
                sql.AppendFormat(" and h.sEmpCode = {0} ", DataUtil.ToSqlString(HrId));
            }
            sql.AppendFormat(" and h.SCOUNTRY in {0} ", ConcurData.countryIn);
            sql.Append(" and h.sPayrollID not in ( ");
            sql.Append(" select h.sPayrollID ");
            sql.Append(" from hr_person h ");
            sql.Append(" where h.STYPE = 'Employee' ");
            sql.Append(" and isnull(h.spayrollid, '') <> '' ");
            sql.Append(" group by h.sPayrollID ");
            sql.Append(" having count(*) > 1 ");
            sql.Append(" ) ");
            sql.Append(" order by h.sempcode ");
            DataTable dt = ds.GetDataTable(sql.ToString());
            foreach (DataRow dr in dt.Rows)
            {
                ConcurData cd = new ConcurSyncLib.ConcurData();
                ConcurUser concurUser = await cd.GetUserByConcurId(dr.ItemArray[0].ToString());
                if (concurUser != null)
                {
                    if (dr.ItemArray[2].ToString() == "")
                    {
                        p = new PatchOps(concurUser);
                        await p.PatchTermination();
                    }
                    else
                    {
                        DateTime term = dr.Field<DateTime>("terminationDate");
                        p = new PatchOps(concurUser);
                        await p.PatchTermination(term);
                    }
                }
            }

        }

        private async Task compareApprovers(DataTable dt, string op)
        {
            foreach (DataRow dr in dt.Rows)
            {
                ConcurData cd = new ConcurSyncLib.ConcurData();
                ConcurUser concurUser = await cd.GetUserByConcurId(dr.ItemArray[0].ToString());
                if (concurUser != null)
                {
                    sql.Length = 0;
                    sql.AppendFormat(" employeeNumber = {0} ", DataUtil.ToSqlString(dr.ItemArray[2].ToString()));
                    DataRow[] dr2 = dtIdMapping.Select(sql.ToString());
                    if (dr2 is null)
                    {
                        Log.LogTrace("missing expenseManagerNumber: " + concurUser.id);
                        return;
                    }
                    string travelManagerId = dr2[0].Field<string>("id");


                    p = new PatchOps(concurUser);
                    await p.PatchExpenseApprover("report", dr.ItemArray[2].ToString(), op, travelManagerId);
                    //p.PatchExpenseApprover("cashAdvance", dr.ItemArray[1].ToString(), op, travelManagerId);
                    //p.PatchTravelManager(travelManagerId);
                    //p.PatchExpenseManager(travelManagerId);
                }
            }

        }

        private async Task compareApprovers()
        {
            //exp approver is wrong
            //Log.LogTrace("expenseApproverWrong start");
            sql.Length = 0;
            sql.Append(" select cu.id, h.sPayrollId, hrexpapr.sPayrollID ");
            sql.Append(" from hr_person h ");
            sql.Append(" inner join hr_person hrexpapr on h.sExpenseReportApprover = hrexpapr.sEmpCode ");
            sql.Append(" inner join concuruser cu on cu.employeenumber = h.sPayrollID ");
            sql.Append(" inner join concuruser cuexpapr on cuexpapr.id = cu.expenseReportApproverId ");
            sql.Append(" inner join hr_person hrexpapr2 on cuexpapr.employeeNumber = hrexpapr2.sPayrollID ");
            sql.Append(" where h.STYPE = 'Employee' ");
            sql.Append(" and isnull(h.bInactive, 0) = 0 ");
            sql.AppendFormat(" and h.SCOUNTRY in {0} ", ConcurData.countryIn);
            sql.Append(" and cuexpapr.employeeNumber <> hrexpapr.sPayrollID ");
            sql.Append(" and isnull(hrexpapr.bInactive, 0) = 0 ");
            if (HrId != "")
            {
                sql.AppendFormat(" and h.sEmpCode = {0} ", DataUtil.ToSqlString(HrId));
            }
            sql.Append(" order by h.sempcode ");

            DataTable dt = ds.GetDataTable(sql.ToString());
            await compareApprovers(dt, "replace");

            //exp approver is missing
            sql.Length = 0;
            sql.Append(" select cu.id, h.sPayrollId, hrexpapr.sPayrollID ");
            sql.Append(" from hr_person h ");
            sql.Append(" inner join concuruser cu on cu.employeenumber = h.sPayrollID ");
            sql.Append(" inner join hr_person hrexpapr on h.sExpenseReportApprover = hrexpapr.sEmpCode ");
            sql.Append(" where h.STYPE = 'Employee' ");
            sql.Append(" and isnull(h.bInactive, 0) = 0 ");
            sql.Append(" and isnull(hrexpapr.bInactive, 0) = 0 ");
            sql.Append(" and h.SCOUNTRY in  ('US', 'Canada', 'Netherlands', 'Germany', 'UK', 'Australia', 'Singapore', 'UAE', 'Hong Kong', 'France') ");
            sql.Append(" and cu.expenseReportApproverId = '' ");
            sql.Append(" order by h.sPayrollId ");
            dt = ds.GetDataTable(sql.ToString());
            await compareApprovers(dt, "add");
            Log.LogTrace("expenseApproverMissing end");




        }

        //public bool SyncUser(String hrId)
        //{
        //    ConcurData cd = new ConcurSyncLib.ConcurData();
        //    ConcurUser concurUser = cd.GetUserByHrId(hrId);
        //    if (concurUser == null)
        //    {
        //        Log.LogError("cannot read concur user: " + hrId);
        //        return false;
        //    }
        //    concurUser.Post();

        //    HRUser hrUser = new ConcurSyncLib.HRUser();
        //    hrUser.ReadByHrId(hrId);

        //    DoCompare(concurUser, hrUser);

        //    return true;
        //}


        //public void DoCompare(ConcurUser concurUser, HRUser hrUser)
        //{
        //    p = new PatchOps(concurUser);
        //    change = false;
        //    this.concurUser = concurUser;
        //    this.hrUser = hrUser;

        //    compareExpenseManager();
        //    compareExpenseApprover();
        //    compareTravelManager();
        //    compareTermination();

        //    if (change && !settings.LogOnly)
        //    {
        //        ConcurSyncLib.ConcurData sync = new ConcurSyncLib.ConcurData();
        //        ConcurSyncLib.ConcurUser ConcurUser = sync.GetUserByConcurId(concurUser.id);
        //        ConcurUser.Post();
        //        change = true;
        //        p.PatchTravelManager();

        //    }

        //}

        //private void compareTermination2()
        //{
        //    if (concurUser.terminationDate != hrUser.terminationDate)
        //    {
        //        Log.LogTrace(string.Format("term date:{0}, {1}, {2} ", hrUser.hrId, concurUser.terminationDate, hrUser.terminationDate));
        //        if (!settings.LogOnly)
        //        {
        //            concurUser.terminationDate = hrUser.terminationDate;
        //            change = true;
        //            p.PatchTermination();
        //        }
        //    }
        //    //template = template.Replace("#startDate#", hrUser.startDate.ToString("yyyy-MM-dd") + "T01:01:01Z");
        //}


        //private void compareTravelManager()
        //{

        //    Log.LogTrace(string.Format("expense approver:{0}, {1}, {2}", hrUser.hrId, concurUser.expenseReportApproverNumber, hrUser.expenseManagerNumber));
        //    if (!settings.LogOnly)
        //    {
        //        sql.Length = 0;
        //        sql.AppendFormat(" employeeNumber = {0} ", DataUtil.ToSqlString(hrUser.expenseManagerNumber));
        //        dr = dtIdMapping.Select(sql.ToString());
        //        if (dr is null)
        //        {
        //            Log.LogTrace("missing expenseManagerNumber: " + concurUser.id);
        //            return;
        //        }
        //        concurUser.travelManagerId = dr[0].Field<string>("id");

        //        change = true;
        //        p.PatchTravelManager(dr[0].Field<string>("id"));
        //    }

        //}

        //private void compareExpenseApprover()
        //{
        //    //for report approver
        //    sql.Length = 0;
        //    sql.AppendFormat(" id = {0} ", DataUtil.ToSqlString(concurUser.expenseReportApproverId));
        //    dr = dtIdMapping.Select(sql.ToString());
        //    if (dr.Length == 1)
        //    {
        //        concurUser.expenseReportApproverNumber = dr[0].Field<string>("employeeNumber");
        //    }
        //    else
        //    {
        //        concurUser.expenseReportApproverNumber = "";
        //    }

        //    if (concurUser.expenseReportApproverNumber != hrUser.expenseManagerNumber)
        //    {
        //        Log.LogTrace(string.Format("expense approver:{0}, {1}, {2}", hrUser.hrId, concurUser.expenseReportApproverNumber, hrUser.expenseManagerNumber));
        //        if (!settings.LogOnly)
        //        {
        //            concurUser.expenseReportApproverNumber = hrUser.expenseManagerNumber;
        //            change = true;
        //            p.PatchExpenseApprover("report");
        //        }
        //    }

        //    //for cash advance approver
        //    sql.Length = 0;
        //    sql.AppendFormat(" id = {0} ", DataUtil.ToSqlString(concurUser.expenseCashApproverId));
        //    dr = dtIdMapping.Select(sql.ToString());
        //    if (dr.Length == 1)
        //    {
        //        concurUser.expenseCashApproverNumber = dr[0].Field<string>("employeeNumber");
        //    }
        //    else
        //    {
        //        concurUser.expenseCashApproverNumber = "";
        //    }
        //    if (dr is null)
        //    {
        //        Log.LogTrace("missing expenseReportApproverId: " + concurUser.id);
        //        return;
        //    }


        //    if (concurUser.expenseCashApproverNumber != hrUser.expenseManagerNumber)
        //    {
        //        Log.LogTrace(string.Format("cash advance approver:{0}, {1}, {2} ", hrUser.hrId, concurUser.expenseReportApproverNumber, hrUser.expenseManagerNumber));
        //        if (!settings.LogOnly)
        //        {
        //            concurUser.expenseCashApproverNumber = hrUser.expenseManagerNumber;
        //            change = true;
        //            p.PatchExpenseApprover("cashAdvance");
        //        }

        //    }

        //}

        //private void compareExpenseManager()
        //{
        //    sql.Length = 0;
        //    sql.AppendFormat(" id = {0} ", DataUtil.ToSqlString(concurUser.expenseManagerId));
        //    dr = dtIdMapping.Select(sql.ToString());
        //    if (dr.Length == 1)
        //    {
        //        concurUser.expenseManagerNumber = dr[0].Field<string>("employeeNumber");
        //    }
        //    else
        //    {
        //        concurUser.expenseManagerNumber = "";
        //    }

        //    if (concurUser.expenseManagerNumber != hrUser.expenseManagerNumber)
        //    {
        //        Log.LogTrace(string.Format("expense manager:{0}, {1}, {2}", hrUser.hrId, concurUser.expenseManagerNumber, hrUser.expenseManagerNumber));
        //        if (!settings.LogOnly)
        //        {
        //            sql.Length = 0;
        //            sql.AppendFormat(" employeeNumber = {0} ", DataUtil.ToSqlString(hrUser.expenseManagerNumber));
        //            dr = dtIdMapping.Select(sql.ToString());
        //            concurUser.expenseManagerId = dr[0].Field<string>("id");
        //            change = true;
        //            p.PatchExpenseManager();
        //        }
        //    }
        //}


    }
}
