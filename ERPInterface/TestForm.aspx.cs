using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace ERPInterface
{
    public partial class TestForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {

        }

        protected void Button1_Click(object sender, EventArgs e)
        {
            Svc svc = new Svc();
            string ret = @"<SalesShipmentTable>
                            <ShipmentId>shp-2017090011</ShipmentId>
                            <LstShipmentLine>
                            <SalesShipmentLine>
                                <SalesId>SO011214</SalesId>
                                <LineId>221771204</LineId>
                                <ItemId>301520500</ItemId>
                                <Qty>1</Qty>
                                <InventDim>
                                <InventLocationId>CHR</InventLocationId>
                                <inventBatchId>S1492008</inventBatchId>
                                <wMsLocationId>DOCK</wMsLocationId>
                                <wMSPalletId>M1000002</wMSPalletId>
                                <inventSerialId>C91942</inventSerialId>
                                </InventDim>
                            </SalesShipmentLine>
                            </LstShipmentLine>
                        </SalesShipmentTable>";
            try
            {
                ret = svc.SalesPackingSlip(ret);
            }
            catch (Exception ex)
            {
                ret = ex.Message;
            }
            this.TextBox1.Text = ret;
        }
    }
}