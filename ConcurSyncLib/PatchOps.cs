using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Nucs.JsonSettings;
using Newtonsoft.Json.Linq;
using System.Xml.Linq;

namespace ConcurSyncLib
{
    public class PatchOps
    {

        private ConcurSyncSettings settings;
        private RestAPI r;
        private string uri;
        private ConcurUser user;
        public PatchOps(ConcurUser user)
        {
            settings = JsonSettings.Load<ConcurSyncSettings>();
            r = new RestAPI();
            this.user = user;
            uri = "/profile/v4/Users/" + user.id;
        }

        //public void PatchCanada(string patch)
        //{
        //    if (settings.SaveFiles)
        //    {
        //        Utils.LogText(user.hrId, "patch_canada_" + user.hrId + "_v1.txt", patch, false);
        //    }

        //    r.MakeRestCall(uri, "PATCH", patch);

        //}

        public async Task PatchUsername(String userName) 
        {
            Log.LogTrace("PatchUserName: " + user.hrId);
            if (settings.LogOnly) { return; }
            string template = File.ReadAllText(settings.TemplateDir + "patch_username.json");
            StringBuilder patch = new StringBuilder();
            template = template.Replace("#userName#", userName);
            if (settings.SaveFiles)
            {
                Utils.LogText(user.hrId, "patch_user_name_" + user.hrId + "_v1.txt", template, false);
            }

            await r.MakeRestCall(uri, "PATCH", template);

        }



        public async Task PatchTravelClass(String travelClass)
        {
            Log.LogTrace("PatchTravelClass: " + user.hrId);
            if (settings.LogOnly) { return; }
            string template = File.ReadAllText(settings.TemplateDir + "patch_travel_class.json");
            StringBuilder patch = new StringBuilder();
            template = template.Replace("#travelClass#", travelClass);
            if (settings.SaveFiles)
            {
                Utils.LogText(user.hrId, "patch_travel_class_" + user.hrId + "_v1.txt", template, false);
            }

            await r.MakeRestCall(uri, "PATCH", template);

        }

        public async Task PatchRole()
        {
            Log.LogTrace("PatchRole: " + user.hrId);
            string template = File.ReadAllText(settings.TemplateDir + "patch_role.json");
            if (settings.SaveFiles)
            {
                Utils.LogText(user.hrId, "patch_role_" + user.hrId + "_v1.txt", template, false);
            }

            await r.MakeRestCall(uri, "PATCH", template);

        }

        public async Task PatchTermination()
        {
            Log.LogTrace("PatchTermination: " + user.hrId);
            if (settings.LogOnly) { return; }
            string template = File.ReadAllText(settings.TemplateDir + "patch_term_date.json");
            StringBuilder patch = new StringBuilder();
            template = template.Replace("\"#terminationDate#\"", "null");
            template = template.Replace("#active#", "true");
            if (settings.SaveFiles)
            {
                Utils.LogText(user.hrId, "patch_term_user_" + user.hrId + "_v1.txt", template, false);
            }

            await r.MakeRestCall(uri, "PATCH", template);

        }

        public async Task PatchTermination(DateTime termDate)
        {
            Log.LogTrace("PatchTermination: " + user.hrId);
            if (settings.LogOnly) { return; }
            string template = File.ReadAllText(settings.TemplateDir + "patch_term_date.json");
            StringBuilder patch = new StringBuilder();
            template = template.Replace("#terminationDate#", termDate.ToString("yyyy-MM-dd") + "T01:01:01Z");
            template = template.Replace("#active#", "false");
            if (settings.SaveFiles)
            {
                Utils.LogText(user.hrId, "patch_term_user_" + user.hrId + "_v1.txt", template, false);
            }

            await r.MakeRestCall(uri, "PATCH", template);

        }


        public async Task PatchEmployeeNumber(string employeeNumber)
        {
            Log.LogTrace("PatchEmployeeNumber: " + user.hrId);
            if (settings.LogOnly) { return; }
            string template = File.ReadAllText(settings.TemplateDir + "patch_employee_number.json");
            StringBuilder patch = new StringBuilder();
            template = template.Replace("#employeeNumber#", employeeNumber);
            if (settings.SaveFiles)
            {
                Utils.LogText(user.hrId, "patch_travel_custom_user_" + user.hrId + "_v1.txt", template, false);
            }

            await r.MakeRestCall(uri, "PATCH", template);


        }

