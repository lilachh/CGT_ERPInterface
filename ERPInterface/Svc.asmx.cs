﻿using ERPInterface.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;

namespace ERPInterface
{
    /// <summary>
    /// Summary description for Svc
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)] 
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class Svc : System.Web.Services.WebService   
    {

        [WebMethod]
        public string HelloWorld(string input)
        {
            string ret = "Hello World";
            //ret = Test4InvCountJournal_Export();
            //Test4InvCountJournal_Import(input);
            ret = Test4InvMovementJournal_Export();
            return ret;
        }

        private string Test4InvCountJournal_Export()
        {
            InvCountTable header = new InvCountTable();
            InvCountLine line = new InvCountLine();
            InventDim invDim = new InventDim();
            //1
            invDim.InventLocationId = "CHR";
            invDim.inventBatchId = "S1064928";
            invDim.inventSerialId = "JRN-126321";
            invDim.wMsLocationId = "CTR1-IN";
            invDim.wMSPallentId = "M1000001";
            //2
            line.ItemId = "010160500";
            line.Qty = 123.45m;
            line.InventDim = invDim;
            //3
            header.Description = "demo data for counting journal";
            List<InvCountLine> lst = new List<InvCountLine>();
            lst.Add(line);
            header.LstCountLine = lst;
            //string s = Utility.XmlSerializeToString(header);
            return Utility.ObjectToJson(header);
        }
        private void Test4InvCountJournal_Import(string xml)
        {
            //InvCountTable header = (InvCountTable)Utility.XmlDeserializeFromString(xml, typeof(InvCountTable));
            InvCountTable header = (InvCountTable)Utility.JsonToObject(xml, new InvCountTable());
        }

        private string Test4InvMovementJournal_Export()
        {
            InvMovementTable header = new InvMovementTable();
            InvMovementLine line = new InvMovementLine();
            InventDim invDim = new InventDim();
            //1
            invDim.InventLocationId = "CHR";
            invDim.inventBatchId = "S1064928";
            invDim.inventSerialId = "JRN-126321";
            invDim.wMsLocationId = "CTR1-IN";
            invDim.wMSPallentId = "M1000001";
            //2
            line.ItemId = "010160500";
            line.Qty = 123.45m;
            line.InventDim = invDim;
            //3
            header.Description = "demo data for movement journal";
            header.MovementType = "IMov";
            List<InvMovementLine> lst = new List<InvMovementLine>();
            lst.Add(line);
            header.ListMovementLine = lst;
            return Utility.ObjectToJson(header);
        }
    }
}
