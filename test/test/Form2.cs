using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace test
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string input = this.richTextBox1.Text;
            SalesShipmentTable st =
               (SalesShipmentTable)Utility.XmlDeserializeFromString(input, typeof(SalesShipmentTable));
            StringBuilder sb = new StringBuilder();
            foreach (SalesShipmentLine line in st.LstShipmentLine)
            {
                sb.AppendLine(line.ItemId + "," + line.Qty + "," + line.InventDim.inventBatchId);
            }
            richTextBox2.Text = sb.ToString();
        }

        
    }

    public static class Utility
    {
        public static string XmlSerializeToString(Object objectInstance)
        {
            var serializer = new XmlSerializer(objectInstance.GetType());
            var sb = new StringBuilder();

            using (TextWriter writer = new StringWriter(sb))
            {
                serializer.Serialize(writer, objectInstance);
            }

            return sb.ToString();
        }


        public static object XmlDeserializeFromString(this string objectData, Type type)
        {
            var serializer = new XmlSerializer(type);
            object result;

            using (TextReader reader = new StringReader(objectData))
            {
                result = serializer.Deserialize(reader);
            }

            return result;
        }
    }
        
    public class SalesShipmentTable
    {
        public string ShipmentId { get; set; }
        public List<SalesShipmentLine> LstShipmentLine { get; set; }
    }
    public class InventDim
    {
        public string InventLocationId { get; set; }
        public string inventBatchId { get; set; }
        public string wMsLocationId { get; set; }
        public string wMSPalletId { get; set; }
        public string inventSerialId { get; set; }
    }
    public class SalesShipmentLine
    {
        public string SalesId { get; set; }
        public string LineId { get; set; }
        public string ItemId { get; set; }
        public decimal Qty { get; set; }
        public InventDim InventDim { get; set; }
    }

}
