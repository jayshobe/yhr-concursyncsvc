using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ConcurSyncLib
{
    public class ListData
    {
        public string listType;
        public string id;
        public string name;
        public string level1;
        public string level2;
        public string level3;

        public void Post()
        {
            DataStore ds = new DataStore();
            StringBuilder sql = new StringBuilder();
            sql.AppendFormat("insert into concurlist (listType, id, name, level1, level2, level3) values (");
            sql.AppendFormat("{0}, ", DataUtil.ToSqlString(listType));
            sql.AppendFormat("{0}, ", DataUtil.ToSqlString(id));
            sql.AppendFormat("{0}, ", DataUtil.ToSqlString(name));
            sql.AppendFormat("{0}, ", DataUtil.ToSqlString(level1));
            sql.AppendFormat("{0}, ", DataUtil.ToSqlString(level2));
            sql.AppendFormat("{0}) ", DataUtil.ToSqlString(level3));
            ds.ExecuteNonQuery(sql.ToString());
        }

        public async Task GetListItems2()
        {
            RestAPI r = new RestAPI();
            string uri = "/list/v4/items/4366a89a-916f-0740-99a9-71b000989a94";
            string response = await r.MakeRestCall("GET", uri, new System.Collections.Specialized.NameValueCollection());
            Utils.LogJson("list_items.json", response, false);

            uri = "/list/v4/items/8652cdf9-c12b-4051-b8d1-80e20840ce9b";
            response = await r.MakeRestCall("GET", uri, new System.Collections.Specialized.NameValueCollection());
            Utils.LogJson("list_items2.json", response, false);
        }

        public async Task GetListItems()
        {
            DataStore ds = new DataStore();
            StringBuilder sql = new StringBuilder();
            sql.AppendFormat("delete from concurlist ");
            ds.ExecuteNonQuery(sql.ToString());

            string uri = "/api/v3.0/common/listitems";
            int page = 1;
            RestAPI r = new RestAPI();
            string response = await r.MakeRestCall("GET", uri, new System.Collections.Specialized.NameValueCollection());
            Utils.LogXml("list_items" + page + ".xml", response, false);

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(response);
            XmlNode node = doc.SelectSingleNode("/ListItems/NextPage/text()");
            string offset = node?.Value;
            ParseListXml(doc);

            while (!string.IsNullOrEmpty(offset))
            {
                page++;
                uri = offset.Replace("https://" + RestAPI.baseUrl, "");
                response = await r.MakeRestCall("GET", uri, new System.Collections.Specialized.NameValueCollection());
                Utils.LogXml("list_items" + page + ".xml", response, false);
                doc = new XmlDocument();
                doc.LoadXml(response);
                ParseListXml(doc);
                node = doc.SelectSingleNode("/ListItems/NextPage/text()");
                offset = node?.Value;
            }
        }

        public async Task GetList()
        {
            int page = 1;
            string uri = "/list/v4/lists?page=" + page;
            RestAPI r = new RestAPI();
            string response = await r.MakeRestCall("GET", uri, new System.Collections.Specialized.NameValueCollection());
            Utils.LogJson("list_" + page + ".json", response, false);

            uri = "/list/v4/items/19b4622a-40da-5442-9081-54d78491d195";
            response = await r.MakeRestCall("GET", uri, new System.Collections.Specialized.NameValueCollection());
            Utils.LogJson("list_19b4622a-40da-5442-9081-54d78491d195.json", response, false);
        }

        private void ParseListXml(XmlDocument doc)
        {
            ListData list;
            XmlNode parent;
            XmlNodeList ids = doc.GetElementsByTagName("ListID");
            foreach (XmlNode node in ids)
            {
                if (node.InnerText == "gWniTIfmq2L2$po4iTrqe5Jh72kix$pH4Qeng")
                {
                    list = new ListData();
                    parent = node.ParentNode;
                    try
                    {
                        list.listType = "expense org";
                        list.id = parent.SelectNodes("ID").Item(0).InnerText;
                        list.name = parent.SelectNodes("Name").Item(0).InnerText;
                        list.level1 = parent.SelectNodes("Level1Code").Item(0).InnerText;
                        list.level2 = parent.SelectNodes("Level2Code").Item(0).InnerText;
                        list.level3 = parent.SelectNodes("Level3Code").Item(0).InnerText;
                        list.Post();
                    }
                    catch (Exception ex)
                    {
                        Log.LogTrace("exception: " + ex.Message);
                        Log.LogTrace("exception: " + ex.StackTrace);
                    }
                }
            }
        }
    }
}
