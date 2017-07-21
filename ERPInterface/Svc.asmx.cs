﻿using AxaptaCOMConnector;
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
        public string PurchCreditNote(string input)
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
                    axRecord.field["PurchReceivedNow"] = pt.Receive ? pl.Qty : pl.Qty * -1;
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

        [WebMethod]
        public string PurchPackingSlip(string input)
        {
            string ret = "";
            Axapta ax = new Axapta();
            try
            {
                // 1.0 get parameters
                PurchTable pt = (PurchTable)Utility.XmlDeserializeFromString(input, typeof(PurchTable));
                if (pt.Receive)
                {
                    #region Log on 
                    ax.Logon();
                    // 2.0 update purchline registered
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
                        ax.CallStaticClassMethod("WMS_Utility", "Svc_PurchPackingSlip_Register"
                            , pt.PurchId, pl.LineNum, pl.Qty, inventDimId);
                    }
                    // 3.0 create packingslip parm
                    // DocumentStatus::PackingSlip = 5
                    IAxaptaObject purchFormLetter =
                        ax.CallStaticClassMethod("PurchFormLetter", "construct", 5) as IAxaptaObject;
                    IAxaptaRecord purchTable = ax.CreateRecord("PurchTable");
                    purchTable.ExecuteStmt(string.Format(
                            "select forupdate * from %1 where %1.PurchId =='{0}'"
                            , pt.PurchId));
                    // 4.0 post packingslip               
                    //PurchUpdate::Recorded = 2
                    purchFormLetter.Call("update", purchTable, DateTime.Now.ToLongTimeString(), DateTime.Now, 2);
                    #endregion
                }
                else
                {
                    ret = PurchCreditNote(input);
                }
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
            return Utility.XmlResult(ret);
        }

        [WebMethod]
        public string InvTransferJournal(string input)
        {
            string ret = "";
            Axapta ax = new Axapta();
            try
            {
                InvJournalTable jt = 
                    (InvJournalTable)Utility.XmlDeserializeFromString(input, typeof(InvJournalTable));
                ax.Logon();
                //1.0 create header
                IAxaptaRecord inventJournalName = ax.CallStaticRecordMethod("InventJournalName","find", "ITrf");
                IAxaptaRecord inventJournalTable = ax.CreateRecord("InventJournalTable");
                inventJournalTable.Call("initValue");
                inventJournalTable.Call("initFromInventJournalName", inventJournalName);
                inventJournalTable.field["Description"] = jt.Description;
                inventJournalTable.Call("insert");
                //2.0 add lines
                foreach (InvJournalLine jl in jt.LstJournalLine)
                {
                    #region get invent dim
                    InventDim dimF = jl.InventDimFrom;
                    IAxaptaRecord inventDimF = ax.CreateRecord("InventDim");
                    inventDimF.field["InventLocationId"] = dimF.InventLocationId;
                    inventDimF.field["inventBatchId"] = dimF.inventBatchId;
                    inventDimF.field["wMsLocationId"] = dimF.wMsLocationId;
                    inventDimF.field["wMSPalletId"] = dimF.wMSPalletId;
                    inventDimF.field["inventSerialId"] = dimF.inventSerialId;
                    inventDimF = ax.CallStaticRecordMethod("InventDim", "findOrCreate", inventDimF);
                    InventDim dimT = jl.InventDimTo;
                    IAxaptaRecord inventDimT = ax.CreateRecord("InventDim");
                    inventDimT.field["InventLocationId"] = dimT.InventLocationId;
                    inventDimT.field["inventBatchId"] = dimT.inventBatchId;
                    inventDimT.field["wMsLocationId"] = dimT.wMsLocationId;
                    inventDimT.field["wMSPalletId"] = dimT.wMSPalletId;
                    inventDimT.field["inventSerialId"] = dimT.inventSerialId;
                    inventDimT = ax.CallStaticRecordMethod("InventDim", "findOrCreate", inventDimT);
                    #endregion

                    ax.CallStaticClassMethod(
                        "WMS_Utility"
                        , "Svc_InventoryJournal_AddLine"
                        , inventJournalTable.field["JournalId"]
                        , jl.ItemId, jl.Qty
                        , inventDimF.field["inventDimId"]
                        , inventDimT.field["inventDimId"]);
                }
                //3.0 post journal
                ax.CallStaticClassMethod(
                        "WMS_Utility"
                        , "Svc_PostInventoryJournal"
                        , inventJournalTable.field["JournalId"]);
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
            return Utility.XmlResult(ret);
        }
        
        [WebMethod]
        public string InvMovementJournal(string input)
        {
            string ret = "";
            Axapta ax = new Axapta();
            try
            {
                InvMovementTable jt =
                  (InvMovementTable)Utility.XmlDeserializeFromString(input, typeof(InvMovementTable));
                ax.Logon();
                //1.0 create header
                IAxaptaRecord inventJournalName = ax.CallStaticRecordMethod("InventJournalName", "find", jt.MovementType);
                IAxaptaRecord inventJournalTable = ax.CreateRecord("InventJournalTable");
                inventJournalTable.Call("initValue");
                inventJournalTable.Call("initFromInventJournalName", inventJournalName);
                inventJournalTable.field["Description"] = jt.Description;
                inventJournalTable.Call("insert");
                //2.0 add lines
                foreach (InvMovementLine jl in jt.ListMovementLine)
                {
                    #region get invent dim
                    InventDim dim = jl.InventDim;
                    IAxaptaRecord inventDim = ax.CreateRecord("InventDim");
                    inventDim.field["InventLocationId"] = dim.InventLocationId;
                    inventDim.field["inventBatchId"] = dim.inventBatchId;
                    inventDim.field["wMsLocationId"] = dim.wMsLocationId;
                    inventDim.field["wMSPalletId"] = dim.wMSPalletId;
                    inventDim.field["inventSerialId"] = dim.inventSerialId;
                    inventDim = ax.CallStaticRecordMethod("InventDim", "findOrCreate", inventDim);
                    #endregion
                    ax.CallStaticClassMethod(
                        "WMS_Utility"
                        , "Svc_MovementJournal_AddLine"
                        , inventJournalTable.field["JournalId"]
                        , jl.ItemId, jl.Qty
                        , inventDim.field["inventDimId"]
                        , jl.OffsetAccount);
                }
                //3.0 post journal
                ax.CallStaticClassMethod(
                        "WMS_Utility"
                        , "Svc_PostInventoryJournal"
                        , inventJournalTable.field["JournalId"]);
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
            return Utility.XmlResult(ret);
        }
        
        [WebMethod]
        public string SalesPackingSlip(string input)
        {
            string ret = "";
            Axapta ax = new Axapta();
            try
            {
                SalesShipmentTable st =
                    (SalesShipmentTable)Utility.XmlDeserializeFromString(input, typeof(SalesShipmentTable));
                ax.Logon();
                //1.0 WMSOrderTransReservation
                foreach (SalesShipmentLine sl in st.LstShipmentLine)
                {
                    InventDim dim = sl.InventDim;
                    IAxaptaRecord inventDim = ax.CreateRecord("InventDim");
                    inventDim.field["InventLocationId"] = dim.InventLocationId;
                    inventDim.field["inventBatchId"] = dim.inventBatchId;
                    inventDim.field["wMsLocationId"] = dim.wMsLocationId;
                    inventDim.field["wMSPalletId"] = dim.wMSPalletId;
                    inventDim.field["inventSerialId"] = dim.inventSerialId;
                    inventDim = ax.CallStaticRecordMethod("InventDim", "findOrCreate", inventDim);
                    IAxaptaRecord WMSOrderTrans = ax.CreateRecord("WMSOrderTrans");
                    WMSOrderTrans.ExecuteStmt(string.Format(
                        "select * from %1 where %1.shipmentId =='{0}' && %1.orderId == '{1}'"
                        , st.ShipmentId, sl.InvOutPutOrder));
                    ax.CallStaticClassMethod("WMS_Utility"
                            , "Svc_WMSOrderTransReservation"
                            , WMSOrderTrans.field["RecId"], inventDim.field["inventDimId"]);
                }
                //2.0 WMSShipmentFinished & SalesPackingSlip
                ax.CallStaticClassMethod("WMS_Utility", "Svc_WMSShipmentFinished", st.ShipmentId);
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
            return Utility.XmlResult(ret);
        }
        
        [WebMethod]
        public string SalesCreditNote(string input)
        {
            string ret = "";
            Axapta ax = new Axapta();
            try
            {
                SalesTable st = 
                    (SalesTable)Utility.XmlDeserializeFromString(input, typeof(SalesTable));
                ax.Logon();
                ax.TTSBegin();
                foreach (SalesLine sl in st.LstSalesLine)
                {
                    InventDim dim = sl.InventDim;
                    IAxaptaRecord inventDim = ax.CreateRecord("InventDim");
                    inventDim.field["InventLocationId"] = dim.InventLocationId;
                    inventDim.field["inventBatchId"] = dim.inventBatchId;
                    inventDim.field["wMsLocationId"] = dim.wMsLocationId;
                    inventDim.field["wMSPalletId"] = dim.wMSPalletId;
                    inventDim.field["inventSerialId"] = dim.inventSerialId;
                    inventDim = ax.CallStaticRecordMethod("InventDim", "findOrCreate", inventDim);
                    IAxaptaRecord SalesLine = ax.CreateRecord("SalesLine");
                    SalesLine.ExecuteStmt(string.Format(
                        "select forupdate * from %1 where %1.SalesId =='{0}' && %1.LineNum == {1}"
                        , st.SalesId, sl.LineNum));
                    SalesLine.field["SalesDeliverNow"] = -1 * sl.Qty;
                    SalesLine.field["inventDimId"] = inventDim.field["inventDimId"];
                    SalesLine.Call("setInventDeliverNow");
                    SalesLine.DoUpdate();
                }
                ax.TTSCommit();
                // SalesUpdate::DeliverNow = 0
                ax.CallStaticClassMethod("WMS_Utility", "Svc_SalesPackingSlip",st.SalesId,0);
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
            return Utility.XmlResult(ret);
        }


        [WebMethod]
        public string InvCountJournal(string input)
        {
            string ret = "";
            Axapta ax = new Axapta();
            try
            {
                InvCountTable jt =
                  (InvCountTable)Utility.XmlDeserializeFromString(input, typeof(InvCountTable));
                ax.Logon();
                //1.0 create header
                IAxaptaRecord inventJournalName = ax.CallStaticRecordMethod("InventJournalName", "find", "ICnt");
                IAxaptaRecord inventJournalTable = ax.CreateRecord("InventJournalTable");
                inventJournalTable.Call("initValue");
                inventJournalTable.Call("initFromInventJournalName", inventJournalName);
                inventJournalTable.field["Description"] = jt.Description;
                inventJournalTable.Call("insert");
                //2.0 add lines
                foreach (InvCountLine jl in jt.LstCountLine)
                {
                    #region get invent dim
                    InventDim dim = jl.InventDim;
                    IAxaptaRecord inventDim = ax.CreateRecord("InventDim");
                    inventDim.field["InventLocationId"] = dim.InventLocationId;
                    inventDim.field["inventBatchId"] = dim.inventBatchId;
                    inventDim.field["wMsLocationId"] = dim.wMsLocationId;
                    inventDim.field["wMSPalletId"] = dim.wMSPalletId;
                    inventDim.field["inventSerialId"] = dim.inventSerialId;
                    inventDim = ax.CallStaticRecordMethod("InventDim", "findOrCreate", inventDim);
                    #endregion
                    ax.CallStaticClassMethod(
                        "WMS_Utility"
                        , "Svc_CountingJournal_AddLine"
                        , inventJournalTable.field["JournalId"]
                        , jl.ItemId, jl.Qty
                        , inventDim.field["inventDimId"]
                        );
                }
                //3.0 post journal
                ax.CallStaticClassMethod(
                        "WMS_Utility"
                        , "Svc_PostInventoryJournal"
                        , inventJournalTable.field["JournalId"]);
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
            return Utility.XmlResult(ret);
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
            //ret = PurchPackingSlip(Test4PurchPackingSlip_Export());
            //Test SalesPackingSlik
            //ret = SalesPackingSlip(Test4SalesPackingSlip_Export());
            //ret = SalesCreditNote(Test4SalesCreditNote_Export());
            //Test InvTransferJournal
            //ret = InvTransferJournal(Test4InvTransferJournal_Export());
            //Test InvMovementJournal
            //ret = InvMovementJournal(Test4InvMovementJournal_Export());
            //Test InvCountJournal
            //ret = InvCountJournal(Test4InvCountJournal_Export());
            ret = Utility.XmlResult(input);
            return ret;
        }

        private string Test4InvCountJournal_Export()
        {
            InvCountTable header = new InvCountTable();
            InvCountLine line = new InvCountLine();
            InventDim invDim = new InventDim();
            //1
            invDim.InventLocationId = "CHR";
            invDim.inventBatchId = "";
            invDim.inventSerialId = "";
            invDim.wMsLocationId = "DOCK";
            invDim.wMSPalletId = "";
            //2
            line.ItemId = "RC010009";
            line.Qty = 1m;
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
            invDim.inventBatchId = "";
            invDim.inventSerialId = "";
            invDim.wMsLocationId = "";
            invDim.wMSPalletId = "";
            //2
            line.ItemId = "M000658";
            line.Qty = 4m;
            line.InventDim = invDim;
            line.OffsetAccount = "64030";
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
            //1
            InventDim invDimF = new InventDim();
            invDimF.InventLocationId = "CHR";
            invDimF.inventBatchId = "";
            invDimF.inventSerialId = "";
            invDimF.wMsLocationId = "D16102";
            invDimF.wMSPalletId = "";
            //2
            line.ItemId = "RC020027";
            line.Qty = 1;
            line.InventDimFrom = invDimF;
            InventDim invDimT = new InventDim();
            invDimT.InventLocationId = "CHR";
            invDimT.inventBatchId = "";
            invDimT.inventSerialId = "";
            invDimT.wMsLocationId = "DOCK";
            invDimT.wMSPalletId = "";
            line.InventDimTo = invDimT;
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
            invDim.InventLocationId = "MRO-P";
            invDim.inventBatchId = "";
            invDim.inventSerialId = "";
            invDim.wMsLocationId = "MRO-P2";
            invDim.wMSPalletId = "";
            //2
            line.ItemId = "MPM00066";
            line.Qty = 2;
            line.InventDim = invDim;
            line.LineNum = 1;
            //3
            header.PurchId = "PP014807";
            header.Receive = false;
            List<PurchLine> lst = new List<PurchLine>();
            lst.Add(line);
            InventDim invDim2 = new InventDim();
            invDim2.InventLocationId = "MRO-P";
            invDim2.inventBatchId = "";
            invDim2.inventSerialId = "";
            invDim2.wMsLocationId = "MRO-P1";
            invDim2.wMSPalletId = "";
            PurchLine line2 = new PurchLine();
            line2.ItemId = "MPM00066";
            line2.Qty = 2;
            line2.LineNum = 1;
            line2.InventDim = invDim2;
            lst.Add(line2);
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
            invDim.inventBatchId = "";
            invDim.inventSerialId = "CTN3109 Caprice DX9";
            invDim.wMsLocationId = "G05139";
            invDim.wMSPalletId = "";
            //2
            line.ItemId = "040000500";
            line.Qty = 2;
            line.InventDim = invDim;
            line.InvOutPutOrder = "INO-030851";
            //3
            header.ShipmentId = "SHP-014304";
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
            invDim.inventBatchId = "";
            invDim.inventSerialId = "CTN3109 Caprice DX9";
            invDim.wMsLocationId = "G05139";
            invDim.wMSPalletId = "";
            //2
            line.ItemId = "040000500";
            line.Qty = 1;
            line.InventDim = invDim;
            line.LineNum = "1";
            //3
            header.SalesId = "SO011158";
            List<SalesLine> lst = new List<SalesLine>();
            lst.Add(line);
            header.LstSalesLine = lst;
            return Utility.XmlSerializeToString(header);
        }
        #endregion

    }
}
