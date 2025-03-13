using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConcurSyncLib
{
    public class ConcurUser
    {

        public string id;
        public string hrId;
        public string displayName;
        public bool isActive;
        public string userName;
        public string employeeNumber;
        public string employeeNumberRaw;
        public DateTime terminationDate;
        public DateTime startDate;
        public string ruleClass;
        public string managerId;
        public string orgUnit;
        public string travelDepartment;
        public string travelBU;
        public string travelManagerNumber;
        public string travelManagerId;
        public string expenseDepartment;
        public string expenseBU;
        public string expenseCountry;
        public string expenseManagerId;
        public string expenseManagerNumber;
        public string expenseReportApproverId;
        public string expenseReportApproverNumber;
        public string expenseCashApproverId;
        public string expenseCashApproverNumber;
        public string expenseCurrency;
        public string expenseSubsidiary;
        public bool isApprover;

        public void Post()
        {
            DataStore ds = new DataStore();
            StringBuilder sql = new StringBuilder();
            sql.AppendFormat("delete from concuruser where id = {0}", DataUtil.ToSqlString(id));
            ds.ExecuteNonQuery(sql.ToString());

            sql.Length = 0;
            sql.AppendFormat("insert into concuruser (id, displayName, isActive, userName, employeeNumber, employeeNumberRaw, startDate, terminationDate, ");
            sql.AppendFormat("ruleClass, managerId, travelDepartment, travelBusinessUnit, expenseDepartment, expenseBusinessUnit, expenseCountry, ");
            sql.AppendFormat(" expenseSubsidiary, expenseManager, expenseCurrency, expenseReportApproverId, expenseReportApproverNumber, expenseCashApproverId, ");
            sql.AppendFormat(" expenseCashApproverNumber, travelManagerId, travelManagerNumber, isApprover ");
            sql.Append(" ) values ( ");
            sql.AppendFormat("{0}, ", DataUtil.ToSqlString(id));
            sql.AppendFormat("{0}, ", DataUtil.ToSqlString(displayName));
            sql.AppendFormat("{0}, ", (isActive ? 1 : 0).ToString());
            sql.AppendFormat("{0}, ", DataUtil.ToSqlString(userName));
            sql.AppendFormat("{0}, ", DataUtil.ToSqlString(employeeNumber));
            sql.AppendFormat("{0}, ", DataUtil.ToSqlString(employeeNumberRaw));
            sql.AppendFormat("{0}, ", DataUtil.ToSqlDate(startDate));
            sql.AppendFormat("{0}, ", DataUtil.ToSqlDate(terminationDate));
            sql.AppendFormat("{0}, ", DataUtil.ToSqlString(ruleClass));
            sql.AppendFormat("{0}, ", DataUtil.ToSqlString(managerId));
            sql.AppendFormat("{0}, ", DataUtil.ToSqlString(travelDepartment));
            sql.AppendFormat("{0}, ", DataUtil.ToSqlString(travelBU));
            sql.AppendFormat("{0}, ", DataUtil.ToSqlString(expenseDepartment));
            sql.AppendFormat("{0}, ", DataUtil.ToSqlString(expenseBU));
            sql.AppendFormat("{0}, ", DataUtil.ToSqlString(expenseCountry));
            sql.AppendFormat("{0}, ", DataUtil.ToSqlString(expenseSubsidiary));
            sql.AppendFormat("{0}, ", DataUtil.ToSqlString(expenseManagerId));
            sql.AppendFormat("{0}, ", DataUtil.ToSqlString(expenseCurrency));
            sql.AppendFormat("{0}, ", DataUtil.ToSqlString(expenseReportApproverId));
            sql.AppendFormat("{0}, ", DataUtil.ToSqlString(expenseReportApproverNumber));
            sql.AppendFormat("{0}, ", DataUtil.ToSqlString(expenseCashApproverId));
            sql.AppendFormat("{0}, ", DataUtil.ToSqlString(expenseCashApproverNumber));
            sql.AppendFormat("{0}, ", DataUtil.ToSqlString(travelManagerId));
            sql.AppendFormat("{0},", DataUtil.ToSqlString(travelManagerNumber));
            sql.AppendFormat("{0}", (isApprover ? 1 : 0));
            sql.Append(")");
            ds.ExecuteNonQuery(sql.ToString());
        }

    }
}