        public async Task PatchExpenseApprover(string type, string approverNumber, string op, string approverId)
        {
            Log.LogTrace("PatchExpenseApprover: " + user.hrId);
            if (settings.LogOnly) { return; }
            string template = File.ReadAllText(settings.TemplateDir + "patch_expense_approver.json");

            template = template.Replace("#type#", type);
            template = template.Replace("#number#", approverNumber);
            template = template.Replace("#op#", op);

            if (settings.SaveFiles)
            {
                Utils.LogText(user.hrId, "patch_expense_approver_" + type + "_user_" + user.hrId + "_v1.txt", template, false);
            }

            await r.MakeRestCall(uri, "PATCH", template);

            template = File.ReadAllText(settings.TemplateDir + "patch_expense_manager.json");
            StringBuilder patch = new StringBuilder();
            patch.AppendFormat("\"id\": \"value\", \"value\": \"{0}\"  ", approverId);

            template = template.Replace("#token#", patch.ToString());

            if (settings.SaveFiles)
            {
                Utils.LogText(user.hrId, "patch_expense_manager_user_" + user.hrId + "_v1.txt", template, false);
            }

            await r.MakeRestCall(uri, "PATCH", template);

            template = File.ReadAllText(settings.TemplateDir + "patch_travel_manager.json");

            template = template.Replace("#token#", approverId);

            if (settings.SaveFiles)
            {
                Utils.LogText(user.hrId, "patch_travel_manager_user_" + user.hrId + "_v1.txt", template, false);
            }

            await r.MakeRestCall(uri, "PATCH", template);
        }

        public async Task PatchTravelCustomData(string name, string value)
        {
            if (settings.LogOnly) { return; }
            string template = File.ReadAllText(settings.TemplateDir + "patch_travel_custom_data.json");
            StringBuilder patch = new StringBuilder();
            patch.AppendFormat("\"name\": \"{0}\", \"value\": \"{1}\"  ", name, value);
            template = template.Replace("#token#", patch.ToString());
            if (settings.SaveFiles)
            {
                Utils.LogText(user.hrId, "patch_travel_custom_user_" + user.hrId + "_v1.txt", template, false);
            }

            await r.MakeRestCall(uri, "PATCH", template);

        }



        //public void PatchExpenseManager(string approverNumber)
        //{
        //    Log.LogTrace("PatchExpenseManager: " + user.hrId);
        //    if (settings.LogOnly) { return; }
        //    string template = File.ReadAllText(settings.TemplateDir + "patch_expense_manager.json");
        //    StringBuilder patch = new StringBuilder();
        //    patch.AppendFormat("\"id\": \"value\", \"value\": \"{0}\"  ", approverNumber);

        //    template = template.Replace("#token#", patch.ToString());

        //    if (settings.SaveFiles)
        //    {
        //        Utils.LogText(user.hrId, "patch_expense_manager_user_" + user.hrId + "_v1.txt", template, false);
        //    }

        //    r.MakeRestCall(uri, "PATCH", template);

        //}

        //public void PatchTravelManager(string approverNumber)
        //{
        //    Log.LogTrace("PatchTravelManager: " + user.hrId);
        //    if (settings.LogOnly) { return; }
        //    string template = File.ReadAllText(settings.TemplateDir + "patch_travel_manager.json");

        //    template = template.Replace("#token#", approverNumber);

        //    if (settings.SaveFiles)
        //    {
        //        Utils.LogText(user.hrId, "patch_travel_manager_user_" + user.hrId + "_v1.txt", template, false);
        //    }

        //    r.MakeRestCall(uri, "PATCH", template);
        //}



        //public void PatchExpenseDepartment()
        //{
        //    Log.LogTrace("PatchTravelClass: " + user.hrId);
        //    if (settings.LogOnly) { return; }
        //    string template = File.ReadAllText(settings.TemplateDir + "patch_expense_department.json");
        //    StringBuilder patch = new StringBuilder();
        //    patch.AppendFormat("{{ \"id\": \"orgunit1\", \"value\": \"{0}\" }}, ", user.expenseCountry);
        //    patch.AppendFormat("{{ \"id\": \"orgunit2\", \"value\": \"{0}\" }}, ", user.expenseSubsidiary);
        //    patch.AppendFormat("{{ \"id\": \"orgunit3\", \"value\": \"{0}\" }}, ", user.expenseDepartment);
        //    patch.AppendFormat("{{ \"id\": \"custom15\", \"value\": \"1\" }}, ");
        //    patch.AppendFormat("{{ \"id\": \"custom20\", \"value\": \"5-X\" }}, ");
        //    patch.AppendFormat("{{ \"id\": \"custom18\", \"value\": \"J\" }}, ");
        //    patch.AppendFormat("{{ \"id\": \"custom19\", \"value\": \"{0}\" }}, ", user.employeeNumber);
        //    patch.AppendFormat("{{ \"id\": \"custom16\", \"value\": \"N\" }}, ");
        //    patch.AppendFormat("{{ \"id\": \"orgunit4\", \"value\": \"N\" }}, ");
        //    patch.AppendFormat("{{ \"id\": \"custom21\", \"value\": \"US\" }} ");

        //    template = template.Replace("#token#", patch.ToString());

        //    if (settings.SaveFiles)
        //    {
        //        Utils.LogText(user.hrId, "patch_expense_department_user_" + user.hrId + "_v1.txt", template, false);
        //    }
        //    r.MakeRestCall(uri, "PATCH", template);
        //}




    }
}
