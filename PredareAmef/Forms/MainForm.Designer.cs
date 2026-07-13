namespace PredareAmef.Forms
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private System.Windows.Forms.GroupBox grpConn;
        private System.Windows.Forms.RadioButton rbSerial;
        private System.Windows.Forms.RadioButton rbLan;
        private System.Windows.Forms.Label lblCom;
        private System.Windows.Forms.ComboBox cbCom;
        private System.Windows.Forms.Label lblBaud;
        private System.Windows.Forms.ComboBox cbBaud;
        private System.Windows.Forms.Label lblIp;
        private System.Windows.Forms.TextBox txtIp;
        private System.Windows.Forms.Label lblPort;
        private System.Windows.Forms.TextBox txtPort;

        private System.Windows.Forms.GroupBox grpOpt;
        private System.Windows.Forms.Label lblClient;
        private System.Windows.Forms.TextBox txtClient;
        private System.Windows.Forms.Label lblOutput;
        private System.Windows.Forms.TextBox txtOutput;
        private System.Windows.Forms.Button btnBrowseOutput;
        private System.Windows.Forms.GroupBox grpSteps;
        private System.Windows.Forms.CheckBox chkStep1;
        private System.Windows.Forms.CheckBox chkStep3;
        private System.Windows.Forms.CheckBox chkStep5;
        private System.Windows.Forms.CheckBox chkStep6;
        private System.Windows.Forms.CheckBox chkStep7;
        private System.Windows.Forms.CheckBox chkAnafCustom;
        private System.Windows.Forms.Label lblAnafFrom;
        private System.Windows.Forms.DateTimePicker dtpAnafFrom;
        private System.Windows.Forms.Label lblAnafTo;
        private System.Windows.Forms.DateTimePicker dtpAnafTo;

        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.ProgressBar prg;
        private System.Windows.Forms.ProgressBar prgSub;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label lblSubStatus;
        private System.Windows.Forms.RichTextBox rtbLog;
        private System.Windows.Forms.Button btnOpenOutput;
        private System.Windows.Forms.Button btnMulti;
        private System.Windows.Forms.Button btnScan;

        private void InitializeComponent()
        {
            this.grpConn = new System.Windows.Forms.GroupBox();
            this.rbSerial = new System.Windows.Forms.RadioButton();
            this.rbLan = new System.Windows.Forms.RadioButton();
            this.lblCom = new System.Windows.Forms.Label();
            this.cbCom = new System.Windows.Forms.ComboBox();
            this.lblBaud = new System.Windows.Forms.Label();
            this.cbBaud = new System.Windows.Forms.ComboBox();
            this.lblIp = new System.Windows.Forms.Label();
            this.txtIp = new System.Windows.Forms.TextBox();
            this.lblPort = new System.Windows.Forms.Label();
            this.txtPort = new System.Windows.Forms.TextBox();

            this.grpOpt = new System.Windows.Forms.GroupBox();
            this.lblClient = new System.Windows.Forms.Label();
            this.txtClient = new System.Windows.Forms.TextBox();
            this.lblOutput = new System.Windows.Forms.Label();
            this.txtOutput = new System.Windows.Forms.TextBox();
            this.btnBrowseOutput = new System.Windows.Forms.Button();

            this.grpSteps = new System.Windows.Forms.GroupBox();
            this.chkStep1 = new System.Windows.Forms.CheckBox();
            this.chkStep3 = new System.Windows.Forms.CheckBox();
            this.chkStep5 = new System.Windows.Forms.CheckBox();
            this.chkStep6 = new System.Windows.Forms.CheckBox();
            this.chkStep7 = new System.Windows.Forms.CheckBox();
            this.chkAnafCustom = new System.Windows.Forms.CheckBox();
            this.lblAnafFrom = new System.Windows.Forms.Label();
            this.dtpAnafFrom = new System.Windows.Forms.DateTimePicker();
            this.lblAnafTo = new System.Windows.Forms.Label();
            this.dtpAnafTo = new System.Windows.Forms.DateTimePicker();

            this.btnStart = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.prg = new System.Windows.Forms.ProgressBar();
            this.prgSub = new System.Windows.Forms.ProgressBar();
            this.lblStatus = new System.Windows.Forms.Label();
            this.lblSubStatus = new System.Windows.Forms.Label();
            this.rtbLog = new System.Windows.Forms.RichTextBox();
            this.btnOpenOutput = new System.Windows.Forms.Button();

            // grpConn
            this.grpConn.Text = "1. Conexiune aparat";
            this.grpConn.Location = new System.Drawing.Point(12, 12);
            this.grpConn.Size = new System.Drawing.Size(860, 92);
            this.grpConn.Controls.Add(this.rbSerial);
            this.grpConn.Controls.Add(this.rbLan);
            this.grpConn.Controls.Add(this.lblCom);
            this.grpConn.Controls.Add(this.cbCom);
            this.grpConn.Controls.Add(this.lblBaud);
            this.grpConn.Controls.Add(this.cbBaud);
            this.grpConn.Controls.Add(this.lblIp);
            this.grpConn.Controls.Add(this.txtIp);
            this.grpConn.Controls.Add(this.lblPort);
            this.grpConn.Controls.Add(this.txtPort);

            this.rbSerial.Text = "Serial";
            this.rbSerial.Location = new System.Drawing.Point(15, 25);
            this.rbSerial.Size = new System.Drawing.Size(70, 24);
            this.rbSerial.Checked = true;
            this.rbSerial.CheckedChanged += new System.EventHandler(this.OnTransportChanged);

            this.rbLan.Text = "LAN";
            this.rbLan.Location = new System.Drawing.Point(15, 55);
            this.rbLan.Size = new System.Drawing.Size(70, 24);

            this.lblCom.Text = "Port:";
            this.lblCom.Location = new System.Drawing.Point(95, 28);
            this.lblCom.Size = new System.Drawing.Size(40, 20);

            this.cbCom.Location = new System.Drawing.Point(140, 25);
            this.cbCom.Size = new System.Drawing.Size(90, 24);

            this.lblBaud.Text = "Baud:";
            this.lblBaud.Location = new System.Drawing.Point(245, 28);
            this.lblBaud.Size = new System.Drawing.Size(40, 20);

            this.cbBaud.Location = new System.Drawing.Point(290, 25);
            this.cbBaud.Size = new System.Drawing.Size(90, 24);
            this.cbBaud.Items.AddRange(new object[] { "9600", "19200", "38400", "57600", "115200" });
            this.cbBaud.SelectedIndex = 4;

            this.lblIp.Text = "IP:";
            this.lblIp.Location = new System.Drawing.Point(95, 58);
            this.lblIp.Size = new System.Drawing.Size(40, 20);

            this.txtIp.Text = "192.168.1.100";
            this.txtIp.Location = new System.Drawing.Point(140, 55);
            this.txtIp.Size = new System.Drawing.Size(140, 23);
            this.txtIp.Enabled = false;

            this.lblPort.Text = "Port:";
            this.lblPort.Location = new System.Drawing.Point(290, 58);
            this.lblPort.Size = new System.Drawing.Size(40, 20);

            this.txtPort.Text = "3999";
            this.txtPort.Location = new System.Drawing.Point(330, 55);
            this.txtPort.Size = new System.Drawing.Size(60, 23);
            this.txtPort.Enabled = false;

            // btnScan — Scan aparate + info detaliat (in grpConn, dreapta)
            this.btnScan = new System.Windows.Forms.Button();
            this.btnScan.Text = "🔍 Scan + Info aparat";
            this.btnScan.Location = new System.Drawing.Point(420, 24);
            this.btnScan.Size = new System.Drawing.Size(220, 55);
            this.btnScan.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnScan.BackColor = System.Drawing.Color.FromArgb(0, 150, 100);
            this.btnScan.ForeColor = System.Drawing.Color.White;
            this.btnScan.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnScan.Click += new System.EventHandler(this.OnScanDevices);
            this.grpConn.Controls.Add(this.btnScan);

            // grpOpt
            this.grpOpt.Text = "2. Identificare client + output";
            this.grpOpt.Location = new System.Drawing.Point(12, 115);
            this.grpOpt.Size = new System.Drawing.Size(860, 95);
            this.grpOpt.Controls.Add(this.lblClient);
            this.grpOpt.Controls.Add(this.txtClient);
            this.grpOpt.Controls.Add(this.lblOutput);
            this.grpOpt.Controls.Add(this.txtOutput);
            this.grpOpt.Controls.Add(this.btnBrowseOutput);

            this.lblClient.Text = "Nume client:";
            this.lblClient.Location = new System.Drawing.Point(15, 28);
            this.lblClient.Size = new System.Drawing.Size(90, 20);

            this.txtClient.Location = new System.Drawing.Point(110, 25);
            this.txtClient.Size = new System.Drawing.Size(300, 23);
            this.txtClient.Text = "Client";

            this.lblOutput.Text = "Folder output:";
            this.lblOutput.Location = new System.Drawing.Point(15, 58);
            this.lblOutput.Size = new System.Drawing.Size(90, 20);

            this.txtOutput.Location = new System.Drawing.Point(110, 55);
            this.txtOutput.Size = new System.Drawing.Size(640, 23);

            this.btnBrowseOutput.Text = "...";
            this.btnBrowseOutput.Location = new System.Drawing.Point(760, 54);
            this.btnBrowseOutput.Size = new System.Drawing.Size(35, 25);
            this.btnBrowseOutput.Click += new System.EventHandler(this.OnBrowseOutput);

            // grpSteps
            this.grpSteps.Text = "3. Pasi predare (toti activi default)";
            this.grpSteps.Location = new System.Drawing.Point(12, 220);
            this.grpSteps.Size = new System.Drawing.Size(860, 135);
            this.grpSteps.Controls.Add(this.chkStep1);
            this.grpSteps.Controls.Add(this.chkStep3);
            this.grpSteps.Controls.Add(this.chkStep5);
            this.grpSteps.Controls.Add(this.chkStep6);
            this.grpSteps.Controls.Add(this.chkStep7);
            this.grpSteps.Controls.Add(this.chkAnafCustom);
            this.grpSteps.Controls.Add(this.lblAnafFrom);
            this.grpSteps.Controls.Add(this.dtpAnafFrom);
            this.grpSteps.Controls.Add(this.lblAnafTo);
            this.grpSteps.Controls.Add(this.dtpAnafTo);

            this.chkStep1.Text = "1+2. Dump FM raw → .bin (CMD 116)";
            this.chkStep1.Location = new System.Drawing.Point(15, 22);
            this.chkStep1.Size = new System.Drawing.Size(280, 24);
            this.chkStep1.Checked = true;

            this.chkStep3.Text = "3. FM sumar Z1→ultim Z (.txt)";
            this.chkStep3.Location = new System.Drawing.Point(305, 22);
            this.chkStep3.Size = new System.Drawing.Size(240, 24);
            this.chkStep3.Checked = true;

            this.chkStep5.Text = "4. Printare sumar pe aparat";
            this.chkStep5.Location = new System.Drawing.Point(555, 22);
            this.chkStep5.Size = new System.Drawing.Size(220, 24);
            this.chkStep5.Checked = true;

            this.chkStep6.Text = "5. Export ANAF .p7b 3 luni";
            this.chkStep6.Location = new System.Drawing.Point(15, 48);
            this.chkStep6.Size = new System.Drawing.Size(280, 24);
            this.chkStep6.Checked = true;

            this.chkStep7.Text = "6. Antet → .txt";
            this.chkStep7.Location = new System.Drawing.Point(305, 48);
            this.chkStep7.Size = new System.Drawing.Size(200, 24);
            this.chkStep7.Checked = true;

            // chkAnafCustom — activeaza perioada custom pentru export ANAF .p7b
            this.chkAnafCustom.Text = "5b. Export ANAF .p7b PERIOADA CUSTOM:";
            this.chkAnafCustom.Location = new System.Drawing.Point(15, 80);
            this.chkAnafCustom.Size = new System.Drawing.Size(290, 24);
            this.chkAnafCustom.Checked = false;
            this.chkAnafCustom.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.chkAnafCustom.ForeColor = System.Drawing.Color.FromArgb(180, 60, 0);
            this.chkAnafCustom.CheckedChanged += new System.EventHandler(this.OnAnafCustomCheckedChanged);

            this.lblAnafFrom.Text = "De la:";
            this.lblAnafFrom.Location = new System.Drawing.Point(310, 84);
            this.lblAnafFrom.Size = new System.Drawing.Size(45, 20);
            this.lblAnafFrom.TextAlign = System.Drawing.ContentAlignment.MiddleRight;

            this.dtpAnafFrom.Location = new System.Drawing.Point(360, 81);
            this.dtpAnafFrom.Size = new System.Drawing.Size(130, 23);
            this.dtpAnafFrom.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpAnafFrom.CustomFormat = "yyyy-MM-dd";
            this.dtpAnafFrom.Value = new System.DateTime(2022, 1, 1);
            this.dtpAnafFrom.MinDate = new System.DateTime(2018, 1, 1);
            this.dtpAnafFrom.Enabled = false;

            this.lblAnafTo.Text = "Pana la:";
            this.lblAnafTo.Location = new System.Drawing.Point(500, 84);
            this.lblAnafTo.Size = new System.Drawing.Size(55, 20);
            this.lblAnafTo.TextAlign = System.Drawing.ContentAlignment.MiddleRight;

            this.dtpAnafTo.Location = new System.Drawing.Point(560, 81);
            this.dtpAnafTo.Size = new System.Drawing.Size(130, 23);
            this.dtpAnafTo.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpAnafTo.CustomFormat = "yyyy-MM-dd";
            this.dtpAnafTo.Value = System.DateTime.Today;
            this.dtpAnafTo.MaxDate = System.DateTime.Today.AddDays(1);
            this.dtpAnafTo.Enabled = false;

            // btnStart
            this.btnStart.Text = "ÎNCEPE PREDARE MEMORIE";
            this.btnStart.Location = new System.Drawing.Point(12, 365);
            this.btnStart.Size = new System.Drawing.Size(380, 40);
            this.btnStart.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.btnStart.BackColor = System.Drawing.Color.FromArgb(0, 120, 215);
            this.btnStart.ForeColor = System.Drawing.Color.White;
            this.btnStart.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStart.Click += new System.EventHandler(this.OnStart);

            // btnCancel
            this.btnCancel.Text = "Anulează";
            this.btnCancel.Location = new System.Drawing.Point(402, 365);
            this.btnCancel.Size = new System.Drawing.Size(110, 40);
            this.btnCancel.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnCancel.BackColor = System.Drawing.Color.FromArgb(200, 60, 60);
            this.btnCancel.ForeColor = System.Drawing.Color.White;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.Enabled = false;
            this.btnCancel.Click += new System.EventHandler(this.OnCancel);

            // btnOpenOutput
            this.btnOpenOutput.Text = "Deschide folder output";
            this.btnOpenOutput.Location = new System.Drawing.Point(522, 365);
            this.btnOpenOutput.Size = new System.Drawing.Size(175, 40);
            this.btnOpenOutput.Click += new System.EventHandler(this.OnOpenOutput);

            // btnMulti — lansare modul multi-aparat
            this.btnMulti = new System.Windows.Forms.Button();
            this.btnMulti.Text = "Multi-aparat ▶";
            this.btnMulti.Location = new System.Drawing.Point(707, 365);
            this.btnMulti.Size = new System.Drawing.Size(165, 40);
            this.btnMulti.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnMulti.BackColor = System.Drawing.Color.FromArgb(70, 90, 160);
            this.btnMulti.ForeColor = System.Drawing.Color.White;
            this.btnMulti.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnMulti.Click += new System.EventHandler(this.OnMultiSession);
            this.Controls.Add(this.btnMulti);

            // prg (overall)
            this.prg.Location = new System.Drawing.Point(12, 415);
            this.prg.Size = new System.Drawing.Size(860, 18);
            this.prg.Minimum = 0;
            this.prg.Maximum = 7;

            // lblStatus
            this.lblStatus.Text = "Gata.";
            this.lblStatus.Location = new System.Drawing.Point(12, 437);
            this.lblStatus.Size = new System.Drawing.Size(860, 18);
            this.lblStatus.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);

            // prgSub (fine-grained per step)
            this.prgSub.Location = new System.Drawing.Point(12, 459);
            this.prgSub.Size = new System.Drawing.Size(860, 14);
            this.prgSub.Minimum = 0;
            this.prgSub.Maximum = 100;
            this.prgSub.Style = System.Windows.Forms.ProgressBarStyle.Continuous;

            // lblSubStatus
            this.lblSubStatus.Text = "";
            this.lblSubStatus.Location = new System.Drawing.Point(12, 477);
            this.lblSubStatus.Size = new System.Drawing.Size(860, 18);
            this.lblSubStatus.ForeColor = System.Drawing.Color.DarkBlue;

            // rtbLog
            this.rtbLog.Location = new System.Drawing.Point(12, 500);
            this.rtbLog.Size = new System.Drawing.Size(860, 280);
            this.rtbLog.Font = new System.Drawing.Font("Consolas", 9F);
            this.rtbLog.ReadOnly = true;

            // MainForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(884, 800);
            this.Controls.Add(this.grpConn);
            this.Controls.Add(this.grpOpt);
            this.Controls.Add(this.grpSteps);
            this.Controls.Add(this.btnStart);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOpenOutput);
            this.Controls.Add(this.prg);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.prgSub);
            this.Controls.Add(this.lblSubStatus);
            this.Controls.Add(this.rtbLog);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Text = "PredareAmef — Utilitar predare memorie fiscala";
            this.Load += new System.EventHandler(this.OnLoad);
        }

        #endregion
    }
}
