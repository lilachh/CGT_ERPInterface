using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace test
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string input = this.txtXML.Text;
            Svc.SvcSoapClient svc = new Svc.SvcSoapClient();
            
            string ret = "";
            try
            {

                switch (this.comboBox1.SelectedItem.ToString())
                {
                    case "InvCountJournal":
                        ret = svc.InvCountJournal(input);
                        break;
                    case "InvMovementJournal":
                        ret = svc.InvMovementJournal(input);
                        break;
                    case "InvTransferJournal":
                        ret = svc.InvTransferJournal(input);
                        break;
                    case "PurchCreditNoteByItem":
                        ret = svc.PurchCreditNoteByItem(input);
                        break;
                    case "PurchPackingSlip":
                        ret = svc.PurchPackingSlip(input);
                        break;
                    case "SalesCreditNote":
                        ret = svc.SalesCreditNote(input);
                        break;
                    case "SalesPackingSlip":
                        ret = svc.SalesPackingSlip(input);
                        break;
                    default:
                        ret = svc.HelloWorld(input);
                        break;
                }
            }
            catch (Exception ex)
            {
                ret = ex.Message;
            }
            txtResult.Text = ret;
        }
    }
}
