using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Data;
using Nucs.JsonSettings;

namespace ConcurSyncLib
{
    public class DataStore
    {

        private System.Data.SqlClient.SqlConnection conn;


        public DataStore(String connstr) 
        {
            conn = new SqlConnection(connstr);
            try
            {
                conn.Open();
                if (conn.State != System.Data.ConnectionState.Open)
                {
                    Console.WriteLine("Unable to open DB.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                conn.Close();
            }

        }

        public DataStore() 
        {
            ConcurSyncSettings Settings = JsonSettings.Load<ConcurSyncSettings>();
            if (Settings.IsUat)
            {
                conn = new SqlConnection(Settings.UatConnStr);
            }
            else
            {
                conn = new SqlConnection(Settings.ConnStr);
            }
            
            try
            {
                conn.Open();
                if (conn.State != System.Data.ConnectionState.Open)
                {
                    Console.WriteLine("Unable to open DB.");
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                conn.Close();
            }


        }

        public Int32 ExecuteNonQueryWithId(string sql)
        {
            try
            {
                SqlCommand command = new SqlCommand();
                command.CommandText = sql;
                command.CommandTimeout = 120;
                conn.Open();
                command.Connection = conn;
                command.ExecuteNonQuery();

                command = new SqlCommand();
                command.CommandText = "select scope_identity();";
                command.Connection = conn;
                SqlDataAdapter adapter = new SqlDataAdapter();
                adapter.SelectCommand = command;
                DataSet ds = new DataSet();
                adapter.Fill(ds);
                conn.Close();
                return Convert.ToInt32(ds.Tables[0].Rows[0].ItemArray[0].ToString());

            }
            catch (Exception ex)
            {
                Log.LogTrace(ex.Message);
                throw (ex);
            }
            finally
            {
                conn.Close();
            }
            
        }
        

            public void ExecuteNonQuery(string sql) {

            try
            {
                SqlCommand command = new SqlCommand();
                command.CommandText = sql;
                command.CommandTimeout = 120;
                conn.Open();
                command.Connection = conn;
                command.ExecuteNonQuery();

            }
            catch (Exception ex)
            {
                Log.LogTrace(ex.Message);
                throw (ex);
            }
            finally
            {
                conn.Close();
            }


        }

        public DataTable GetDataTable (String sql)
        {
            SqlCommand command = new SqlCommand();
            conn.Open();
            command.CommandText = sql;
            command.Connection = conn;
            SqlDataAdapter adapter = new SqlDataAdapter();
            adapter.SelectCommand = command;
            DataSet ds = new DataSet();
            adapter.Fill(ds);
            conn.Close();
            return ds.Tables[0];

        }



    }
}
