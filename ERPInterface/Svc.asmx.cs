using AxaptaCOMConnector;
using ERPInterface.Entities;
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
        public string PurchPackingSlip(string input)
        {
            string ret = "";
            Axapta ax = new Axapta();
            try
            {
                // 1.0 get parameters
                PurchTable pt = (PurchTable)Utility.XmlDeserializeFromString(input, typeof(PurchTable));

                #region Log on 
                ax.Logon();
                ax.TTSBegin();
                // 2.0 update purchline receive now
                foreach (PurchLine pl in pt.LstPurchLine)
                {
                    InventDim dim = pl.InventDim;
                    IAxaptaRecord inventDim = ax.CreateRecord("InventDim");
                    inventDim.field["InventLocationId"] = dim.InventLocationId;
                    inventDim.field["inventBatchId"] = dim.inventBatchId;
                    inventDim.field["wMsLocationId"] = dim.wMsLocationId;
                    inventDim.field["wMSPalletId"] = dim.wMSPalletId;
                    inventDim.field["inventSerialId"] = dim.inventSerialId;
                    inventDim = ax.CallStaticRecordMethod("InventDim", "findOrCreate", inventDim);
                    string inventDimId = inventDim.field["inventDimId"];
                    IAxaptaRecord axRecord = ax.CreateRecord("PurchLine");
                    
                    string stmt = string.Format(
                        "select forupdate * from %1 where %1.PurchId =='{0}' && %1.LineNum == {1}"
                        ,pt.PurchId,pl.LineNum);
                    axRecord.ExecuteStmt(stmt);
                    pl.InventTransId = axRecord.field["InventTransId"];
                    axRecord.field["PurchReceivedNow"] = pl.Qty;
                    axRecord.field["inventDimId"] = inventDimId;
                    axRecord.Call("setInventReceivedNow");
                    axRecord.DoUpdate();
                }
                ax.TTSCommit();
                // 3.0 create packingslip parm
                // DocumentStatus::PackingSlip = 5
                IAxaptaObject purchFormLetter = 
                    ax.CallStaticClassMethod("PurchFormLetter", "construct",5) as IAxaptaObject;
                IAxaptaRecord purchTable = ax.CreateRecord("PurchTable");
                purchTable.ExecuteStmt(string.Format(
                        "select forupdate * from %1 where %1.PurchId =='{0}'"
                        , pt.PurchId));
                // 4.0 post packingslip
                //PurchUpdate::ReceiveNow = 0
                purchFormLetter.Call("update",purchTable,DateTime.Now.ToLongTimeString(), DateTime.Now,0);
                #endregion
            }
            catch (Exception ex)
            {
                ax.TTSAbort();
                ret = ex.Message;
            }
            finally
            {
                ax.Logoff();
            }
            return ret;
        }

        #region Test
        [WebMethod]
        public string HelloWorld(string input)
        {
            string ret = "Hello World";
            //ret = Test4InvCountJournal_Export();
            ////Test4InvCountJournal_Import(input);
            //ret = Test4InvMovementJournal_Export();
            //ret = Test4InvTransferJournal_Export();
            //ret = Test4PurchPackingSlip_Export();
            //ret = Test4SalesCreditNote_Export();
            //ret = Test4SalesPackingSlip_Export();
            //ret = AXClassEntity.FetchCustTable();

            //Test PurchPackingSlip
            ret = PurchPackingSlip(Test4PurchPackingSlip_Export());
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
            invDim.wMSPalletId = "M1000001";
            //2
            line.ItemId = "010160500";
            line.Qty = 123.45m;
            line.InventDim = invDim;
            //3
            header.Description = "demo data for counting journal";
            List<InvCountLine> lst = new List<InvCountLine>();
            lst.Add(line);
            header.LstCountLine = lst;
            return Utility.XmlSerializeToString(header);
            //return Utility.ObjectToJson(header);
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
            invDim.wMSPalletId = "M1000001";
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
            return Utility.XmlSerializeToString(header);
        }

        private string Test4InvTransferJournal_Export()
        {
            InvJournalTable header = new InvJournalTable();
            InvJournalLine line = new InvJournalLine();
            InventDim invDim = new InventDim();
            //1
            invDim.InventLocationId = "CHR";
            invDim.inventBatchId = "S1064928";
            invDim.inventSerialId = "JRN-126321";
            invDim.wMsLocationId = "CTR1-IN";
            invDim.wMSPalletId = "M1000001";
            //2
            line.ItemId = "010160500";
            line.Qty = 123.45m;
            line.InventDimFrom = invDim;
            line.InventDimTo = invDim;
            //3
            header.Description = "demo data for transfer journal";
            List<InvJournalLine> lst = new List<InvJournalLine>();
            lst.Add(line);
            header.LstJournalLine = lst;
            return Utility.XmlSerializeToString(header);
        }

        private string Test4PurchPackingSlip_Export()
        {
            PurchTable header = new PurchTable();
            PurchLine line = new PurchLine();
            InventDim invDim = new InventDim();
            //1
            invDim.InventLocationId = "CHR";
            invDim.inventBatchId = "";
            invDim.inventSerialId = "";
            invDim.wMsLocationId = "D11101";
            invDim.wMSPalletId = "";
            //2
            line.ItemId = "RC061079";
            line.Qty = 2;
            line.InventDim = invDim;
            line.LineNum = 1;
            //3
            header.PurchId = "PP014800";
            header.Receive = true;
            List<PurchLine> lst = new List<PurchLine>();
            lst.Add(line);
            header.LstPurchLine = lst;
            return Utility.XmlSerializeToString(header);
        }

        private string Test4SalesPackingSlip_Export()
        {
            SalesShipmentTable header = new SalesShipmentTable();
            SalesShipmentLine line = new SalesShipmentLine();
            InventDim invDim = new InventDim();
            //1
            invDim.InventLocationId = "CHR";
            invDim.inventBatchId = "S1064928";
            invDim.inventSerialId = "JRN-126321";
            invDim.wMsLocationId = "CTR1-IN";
            invDim.wMSPalletId = "M1000001";
            //2
            line.ItemId = "010160500";
            line.Qty = 123.45m;
            line.InventDim = invDim;
            //3
            header.ShipmentId = "SHP-011774";
            List<SalesShipmentLine> lst = new List<SalesShipmentLine>();
            lst.Add(line);
            header.LstShipmentLine = lst;
            return Utility.XmlSerializeToString(header);
        }

        private string Test4SalesCreditNote_Export()
        {
            SalesTable header = new SalesTable();
            SalesLine line = new SalesLine();
            InventDim invDim = new InventDim();
            //1
            invDim.InventLocationId = "CHR";
            invDim.inventBatchId = "S1064928";
            invDim.inventSerialId = "JRN-126321";
            invDim.wMsLocationId = "CTR1-IN";
            invDim.wMSPalletId = "M1000001";
            //2
            line.ItemId = "010160500";
            line.Qty = 123.45m;
            line.InventDim = invDim;
            //3
            header.SalesId = "SO009430";
            List<SalesLine> lst = new List<SalesLine>();
            lst.Add(line);
            header.LstSalesLine = lst;
            return Utility.XmlSerializeToString(header);
        }
        #endregion

    }
}
