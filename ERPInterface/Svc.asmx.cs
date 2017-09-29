using AxaptaCOMConnector;
using ERPInterface.Entities;
using ERPInterface.Entities.SalesPackingSlip;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            Log.Info("PurchPackingSlip");
            Log.Info(input);
            string ret = "";
            string purchId = "";
            string packingslipId = "";
            string inProcess = "";
            string shipmentId = "";
            Axapta ax = new Axapta();
            try
            {
                // 1.0 get parameters
                PurchTable pt = (PurchTable)Utility.XmlDeserializeFromString(input, typeof(PurchTable));
                purchId = pt.PurchId;
                shipmentId = pt.PackingSlipId;
                if (shipmentId == "" || shipmentId == null)
                {
                    ret = "PackingSlipId con't be null.";
                    return Utility.XmlResult(ret);
                }
                if (pt.Receive)
                {
                    #region Log on 
                    ax.Logon();
                    //1.1 Check inprocess
                    inProcess = ax.CallStaticClassMethod("WMS_Utility", "Svc_WMSStatusCheck", shipmentId);
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
                        //inventDim = ax.CallStaticRecordMethod("InventDim", "findOrCreate", inventDim);
                        inventDim = ax.CallStaticClassMethod("WMS_Utility", "Svc_InventDim", pl.ItemId, inventDim);
                        string inventDimId = inventDim.field["inventDimId"];                         
                        ax.CallStaticClassMethod("WMS_Utility", "Svc_InvBatchPallet_FindOrCreate"
                            ,pl.ItemId, inventDimId);
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
                    DateTime sessionDate = new DateTime(int.Parse(pt.DateYMD.Substring(0, 4))
                        , int.Parse(pt.DateYMD.Substring(4, 2))
                        , int.Parse(pt.DateYMD.Substring(6, 2)));
                    //PurchUpdate::Recorded = 2
                    purchFormLetter.Call("update", purchTable, pt.PackingSlipId, sessionDate, 2);
                    IAxaptaRecord purchJour = purchFormLetter.Call("journal");
                    packingslipId = purchJour.field["PackingSlipId"]; 
                    if(packingslipId == "")
                    {
                        throw new Exception("Post Purchase Packing Slip failed!");
                    }
                    #endregion
                }
                else
                {
                    inProcess = "inProcess";
                    ret = PurchCreditNote(input);
                }
            }
            catch (Exception ex)
            {
                ret = ex.Message;
                Log.Error(ex.Message);
                Log.Error(ex.StackTrace);
                Log.Error(input);
                //roll back
                ax.CallStaticClassMethod("WMS_Utility", "Svc_PurchPackingSlip_UnRegister", purchId);
            }
            finally
            {
                if (inProcess == "")
                {
                    ax.CallStaticClassMethod("WMS_Utility", "Svc_WMSStatusRemove", shipmentId);
                }
                
                ax.Logoff();                
            }
            return Utility.XmlResult(ret, packingslipId);
        }

        [WebMethod]
        public string PurchCreditNoteByItem(string input)
        {
            Log.Info("PurchCreditNoteByItem");
            Log.Info(input);
            string ret = "";
            string packingslipId = "";
            Axapta ax = new Axapta();
            try
            {
                // 1.0 get parameters
                PurchCreditNote pt = (PurchCreditNote)Utility.XmlDeserializeFromString(input, typeof(PurchCreditNote));

                ax.Logon();

                // 2.0 update purchline
                string oriLineId = ax.CallStaticClassMethod("WMS_Utility", "Svc_PurchCreditFetchLine"
                                                            , pt.PurchId, pt.ItemId,pt.InventDim.inventSerialId);
                InventDim dim = pt.InventDim;
                IAxaptaRecord inventDim = ax.CreateRecord("InventDim");
                inventDim.field["InventLocationId"] = dim.InventLocationId;
                inventDim.field["inventBatchId"] = dim.inventBatchId;
                inventDim.field["wMsLocationId"] = dim.wMsLocationId;
                inventDim.field["wMSPalletId"] = dim.wMSPalletId;
                inventDim.field["inventSerialId"] = dim.inventSerialId;
                //inventDim = ax.CallStaticRecordMethod("InventDim", "findOrCreate", inventDim);
                inventDim = ax.CallStaticClassMethod("WMS_Utility", "Svc_InventDim", pt.ItemId, inventDim);
                string inventDimId = inventDim.field["inventDimId"];
                string purchId = ax.CallStaticClassMethod("WMS_Utility", "Svc_PurchCreditCreatePO"
                                                            , oriLineId, pt.Qty, inventDimId);
                // 3.0 create packingslip parm
                // DocumentStatus::PackingSlip = 5
                IAxaptaObject purchFormLetter =
                    ax.CallStaticClassMethod("PurchFormLetter", "construct", 5) as IAxaptaObject;
                IAxaptaRecord purchTable = ax.CreateRecord("PurchTable");
                purchTable.ExecuteStmt(string.Format(
                        "select forupdate * from %1 where %1.PurchId =='{0}'"
                        , purchId));
                try
                {
                    // 4.0 post packingslip   
                    DateTime sessionDate = new DateTime(int.Parse(pt.DateYMD.Substring(0, 4))
                        , int.Parse(pt.DateYMD.Substring(4, 2))
                        , int.Parse(pt.DateYMD.Substring(6, 2)));
                    //PurchUpdate::ReceiveNow = 0
                    purchFormLetter.Call("update", purchTable, pt.PackingSlipId, sessionDate, 0);
                    IAxaptaRecord purchJour = purchFormLetter.Call("journal");
                    packingslipId = purchJour.field["PackingSlipId"];
                    if (packingslipId == "")
                    {
                        throw new Exception("Post Purchase Packing Slip failed!");
                    }
                }
                catch (Exception ex)
                {
                    ax.CallStaticClassMethod("WMS_Utility", "Svc_DeletePurchCredit", purchId);
                    throw ex;
                }
            }
            catch (Exception ex)
            {
                ret = ex.Message;
                Log.Error(ex.Message);
                Log.Error(ex.StackTrace);
                Log.Error(input);
            }
            finally
            {
                ax.Logoff();
            }
            return Utility.XmlResult(ret, packingslipId);
        }

        public string PurchCreditNote(string input)
        {
            string ret = "";
            string purchId = "";
            Axapta ax = new Axapta();
            try
            {
                // 1.0 get parameters
                PurchTable pt = (PurchTable)Utility.XmlDeserializeFromString(input, typeof(PurchTable));
                purchId = pt.PurchId;
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
                    //IAxaptaRecord axRecord = ax.CreateRecord("PurchLine");

                    //string stmt = string.Format(
                    //    "select forupdate * from %1 where %1.PurchId =='{0}' && %1.LineNum == {1}"
                    //    , pt.PurchId, pl.LineNum);
                    //axRecord.ExecuteStmt(stmt);
                    //axRecord.field["PurchReceivedNow"] = pl.Qty;
                    //axRecord.field["inventDimId"] = inventDimId;
                    //axRecord.Call("setInventReceivedNow");
                    //axRecord.DoUpdate();
                    ax.CallStaticClassMethod("WMS_Utility", "Svc_PurchLineReservation", pt.PurchId, pl.LineNum, inventDimId,pl.Qty);
                }
                ax.TTSCommit();
                // 3.0 create packingslip parm
                // DocumentStatus::PackingSlip = 5
                //IAxaptaObject purchFormLetter =
                //    ax.CallStaticClassMethod("PurchFormLetter", "construct", 5) as IAxaptaObject;
                //IAxaptaRecord purchTable = ax.CreateRecord("PurchTable");
                //purchTable.ExecuteStmt(string.Format(
                //        "select forupdate * from %1 where %1.PurchId =='{0}'"
                //        , pt.PurchId));
                //// 4.0 post packingslip    
                //DateTime sessionDate = new DateTime(int.Parse(pt.DateYMD.Substring(0, 4))
                //        , int.Parse(pt.DateYMD.Substring(4, 2))
                //        , int.Parse(pt.DateYMD.Substring(6, 2)));
                ////PurchUpdate::ReceiveNow = 0
                //purchFormLetter.Call("update", purchTable, pt.PackingSlipId, sessionDate, 0);
                //IAxaptaRecord purchJour = purchFormLetter.Call("journal");
                //string packingslipId = purchJour.field["PackingSlipId"];
                DateTime sessionDate = new DateTime(int.Parse(pt.DateYMD.Substring(0, 4))
                    , int.Parse(pt.DateYMD.Substring(4, 2))
                    , int.Parse(pt.DateYMD.Substring(6, 2)));
                string packingslipId = ax.CallStaticClassMethod("WMS_Utility", "Svc_PostPurchPackingSlip",pt.PurchId, "ReceiveNow",pt.PackingSlipId, sessionDate);
                if (packingslipId != pt.PackingSlipId)
                {
                    throw new Exception(packingslipId);
                }
                #endregion
            }
            catch (Exception ex)
            {
                ax.TTSAbort();
                ax.CallStaticClassMethod("WMS_Utility", "Svc_PurchUnreservation", purchId);
                ret = ex.Message;
                Log.Error(ex.Message);
                Log.Error(ex.StackTrace);
                Log.Error(input);
            }
            finally
            {
                ax.Logoff();
            }
            return ret;
        }

        [WebMethod]
        public string InvTransferJournal(string input)
        {
            Log.Info("InvTransferJournal");
            Log.Info(input);
            string ret = "";
            string jourId = "";
            Axapta ax = new Axapta();
            try
            {
                InvJournalTable jt = 
                    (InvJournalTable)Utility.XmlDeserializeFromString(input, typeof(InvJournalTable));
                DateTime sessionDate = new DateTime(int.Parse(jt.DateYMD.Substring(0, 4))
                    , int.Parse(jt.DateYMD.Substring(4, 2))
                    , int.Parse(jt.DateYMD.Substring(6, 2)));
                ax.Logon();
                //1.0 create header
                IAxaptaRecord inventJournalName = ax.CallStaticRecordMethod("InventJournalName","find", "ITrf");
                IAxaptaRecord inventJournalTable = ax.CreateRecord("InventJournalTable");
                inventJournalTable.Call("initValue");
                inventJournalTable.Call("initFromInventJournalName", inventJournalName);
                inventJournalTable.field["Description"] = jt.Description;
                inventJournalTable.Call("insert");
                jourId = inventJournalTable.field["JournalId"]; 
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
                    ax.CallStaticClassMethod("WMS_Utility", "Svc_InvBatchPallet_FindOrCreate"
                        ,jl.ItemId
                        , inventDimF.field["inventDimId"]);
                    ax.CallStaticClassMethod("WMS_Utility", "Svc_InvBatchPallet_FindOrCreate"
                        , jl.ItemId, inventDimF.field["inventDimId"]);
                    InventDim dimT = jl.InventDimTo;
                    IAxaptaRecord inventDimT = ax.CreateRecord("InventDim");
                    inventDimT.field["InventLocationId"] = dimT.InventLocationId;
                    inventDimT.field["inventBatchId"] = dimT.inventBatchId;
                    inventDimT.field["wMsLocationId"] = dimT.wMsLocationId;
                    inventDimT.field["wMSPalletId"] = dimT.wMSPalletId;
                    inventDimT.field["inventSerialId"] = dimT.inventSerialId;
                    inventDimT = ax.CallStaticRecordMethod("InventDim", "findOrCreate", inventDimT);
                    ax.CallStaticClassMethod("WMS_Utility", "Svc_InvBatchPallet_FindOrCreate"
                        , jl.ItemId
                        , inventDimT.field["inventDimId"]);
                    ax.CallStaticClassMethod("WMS_Utility", "Svc_InvBatchPallet_FindOrCreate"
                       , jl.ItemId, inventDimT.field["inventDimId"]);
                    #endregion

                    ax.CallStaticClassMethod(
                        "WMS_Utility"
                        , "Svc_InventoryJournal_AddLine"
                        , inventJournalTable.field["JournalId"]
                        , jl.ItemId, jl.Qty
                        , inventDimF.field["inventDimId"]
                        , inventDimT.field["inventDimId"]
                        , sessionDate);
                }
                try
                {
                    //3.0 post journal
                    ax.CallStaticClassMethod(
                            "WMS_Utility"
                            , "Svc_PostInventoryJournal"
                            , inventJournalTable.field["JournalId"]);
                }
                catch (Exception ex)
                {

                    ax.CallStaticClassMethod("WMS_Utility", "Svc_DeleteInventoryJournal", inventJournalTable.field["JournalId"]);
                    throw ex;
                }
            }
            catch (Exception ex)
            {
                ret = ex.Message;

                Log.Error(ex.Message);
                Log.Error(ex.StackTrace);
                Log.Error(input);
            }
            finally
            {
                ax.Logoff();
            }
            return Utility.XmlResult(ret, jourId);
        }
        
        [WebMethod]
        public string InvMovementJournal(string input)
        {
            Log.Info("InvMovementJournal");
            Log.Info(input);
            string ret = "";
            string jourId = "";
            Axapta ax = new Axapta();
            try
            {
                InvMovementTable jt =
                  (InvMovementTable)Utility.XmlDeserializeFromString(input, typeof(InvMovementTable));
                DateTime sessionDate = new DateTime(int.Parse(jt.DateYMD.Substring(0, 4))
                    , int.Parse(jt.DateYMD.Substring(4, 2))
                    , int.Parse(jt.DateYMD.Substring(6, 2)));
                ax.Logon();
                //1.0 create header
                IAxaptaRecord inventJournalName = ax.CallStaticRecordMethod("InventJournalName", "find", jt.MovementType);
                IAxaptaRecord inventJournalTable = ax.CreateRecord("InventJournalTable");                
                inventJournalTable.Call("initValue");
                inventJournalTable.Call("initFromInventJournalName", inventJournalName);
                inventJournalTable.field["Description"] = jt.Description;
                inventJournalTable.Call("insert");
                jourId = inventJournalTable.field["JournalId"];
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
                    ax.CallStaticClassMethod("WMS_Utility", "Svc_InvBatchPallet_FindOrCreate"
                       , jl.ItemId, inventDim.field["inventDimId"]);
                    #endregion
                    IAxaptaContainer container = ax.CreateContainer();
                    container.Append(jl.OffsetAccount, jl.Department);
                    ax.CallStaticClassMethod(
                        "WMS_Utility"
                        , "Svc_MovementJournal_AddLine"
                        , inventJournalTable.field["JournalId"]
                        , jl.ItemId, jl.Qty
                        , inventDim.field["inventDimId"]
                        , container
                        , sessionDate);
                }

                try
                {
                    //3.0 post journal
                    ax.CallStaticClassMethod(
                            "WMS_Utility"
                            , "Svc_PostInventoryJournal"
                            , inventJournalTable.field["JournalId"]);
                }
                catch (Exception ex)
                {

                    ax.CallStaticClassMethod("WMS_Utility", "Svc_DeleteInventoryJournal", inventJournalTable.field["JournalId"]);
                    throw ex;
                }
            }
            catch (Exception ex)
            {
                ret = ex.Message;
                Log.Error(ex.Message);
                Log.Error(ex.StackTrace);
                Log.Error(input);
            }
            finally
            {
                ax.Logoff();
            }
            return Utility.XmlResult(ret, jourId);
        }

        [WebMethod]
        public string SalesPackingSlip(string input)
        {
            Log.Info("SalesPackingSlip");
            Log.Info(input);
            string ret = "";
            Axapta ax = new Axapta();
            string strSO = "";
            string packingslipId = "";
            string shipmentId = "";
            string inProcess = "";
            SalesShipmentTable st =
                (SalesShipmentTable)Utility.XmlDeserializeFromString(input, typeof(SalesShipmentTable));
            DateTime sessionDate = new DateTime(int.Parse(st.DateYMD.Substring(0, 4))
                , int.Parse(st.DateYMD.Substring(4, 2))
                , int.Parse(st.DateYMD.Substring(6, 2)));
            List<string> lstSO = st.LstShipmentLine.Select(p => p.SalesId).ToList();
            lstSO = lstSO.Distinct().ToList();
            if (st.ShipmentId == "" || st.ShipmentId == null)
            {
                ret = "ShipmentId con't be null.";
                return Utility.XmlResult(ret);
            }
            shipmentId = st.ShipmentId;
            try
            {
                ax.Logon();
                //1.0 Sales order Unreservation & clean delivery now
                //1.1 Check inprocess
                inProcess = ax.CallStaticClassMethod("WMS_Utility", "Svc_WMSStatusCheck", shipmentId);
                if (inProcess != "")
                {
                    return Utility.XmlResult(inProcess);
                }
                foreach (string item in lstSO)
                {
                    strSO = strSO + "," + item;
                    ax.CallStaticClassMethod("WMS_Utility", "Svc_SalesUnreservation", item);
                }
                //2.0 Reservation
                foreach (SalesShipmentLine sl in st.LstShipmentLine)
                {
                    string inventDimId = GetInventDimId(sl.InventDim, ax);
                    IAxaptaRecord SalesLine = ax.CreateRecord("SalesLine");
                    ax.CallStaticClassMethod("WMS_Utility", "Svc_SalesLineReservation", sl.LineId, inventDimId, sl.Qty);
                }
                //3.0 SalesPackingSlip
                packingslipId = ax.CallStaticClassMethod("WMS_Utility", "Svc_SalesPackingSlipByMultiSO", strSO, st.ShipmentId, sessionDate);
                if (packingslipId == "")
                {
                    throw new Exception("Post Sales Packing Slip failed!");
                }
            }
            catch (Exception ex)
            {
                ret = ex.Message;
                Log.Error(ex.Message);
                Log.Error(ex.StackTrace);
                Log.Error(input);
                //1.0 Sales order Unreservation & clean delivery now
                foreach (var item in lstSO)
                {
                    strSO = strSO + "," + item.ToString();
                    ax.CallStaticClassMethod("WMS_Utility"
                               , "Svc_SalesUnreservation"
                               , item.ToString());
                }
            }
            finally
            {
                if (inProcess == "")
                {
                    ax.CallStaticClassMethod("WMS_Utility", "Svc_WMSStatusRemove", shipmentId);
                }
                ax.Logoff();
            }
            return Utility.XmlResult(ret, packingslipId);
        }

        [WebMethod]
        public string SalesCreditNote(string input)
        {
            Log.Info("SalesCreditNote");
            Log.Info(input);
            string ret = "";
            string packingslipId = "";
            string salesId = "";
            Axapta ax = new Axapta();
            try
            {
                SalesTable st = 
                    (SalesTable)Utility.XmlDeserializeFromString(input, typeof(SalesTable));
                salesId = st.SalesId;
                DateTime sessionDate = new DateTime(int.Parse(st.DateYMD.Substring(0, 4))
                    , int.Parse(st.DateYMD.Substring(4, 2))
                    , int.Parse(st.DateYMD.Substring(6, 2)));
                ax.Logon();
                ax.CallStaticClassMethod("WMS_Utility", "Svc_SalesCredit_UnRegister", st.SalesId);
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
                    ax.CallStaticClassMethod("WMS_Utility", "Svc_InvBatchPallet_FindOrCreate"
                       , sl.ItemId, inventDim.field["inventDimId"]);
                    ax.CallStaticClassMethod("WMS_Utility", "Svc_SalesCredit_Register",st.SalesId,sl.LineNum,sl.Qty, inventDim.field["inventDimId"]); 
                    IAxaptaRecord SalesLine = ax.CreateRecord("SalesLine");
                    SalesLine.ExecuteStmt(string.Format(
                        "select forupdate * from %1 where %1.SalesId =='{0}' && %1.LineNum == {1}"
                        , st.SalesId, sl.LineNum));
                    SalesLine.field["SalesDeliverNow"] = Convert.ToDouble(-1 * sl.Qty) + (double)SalesLine.field["SalesDeliverNow"];
                    SalesLine.field["inventDimId"] = inventDim.field["inventDimId"];
                    SalesLine.Call("setInventDeliverNow");
                    SalesLine.DoUpdate();
                }
                ax.TTSCommit();
                // SalesUpdate::DeliverNow = 0
                packingslipId = ax.CallStaticClassMethod("WMS_Utility", "Svc_SalesPackingSlip",st.SalesId,0,st.ShipmentId, sessionDate);       
            }
            catch (Exception ex)
            {
                ax.TTSAbort();
                ax.CallStaticClassMethod("WMS_Utility", "Svc_SalesCredit_UnRegister", salesId);
                ret = ex.Message;
                Log.Error(ex.Message);
                Log.Error(ex.StackTrace);
                Log.Error(input);
            }
            finally
            {
                ax.Logoff();
            }
            return Utility.XmlResult(ret, packingslipId);
        }
        
        [WebMethod]
        public string InvCountJournal(string input)
        {
            Log.Info("InvCountJournal");
            Log.Info(input);
            string ret = "";
            string jourId = "";
            Axapta ax = new Axapta();
            try
            {
                InvCountTable jt =
                  (InvCountTable)Utility.XmlDeserializeFromString(input, typeof(InvCountTable));
                DateTime sessionDate = new DateTime(int.Parse(jt.DateYMD.Substring(0, 4))
                    , int.Parse(jt.DateYMD.Substring(4, 2))
                    , int.Parse(jt.DateYMD.Substring(6, 2)));
                ax.Logon();
                //1.0 create header
                IAxaptaRecord inventJournalName = ax.CallStaticRecordMethod("InventJournalName", "find", "ICnt");
                IAxaptaRecord inventJournalTable = ax.CreateRecord("InventJournalTable");
                inventJournalTable.Call("initValue");
                inventJournalTable.Call("initFromInventJournalName", inventJournalName);
                inventJournalTable.field["Description"] = jt.Description;
                inventJournalTable.Call("insert");
                jourId = inventJournalTable.field["JournalId"];
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
                    ax.CallStaticClassMethod("WMS_Utility", "Svc_InvBatchPallet_FindOrCreate"
                       , jl.ItemId, inventDim.field["inventDimId"]);
                    #endregion
                    ax.CallStaticClassMethod(
                        "WMS_Utility"
                        , "Svc_CountingJournal_AddLine"
                        , inventJournalTable.field["JournalId"]
                        , jl.ItemId, jl.Qty
                        , inventDim.field["inventDimId"]
                        , sessionDate
                        );
                }
                try
                {
                    //3.0 post journal
                    ax.CallStaticClassMethod(
                            "WMS_Utility"
                            , "Svc_PostInventoryJournal"
                            , inventJournalTable.field["JournalId"]);
                }
                catch (Exception ex)
                {
                    ax.CallStaticClassMethod("WMS_Utility", "Svc_DeleteInventoryJournal", inventJournalTable.field["JournalId"]);
                    throw ex;
                }
            }
            catch (Exception ex)
            {
                ret = ex.Message;
                Log.Error(ex.Message);
                Log.Error(ex.StackTrace);
                Log.Error(input);
            }
            finally
            {
                ax.Logoff();
            }
            return Utility.XmlResult(ret, jourId);
        }
        
        private string GetInventDimId(InventDim dim, Axapta ax)
        {
            IAxaptaRecord inventDim = ax.CreateRecord("InventDim");
            inventDim.field["InventLocationId"] = dim.InventLocationId;
            inventDim.field["inventBatchId"] = dim.inventBatchId;
            inventDim.field["wMsLocationId"] = dim.wMsLocationId;
            inventDim.field["wMSPalletId"] = dim.wMSPalletId;
            inventDim.field["inventSerialId"] = dim.inventSerialId;
            inventDim = ax.CallStaticRecordMethod("InventDim", "findOrCreate", inventDim);
            return inventDim.field["inventDimId"];
        }

        #region Test
        [WebMethod]
        public string HelloWorld(string input)
        {
            Log.Info("InvCountJournal");
            Log.Info(input);
            string ret = "Hello World";
            Axapta ax = new Axapta();
            try
            {
                ax.Logon();
                ret = ax.CallStaticClassMethod("WMS_Utility", "HelloWorld", input);
                Log.Error(ret);
            }
            catch (Exception ex)
            {
                ret = ex.Message;
                Log.Error(ex.Message);
            }
            finally
            {
                ax.Logoff();
            }
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
            //string s = @"<PurchTable><PurchId>PP014835</PurchId><LstPurchLine><PurchLine><LineNum>1.000000000000</LineNum><ItemId>RC040049</ItemId><Qty>400</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>SL201708300009</inventBatchId><wMsLocationId>D11101</wMsLocationId><wMSPalletId>SL201708300009</wMSPalletId><inventSerialId>20170830556</inventSerialId></InventDim></PurchLine></LstPurchLine><Receive>true</Receive></PurchTable>";
            //string s = @"<InvJournalTable><Description /><LstJournalLine><InvJournalLine><ItemId>301520500</ItemId><Qty>60</Qty><InventDimFrom><InventLocationId>CHR</InventLocationId><inventBatchId>S1711321</inventBatchId><wMsLocationId>G07405</wMsLocationId><wMSPalletId>M1199879</wMSPalletId><inventSerialId>C36651</inventSerialId></InventDimFrom><InventDimTo><InventLocationId>CHR</InventLocationId><inventBatchId>S1711321</inventBatchId><wMsLocationId>Buffer</wMsLocationId><wMSPalletId>M1199879</wMSPalletId><inventSerialId>C36651</inventSerialId></InventDimTo></InvJournalLine><InvJournalLine><ItemId>301520500</ItemId><Qty>60</Qty><InventDimFrom><InventLocationId>CHR</InventLocationId><inventBatchId>S1711322</inventBatchId><wMsLocationId>G07405</wMsLocationId><wMSPalletId>M1199879</wMSPalletId><inventSerialId>C36651</inventSerialId></InventDimFrom><InventDimTo><InventLocationId>CHR</InventLocationId><inventBatchId>S1711322</inventBatchId><wMsLocationId>Buffer</wMsLocationId><wMSPalletId>M1199879</wMSPalletId><inventSerialId>C36651</inventSerialId></InventDimTo></InvJournalLine><InvJournalLine><ItemId>301520500</ItemId><Qty>60</Qty><InventDimFrom><InventLocationId>CHR</InventLocationId><inventBatchId>S1711323</inventBatchId><wMsLocationId>G07405</wMsLocationId><wMSPalletId>M1199879</wMSPalletId><inventSerialId>C36651</inventSerialId></InventDimFrom><InventDimTo><InventLocationId>CHR</InventLocationId><inventBatchId>S1711323</inventBatchId><wMsLocationId>Buffer</wMsLocationId><wMSPalletId>M1199879</wMSPalletId><inventSerialId>C36651</inventSerialId></InventDimTo></InvJournalLine><InvJournalLine><ItemId>301520500</ItemId><Qty>60</Qty><InventDimFrom><InventLocationId>CHR</InventLocationId><inventBatchId>S1711324</inventBatchId><wMsLocationId>G07405</wMsLocationId><wMSPalletId>M1199879</wMSPalletId><inventSerialId>C36651</inventSerialId></InventDimFrom><InventDimTo><InventLocationId>CHR</InventLocationId><inventBatchId>S1711324</inventBatchId><wMsLocationId>Buffer</wMsLocationId><wMSPalletId>M1199879</wMSPalletId><inventSerialId>C36651</inventSerialId></InventDimTo></InvJournalLine><InvJournalLine><ItemId>301520500</ItemId><Qty>0</Qty><InventDimFrom><InventLocationId>CHR</InventLocationId><inventBatchId>S1711325</inventBatchId><wMsLocationId>G07405</wMsLocationId><wMSPalletId>M1199879</wMSPalletId><inventSerialId>C36651</inventSerialId></InventDimFrom><InventDimTo><InventLocationId>CHR</InventLocationId><inventBatchId>S1711325</inventBatchId><wMsLocationId>Buffer</wMsLocationId><wMSPalletId>M1199879</wMSPalletId><inventSerialId>C36651</inventSerialId></InventDimTo></InvJournalLine><InvJournalLine><ItemId>301520500</ItemId><Qty>0</Qty><InventDimFrom><InventLocationId>CHR</InventLocationId><inventBatchId>S1711328</inventBatchId><wMsLocationId>G07405</wMsLocationId><wMSPalletId>M1199879</wMSPalletId><inventSerialId>C36651</inventSerialId></InventDimFrom><InventDimTo><InventLocationId>CHR</InventLocationId><inventBatchId>S1711328</inventBatchId><wMsLocationId>Buffer</wMsLocationId><wMSPalletId>M1199879</wMSPalletId><inventSerialId>C36651</inventSerialId></InventDimTo></InvJournalLine><InvJournalLine><ItemId>301520500</ItemId><Qty>0</Qty><InventDimFrom><InventLocationId>CHR</InventLocationId><inventBatchId>S1711329</inventBatchId><wMsLocationId>G07405</wMsLocationId><wMSPalletId>M1199879</wMSPalletId><inventSerialId>C36651</inventSerialId></InventDimFrom><InventDimTo><InventLocationId>CHR</InventLocationId><inventBatchId>S1711329</inventBatchId><wMsLocationId>Buffer</wMsLocationId><wMSPalletId>M1199879</wMSPalletId><inventSerialId>C36651</inventSerialId></InventDimTo></InvJournalLine><InvJournalLine><ItemId>301520500</ItemId><Qty>0</Qty><InventDimFrom><InventLocationId>CHR</InventLocationId><inventBatchId>S1711326</inventBatchId><wMsLocationId>G07405</wMsLocationId><wMSPalletId>M1199879</wMSPalletId><inventSerialId>C36651</inventSerialId></InventDimFrom><InventDimTo><InventLocationId>CHR</InventLocationId><inventBatchId>S1711326</inventBatchId><wMsLocationId>Buffer</wMsLocationId><wMSPalletId>M1199879</wMSPalletId><inventSerialId>C36651</inventSerialId></InventDimTo></InvJournalLine><InvJournalLine><ItemId>301520500</ItemId><Qty>0</Qty><InventDimFrom><InventLocationId>CHR</InventLocationId><inventBatchId>S1711327</inventBatchId><wMsLocationId>G07405</wMsLocationId><wMSPalletId>M1199879</wMSPalletId><inventSerialId>C36651</inventSerialId></InventDimFrom><InventDimTo><InventLocationId>CHR</InventLocationId><inventBatchId>S1711327</inventBatchId><wMsLocationId>Buffer</wMsLocationId><wMSPalletId>M1199879</wMSPalletId><inventSerialId>C36651</inventSerialId></InventDimTo></InvJournalLine><InvJournalLine><ItemId>301520500</ItemId><Qty>0</Qty><InventDimFrom><InventLocationId>CHR</InventLocationId><inventBatchId>S1711330</inventBatchId><wMsLocationId>G07405</wMsLocationId><wMSPalletId>M1199879</wMSPalletId><inventSerialId>C36651</inventSerialId></InventDimFrom><InventDimTo><InventLocationId>CHR</InventLocationId><inventBatchId>S1711330</inventBatchId><wMsLocationId>Buffer</wMsLocationId><wMSPalletId>M1199879</wMSPalletId><inventSerialId>C36651</inventSerialId></InventDimTo></InvJournalLine></LstJournalLine></InvJournalTable>";
            //ret = PurchPackingSlip(s);
            //Test SalesPackingSlik
            //ret = SalesPackingSlip(Test4SalesPackingSlip_Export());
            //ret = SalesCreditNote(Test4SalesCreditNote_Export());
            //Test InvTransferJournal
            //ret = InvTransferJournal(Test4InvTransferJournal_Export());
            //Test InvMovementJournal
            //ret = InvMovementJournal(Test4InvMovementJournal_Export());
            //Test InvCountJournal
            //ret = InvCountJournal(Test4InvCountJournal_Export());
            //ret = Utility.XmlResult(input);
            //ret = Test4PurchCreditNote_Export();
            //ret = PurchCreditNoteByItem(ret);
            //string ret = InvTransferJournal(s);
            return ret;
        }

        #region test
        //[WebMethod]
        //public void Test()
        //{
        //    #region s
        //    string s = @"<SalesShipmentTable><ShipmentId>XSCK2017090161</ShipmentId><LstShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1785557</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219982</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1785558</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219982</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783384</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219416</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783385</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219416</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783386</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219416</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783428</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219434</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783913</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219577</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783914</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219577</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1785649</inventBatchId><wMsLocationId>G07303</wMsLocationId><wMSPalletId>M1220010</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>48</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1785651</inventBatchId><wMsLocationId>G07303</wMsLocationId><wMSPalletId>M1220010</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1785652</inventBatchId><wMsLocationId>G07303</wMsLocationId><wMSPalletId>M1220010</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>48</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1785654</inventBatchId><wMsLocationId>G07303</wMsLocationId><wMSPalletId>M1220010</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1785650</inventBatchId><wMsLocationId>G07303</wMsLocationId><wMSPalletId>M1220010</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1785653</inventBatchId><wMsLocationId>G07303</wMsLocationId><wMSPalletId>M1220010</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>48</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783910</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219577</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>46</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783911</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219577</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783912</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219577</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783905</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219577</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>41</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783906</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219577</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783907</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219577</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783908</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219577</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>47</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783918</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219578</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>44</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783919</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219578</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783920</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219578</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783922</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219578</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783923</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219578</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>46</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783924</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219578</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783915</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219578</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783916</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219578</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783917</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219578</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783381</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219416</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783382</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219416</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783679</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219507</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>48</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783680</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219507</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783687</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219507</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783688</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219507</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783681</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219507</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783682</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219507</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783683</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219507</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783684</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219507</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783685</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219507</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783686</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219507</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783427</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219434</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>44</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783677</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219506</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783678</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219506</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783671</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219506</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783672</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219506</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783673</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219506</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783674</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219506</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783675</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219506</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783676</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219506</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783669</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219506</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783670</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219506</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783861</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219552</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783862</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219552</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783853</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219552</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783854</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219552</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783855</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219552</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>48</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783856</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219552</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>48</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783857</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219552</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>48</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783858</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219552</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783921</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219578</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783387</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219416</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>47</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783388</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219416</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783389</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219416</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783380</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219416</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783383</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219416</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783909</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219577</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783425</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219434</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783426</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219434</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783429</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219434</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783430</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219434</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783431</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219434</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783422</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219434</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783423</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219434</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>43</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783424</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219434</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783859</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219552</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116618</LineId><ItemId>303960500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1783860</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1219552</wMSPalletId><inventSerialId>C61388</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116634</LineId><ItemId>303990500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1785082</inventBatchId><wMsLocationId>G05423</wMsLocationId><wMSPalletId>M1219847</wMSPalletId><inventSerialId>C61532</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116634</LineId><ItemId>303990500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1785077</inventBatchId><wMsLocationId>G05423</wMsLocationId><wMSPalletId>M1219847</wMSPalletId><inventSerialId>C61532</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116634</LineId><ItemId>303990500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1785078</inventBatchId><wMsLocationId>G05423</wMsLocationId><wMSPalletId>M1219847</wMSPalletId><inventSerialId>C61532</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116634</LineId><ItemId>303990500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1785079</inventBatchId><wMsLocationId>G05423</wMsLocationId><wMSPalletId>M1219847</wMSPalletId><inventSerialId>C61532</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116634</LineId><ItemId>303990500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1785080</inventBatchId><wMsLocationId>G05423</wMsLocationId><wMSPalletId>M1219847</wMSPalletId><inventSerialId>C61532</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116634</LineId><ItemId>303990500</ItemId><Qty>48</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1785081</inventBatchId><wMsLocationId>G05423</wMsLocationId><wMSPalletId>M1219847</wMSPalletId><inventSerialId>C61532</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116634</LineId><ItemId>303990500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1785083</inventBatchId><wMsLocationId>G05423</wMsLocationId><wMSPalletId>M1219847</wMSPalletId><inventSerialId>C61532</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116634</LineId><ItemId>303990500</ItemId><Qty>48</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1785074</inventBatchId><wMsLocationId>G05423</wMsLocationId><wMSPalletId>M1219847</wMSPalletId><inventSerialId>C61532</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116634</LineId><ItemId>303990500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1785075</inventBatchId><wMsLocationId>G05423</wMsLocationId><wMSPalletId>M1219847</wMSPalletId><inventSerialId>C61532</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116634</LineId><ItemId>303990500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1785300</inventBatchId><wMsLocationId>G08405</wMsLocationId><wMSPalletId>M1219912</wMSPalletId><inventSerialId>C61532</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116634</LineId><ItemId>303990500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1785301</inventBatchId><wMsLocationId>G08405</wMsLocationId><wMSPalletId>M1219912</wMSPalletId><inventSerialId>C61532</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116634</LineId><ItemId>303990500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1785299</inventBatchId><wMsLocationId>G08405</wMsLocationId><wMSPalletId>M1219912</wMSPalletId><inventSerialId>C61532</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116634</LineId><ItemId>303990500</ItemId><Qty>48</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1785294</inventBatchId><wMsLocationId>G08405</wMsLocationId><wMSPalletId>M1219912</wMSPalletId><inventSerialId>C61532</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116634</LineId><ItemId>303990500</ItemId><Qty>47</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1785295</inventBatchId><wMsLocationId>G08405</wMsLocationId><wMSPalletId>M1219912</wMSPalletId><inventSerialId>C61532</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116634</LineId><ItemId>303990500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1785296</inventBatchId><wMsLocationId>G08405</wMsLocationId><wMSPalletId>M1219912</wMSPalletId><inventSerialId>C61532</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116634</LineId><ItemId>303990500</ItemId><Qty>40</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1785293</inventBatchId><wMsLocationId>G08405</wMsLocationId><wMSPalletId>M1219912</wMSPalletId><inventSerialId>C61532</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116634</LineId><ItemId>303990500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1785297</inventBatchId><wMsLocationId>G08405</wMsLocationId><wMSPalletId>M1219912</wMSPalletId><inventSerialId>C61532</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116634</LineId><ItemId>303990500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1785298</inventBatchId><wMsLocationId>G08405</wMsLocationId><wMSPalletId>M1219912</wMSPalletId><inventSerialId>C61532</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116634</LineId><ItemId>303990500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1785253</inventBatchId><wMsLocationId>G04319</wMsLocationId><wMSPalletId>M1219908</wMSPalletId><inventSerialId>C61532</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116634</LineId><ItemId>303990500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1785254</inventBatchId><wMsLocationId>G04319</wMsLocationId><wMSPalletId>M1219908</wMSPalletId><inventSerialId>C61532</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116634</LineId><ItemId>303990500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1785256</inventBatchId><wMsLocationId>G04319</wMsLocationId><wMSPalletId>M1219908</wMSPalletId><inventSerialId>C61532</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116634</LineId><ItemId>303990500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1785257</inventBatchId><wMsLocationId>G04319</wMsLocationId><wMSPalletId>M1219908</wMSPalletId><inventSerialId>C61532</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116634</LineId><ItemId>303990500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1785258</inventBatchId><wMsLocationId>G04319</wMsLocationId><wMSPalletId>M1219908</wMSPalletId><inventSerialId>C61532</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116634</LineId><ItemId>303990500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1785259</inventBatchId><wMsLocationId>G04319</wMsLocationId><wMSPalletId>M1219908</wMSPalletId><inventSerialId>C61532</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116634</LineId><ItemId>303990500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1785261</inventBatchId><wMsLocationId>G04319</wMsLocationId><wMSPalletId>M1219908</wMSPalletId><inventSerialId>C61532</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116634</LineId><ItemId>303990500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1785262</inventBatchId><wMsLocationId>G04319</wMsLocationId><wMSPalletId>M1219908</wMSPalletId><inventSerialId>C61532</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116634</LineId><ItemId>303990500</ItemId><Qty>40</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1785260</inventBatchId><wMsLocationId>G04319</wMsLocationId><wMSPalletId>M1219908</wMSPalletId><inventSerialId>C61532</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116634</LineId><ItemId>303990500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1785255</inventBatchId><wMsLocationId>G04319</wMsLocationId><wMSPalletId>M1219908</wMSPalletId><inventSerialId>C61532</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116634</LineId><ItemId>303990500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1785395</inventBatchId><wMsLocationId>G04424</wMsLocationId><wMSPalletId>M1219942</wMSPalletId><inventSerialId>C61532</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116634</LineId><ItemId>303990500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1785400</inventBatchId><wMsLocationId>G04424</wMsLocationId><wMSPalletId>M1219942</wMSPalletId><inventSerialId>C61532</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116634</LineId><ItemId>303990500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1785402</inventBatchId><wMsLocationId>G04424</wMsLocationId><wMSPalletId>M1219942</wMSPalletId><inventSerialId>C61532</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116634</LineId><ItemId>303990500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1785404</inventBatchId><wMsLocationId>G04424</wMsLocationId><wMSPalletId>M1219942</wMSPalletId><inventSerialId>C61532</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116634</LineId><ItemId>303990500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1785396</inventBatchId><wMsLocationId>G04424</wMsLocationId><wMSPalletId>M1219942</wMSPalletId><inventSerialId>C61532</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116634</LineId><ItemId>303990500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1785397</inventBatchId><wMsLocationId>G04424</wMsLocationId><wMSPalletId>M1219942</wMSPalletId><inventSerialId>C61532</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116634</LineId><ItemId>303990500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1785398</inventBatchId><wMsLocationId>G04424</wMsLocationId><wMSPalletId>M1219942</wMSPalletId><inventSerialId>C61532</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116634</LineId><ItemId>303990500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1785399</inventBatchId><wMsLocationId>G04424</wMsLocationId><wMSPalletId>M1219942</wMSPalletId><inventSerialId>C61532</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116634</LineId><ItemId>303990500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1785401</inventBatchId><wMsLocationId>G04424</wMsLocationId><wMSPalletId>M1219942</wMSPalletId><inventSerialId>C61532</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116634</LineId><ItemId>303990500</ItemId><Qty>48</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1785403</inventBatchId><wMsLocationId>G04424</wMsLocationId><wMSPalletId>M1219942</wMSPalletId><inventSerialId>C61532</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116634</LineId><ItemId>303990500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1785416</inventBatchId><wMsLocationId>G06323</wMsLocationId><wMSPalletId>M1219944</wMSPalletId><inventSerialId>C61532</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116634</LineId><ItemId>303990500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1785417</inventBatchId><wMsLocationId>G06323</wMsLocationId><wMSPalletId>M1219944</wMSPalletId><inventSerialId>C61532</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116634</LineId><ItemId>303990500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1785418</inventBatchId><wMsLocationId>G06323</wMsLocationId><wMSPalletId>M1219944</wMSPalletId><inventSerialId>C61532</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116634</LineId><ItemId>303990500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1785419</inventBatchId><wMsLocationId>G06323</wMsLocationId><wMSPalletId>M1219944</wMSPalletId><inventSerialId>C61532</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116634</LineId><ItemId>303990500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1785420</inventBatchId><wMsLocationId>G06323</wMsLocationId><wMSPalletId>M1219944</wMSPalletId><inventSerialId>C61532</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116634</LineId><ItemId>303990500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1785415</inventBatchId><wMsLocationId>G06323</wMsLocationId><wMSPalletId>M1219944</wMSPalletId><inventSerialId>C61532</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116634</LineId><ItemId>303990500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1785424</inventBatchId><wMsLocationId>G06323</wMsLocationId><wMSPalletId>M1219944</wMSPalletId><inventSerialId>C61532</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116634</LineId><ItemId>303990500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1785421</inventBatchId><wMsLocationId>G06323</wMsLocationId><wMSPalletId>M1219944</wMSPalletId><inventSerialId>C61532</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116634</LineId><ItemId>303990500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1785422</inventBatchId><wMsLocationId>G06323</wMsLocationId><wMSPalletId>M1219944</wMSPalletId><inventSerialId>C61532</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116634</LineId><ItemId>303990500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1785423</inventBatchId><wMsLocationId>G06323</wMsLocationId><wMSPalletId>M1219944</wMSPalletId><inventSerialId>C61532</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><SalesId>SO012188</SalesId><LineId>237116634</LineId><ItemId>303990500</ItemId><Qty>45</Qty><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1785076</inventBatchId><wMsLocationId>G05423</wMsLocationId><wMSPalletId>M1219847</wMSPalletId><inventSerialId>C61532</inventSerialId></InventDim></SalesShipmentLine></LstShipmentLine></SalesShipmentTable>";
        //    #endregion
        //    SalesShipmentTable st =
        //            (SalesShipmentTable)Utility.XmlDeserializeFromString(s, typeof(SalesShipmentTable));
        //    StringBuilder sb = new StringBuilder();
        //    foreach (SalesShipmentLine line in st.LstShipmentLine)
        //    {
        //        InventDim dim = line.InventDim;
        //        sb.AppendLine(line.ItemId + "," + dim.inventBatchId);
        //    }
        //    s = sb.ToString();
        //}
        #endregion
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
            header.PackingSlipId = "Packing Slip 001";
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
        private string Test4PurchCreditNote_Export()
        {
            PurchCreditNote header = new PurchCreditNote();
            InventDim invDim = new InventDim();
            //1
            invDim.InventLocationId = "CHR";
            invDim.inventBatchId = "SL201709140009";
            invDim.inventSerialId = "201709142590";
            invDim.wMsLocationId = "R01105";
            invDim.wMSPalletId = "";
            
            //3
            header.PurchId = "PP014847";
            header.PackingSlipId = "Packing Slip 001";
            header.ItemId = "RC040049";
            header.Qty = -10;
            header.InventDim = invDim;
            return Utility.XmlSerializeToString(header);
        }
        private string Test4SalesPackingSlip_Export()
        {
            SalesShipmentTable header = new SalesShipmentTable();
            SalesShipmentLine line = new SalesShipmentLine();
            InventDim invDim = new InventDim();
            //1
            invDim.InventLocationId = "CHR";
            invDim.inventBatchId = "S1667014";
            invDim.inventSerialId = "C26345";
            invDim.wMsLocationId = "G03307";
            invDim.wMSPalletId = "M1188033";
            //2
            line.SalesId = "SO011152";
            line.ItemId = "026580500";
            line.Qty = 1;
            line.InventDim = invDim;
            line.LineId = "221223787";
            //3
            header.ShipmentId = "SHP-000004";
            List<SalesShipmentLine> lst = new List<SalesShipmentLine>();
            lst.Add(line);
            SalesShipmentLine line2 = new SalesShipmentLine();
            InventDim invDim2 = new InventDim();
            invDim2.InventLocationId = "CHR";
            invDim2.inventBatchId = "S1703455";
            invDim2.inventSerialId = "C37441";
            invDim2.wMsLocationId = "G02108";
            invDim2.wMSPalletId = "M1197854";
            line2.SalesId = "SO011153";
            line2.ItemId = "026580500";
            line2.Qty = 1;
            line2.InventDim = invDim2;
            line2.LineId = "221223782";
            lst.Add(line2);
            SalesShipmentLine line3 = new SalesShipmentLine();
            InventDim invDim3 = new InventDim();
            invDim3.InventLocationId = "CHR";
            invDim3.inventBatchId = "S1703455";
            invDim3.inventSerialId = "C37441";
            invDim3.wMsLocationId = "G02108";
            invDim3.wMSPalletId = "M1197854";
            line3.SalesId = "SO011153";
            line3.ItemId = "026580500";
            line3.Qty = 1;
            line3.InventDim = invDim3;
            line3.LineId = "221223782";
            lst.Add(line3);
            header.LstShipmentLine = lst;
            //return @"<SalesShipmentTable><ShipmentId>SHP-014302</ShipmentId><LstShipmentLine><SalesShipmentLine><ItemId>302530500G</ItemId><Qty>60</Qty><InvOutPutOrder>INO-030845</InvOutPutOrder><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1337612</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1096349</wMSPalletId><inventSerialId>B12161</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><ItemId>302530500G</ItemId><Qty>60</Qty><InvOutPutOrder>INO-030845</InvOutPutOrder><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1337615</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1096349</wMSPalletId><inventSerialId>B12161</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><ItemId>302530500G</ItemId><Qty>55</Qty><InvOutPutOrder>INO-030845</InvOutPutOrder><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1353734</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1100828</wMSPalletId><inventSerialId>B18022</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><ItemId>302530500G</ItemId><Qty>55</Qty><InvOutPutOrder>INO-030845</InvOutPutOrder><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1353741</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1100828</wMSPalletId><inventSerialId>B18022</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><ItemId>302770500G</ItemId><Qty>60</Qty><InvOutPutOrder>INO-030846</InvOutPutOrder><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1505701</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1143220</wMSPalletId><inventSerialId>B70498</inventSerialId></InventDim></SalesShipmentLine><SalesShipmentLine><ItemId>302770500G</ItemId><Qty>60</Qty><InvOutPutOrder>INO-030846</InvOutPutOrder><InventDim><InventLocationId>CHR</InventLocationId><inventBatchId>S1505699</inventBatchId><wMsLocationId>DOCK</wMsLocationId><wMSPalletId>M1143220</wMSPalletId><inventSerialId>B70498</inventSerialId></InventDim></SalesShipmentLine></LstShipmentLine></SalesShipmentTable>";
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
