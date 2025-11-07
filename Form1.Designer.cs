namespace MIB_AlienScanner
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
            this.components = new System.ComponentModel.Container();
            this.RadarDisplay = new System.Windows.Forms.PictureBox();
            this.LblStatus = new System.Windows.Forms.Label();
            this.RadarTimer = new System.Windows.Forms.Timer(this.components);
            this.AlertTimer = new System.Windows.Forms.Timer(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.RadarDisplay)).BeginInit();
            this.SuspendLayout();
            // 
            // RadarDisplay
            // 
            this.RadarDisplay.BackColor = System.Drawing.Color.Black;
            this.RadarDisplay.Dock = System.Windows.Forms.DockStyle.Fill;
            this.RadarDisplay.Location = new System.Drawing.Point(0, 0);
            this.RadarDisplay.Name = "RadarDisplay";
            this.RadarDisplay.Size = new System.Drawing.Size(831, 499);
            this.RadarDisplay.TabIndex = 0;
            this.RadarDisplay.TabStop = false;
            this.RadarDisplay.Paint += new System.Windows.Forms.PaintEventHandler(this.RadarDisplay_Paint);
            // 
            // LblStatus
            // 
            this.LblStatus.AutoSize = true;
            this.LblStatus.Font = new System.Drawing.Font("Consolas", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LblStatus.ForeColor = System.Drawing.Color.Lime;
            this.LblStatus.Location = new System.Drawing.Point(369, 468);
            this.LblStatus.Name = "LblStatus";
            this.LblStatus.Size = new System.Drawing.Size(70, 22);
            this.LblStatus.TabIndex = 1;
            this.LblStatus.Text = "label1";
            // 
            // RadarTimer
            // 
            this.RadarTimer.Tick += new System.EventHandler(this.RadarTimer_Tick);
            // 
            // AlertTimer
            // 
            this.AlertTimer.Tick += new System.EventHandler(this.AlertTimer_Tick);
            // 
            // Form1
            // 
            this.ClientSize = new System.Drawing.Size(831, 499);
            this.Controls.Add(this.LblStatus);
            this.Controls.Add(this.RadarDisplay);
            this.Name = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.RadarDisplay)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox RadarDisplay;
        private System.Windows.Forms.Label LblStatus;
        private System.Windows.Forms.Timer RadarTimer;
        private System.Windows.Forms.Timer AlertTimer;
    }
}

