using AxaptaCOMConnector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace ERPInterface.Entities
{
    public class AXClassEntity
    {
        public static int GetTableId(Axapta ax, string tableName)
        {
            return (int)ax.CallStaticClassMethod("Global", "tableName2Id", tableName);
        }

        public static string FetchCustTable()
        {

            try
            {
                Axapta ax = new Axapta();
                ax.Logon();
                string range = "*shanghai*";
                int CustTable = 77;
                int CustTable_Name = 2;
                IAxaptaObject AxaptaQuery = ax.CreateObject("Query");
                IAxaptaObject AxaptaDataSource = AxaptaQuery.Call("AddDataSource", CustTable);
                IAxaptaObject AxaptaRange = AxaptaDataSource.Call("AddRange", CustTable_Name);
                AxaptaRange.Call("Value", range);
                IAxaptaObject AxaptaQueryRun = ax.CreateObject("QueryRun", AxaptaQuery);
                List<string> lstCust = new List<string>();
                int i = 1;
                StringBuilder sb = new StringBuilder();
                while (AxaptaQueryRun.Call("Next"))
                {
                    IAxaptaRecord CustTableBuffer = AxaptaQueryRun.Call("GetNo", 1);
                    string a = CustTableBuffer.field["Name"];
                    sb.Append("----" + a.ToString());
                    lstCust.Add(sb.ToString());
                    i++;
                }
                ax.Logoff();
                return sb.ToString();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


    }
}