namespace test
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.btnInvoke = new System.Windows.Forms.Button();
            this.txtXML = new System.Windows.Forms.RichTextBox();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.txtResult = new System.Windows.Forms.RichTextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // btnInvoke
            // 
            this.btnInvoke.Location = new System.Drawing.Point(573, 352);
            this.btnInvoke.Name = "btnInvoke";
            this.btnInvoke.Size = new System.Drawing.Size(75, 23);
            this.btnInvoke.TabIndex = 0;
            this.btnInvoke.Text = "Invoke";
            this.btnInvoke.UseVisualStyleBackColor = true;
            this.btnInvoke.Click += new System.EventHandler(this.button1_Click);
            // 
            // txtXML
            // 
            this.txtXML.Location = new System.Drawing.Point(27, 25);
            this.txtXML.Name = "txtXML";
            this.txtXML.Size = new System.Drawing.Size(678, 311);
            this.txtXML.TabIndex = 1;
            this.txtXML.Text = "";
            // 
            // comboBox1
            // 
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Items.AddRange(new object[] {
            "InvCountJournal",
            "InvMovementJournal",
            "InvTransferJournal",
            "PurchCreditNoteByItem",
            "PurchPackingSlip",
            "SalesCreditNote",
            "SalesPackingSlip"});
            this.comboBox1.Location = new System.Drawing.Point(159, 354);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(355, 21);
            this.comboBox1.TabIndex = 2;
            // 
            // txtResult
            // 
            this.txtResult.Location = new System.Drawing.Point(27, 406);
            this.txtResult.Name = "txtResult";
            this.txtResult.Size = new System.Drawing.Size(678, 195);
            this.txtResult.TabIndex = 3;
            this.txtResult.Text = "";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(27, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(136, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "XML parameter input below";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(27, 359);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(90, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "Interface function";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(27, 390);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(107, 13);
            this.label3.TabIndex = 6;
            this.label3.Text = "Interface return result";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(756, 625);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtResult);
            this.Controls.Add(this.comboBox1);
            this.Controls.Add(this.txtXML);
            this.Controls.Add(this.btnInvoke);
            this.Name = "Form1";
            this.Text = "AX interface testing tool";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnInvoke;
        private System.Windows.Forms.RichTextBox txtXML;
        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.RichTextBox txtResult;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
    }
}

