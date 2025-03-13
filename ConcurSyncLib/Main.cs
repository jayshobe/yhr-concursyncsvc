namespace ConcurSyncLib
{
    public class Main
    {
        public static async Task<bool> DoWork()
        {
            try
            {

                ConcurSyncLib.Log.LogTrace("GetUsers start");
                ConcurSyncLib.ConcurData data = new ConcurSyncLib.ConcurData();
                await data.GetUsers();
                ConcurSyncLib.Log.LogTrace("GetUsers end");

                ConcurSyncLib.Log.LogTrace("SyncUsers start");
                ConcurSyncLib.SyncUserUtil d = new ConcurSyncLib.SyncUserUtil();
                await d.SyncUsers();
                ConcurSyncLib.Log.LogTrace("SyncUsers end");

                ConcurSyncLib.Log.LogTrace("CreateUsers start");
                ConcurSyncLib.CreateUserUtil cu = new ConcurSyncLib.CreateUserUtil();
                await cu.CreateUsers();
                ConcurSyncLib.Log.LogTrace("CreateUsers end");



                return true;
            }
            catch (Exception ex)
            {
                Log.LogFatal("DoWork failed.", ex);
                return false;
            }   
        }
    }
}
