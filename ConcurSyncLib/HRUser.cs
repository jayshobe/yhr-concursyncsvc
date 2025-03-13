using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

namespace ConcurSyncLib
{
    public class HRUser
    {

        public string hrId;
        public string payrollId;
        public string firstName;
        public string middleName;
        public string lastName;
        public string state;
        public string location;
        public string email;        
        public DateTime terminationDate;
        public DateTime startDate;
        public string managerId;
        public string expenseManagerNumber;
        public string orgUnit;
        public string department;
        public string BU;
        public string country;
        public string legalEntity;


        public bool ReadByHrId(string id)
        {
            return DoRead(id, true);
        }

        public bool ReadByPayrollId(string id)
        {
            return DoRead(id, false);
        }

        private bool DoRead(string id, bool byHrId)
        {
            DataStore ds = new DataStore();
            StringBuilder sql = new StringBuilder();
            sql.Append(" select h.SEMPCODE hrCode, h.sPayrollID payrollId, h.sDepartment BU,  ");
            sql.Append(" h.sNewDepartment department, h.dtStart startDate, h.dtTerminationDate terminationDate, ");
            sql.Append(" repto.sPayrollId managerPayrollId, expman.sPayrollId expenseManagerNumber, h.sCountry country, h.sDivision legalEntity, ");
            sql.Append(" h.sFirstName FirstName, isnull(h.SMIDDLENAME, '') MiddleName, h.sLastName LastName, h.sState State, h.sOfficeLocation location, h.sEmail email ");
            sql.Append(" from hr_person h ");
            sql.Append(" inner JOIN hr_person repto ON H.sreportingto = repto.sempcode ");
            sql.Append(" inner JOIN hr_person expman ON H.sExpenseReportApprover = expman.sempcode ");
            sql.Append(" inner join property p on p.hmy = h.hProperty ");
            sql.Append(" and isnull(h.bInactive, 0) = 0 ");
            if (byHrId)
            {
                sql.AppendFormat(" where h.sEmpCode = {0} ", DataUtil.ToSqlString(id));
            }
            else
            {
                sql.AppendFormat(" where h.sPayrollId = {0} ", DataUtil.ToSqlString(id));
            }
            
            DataTable dt = ds.GetDataTable(sql.ToString());
            if (dt.Rows.Count != 1)
            {
                return false;
            }

            hrId = dt.Rows[0].Field<string>("hrCode");
            payrollId = dt.Rows[0].Field<string>("payrollId");
            department = dt.Rows[0].Field<string>("department");
            BU = dt.Rows[0].Field<string>("BU");
            payrollId = dt.Rows[0].Field<string>("payrollId");
            startDate = dt.Rows[0].Field<DateTime>("startDate");
            if (dt.Rows[0].Field<Object>("terminationDate") != null)
            {
                terminationDate = dt.Rows[0].Field<DateTime>("terminationDate");
            }
            managerId = dt.Rows[0].Field<string>("managerPayrollId");
            expenseManagerNumber = dt.Rows[0].Field<string>("expenseManagerNumber");
            country = dt.Rows[0].Field<string>("country");
            legalEntity = dt.Rows[0].Field<string>("legalEntity");
            firstName = dt.Rows[0].Field<string>("firstName");
            lastName = dt.Rows[0].Field<string>("lastName");
            middleName = dt.Rows[0].Field<string>("middleName");
            state = dt.Rows[0].Field<string>("state");
            location = dt.Rows[0].Field<string>("location");
            email = dt.Rows[0].Field<string>("email");
            return true;

        }


    }
}

