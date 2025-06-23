namespace Alstom.MotionSeatPlugin
{
    partial class Pop_Up_MotionSeat
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Pop_Up_MotionSeat));
            this.PopUp_ButtonOk = new System.Windows.Forms.Button();
            this.PopUp_ButtonCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // PopUp_ButtonOk
            // 
            this.PopUp_ButtonOk.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(99)))), ((int)(((byte)(196)))), ((int)(((byte)(99)))));
            this.PopUp_ButtonOk.Cursor = System.Windows.Forms.Cursors.Hand;
            resources.ApplyResources(this.PopUp_ButtonOk, "PopUp_ButtonOk");
            this.PopUp_ButtonOk.ForeColor = System.Drawing.Color.White;
            this.PopUp_ButtonOk.Name = "PopUp_ButtonOk";
            this.PopUp_ButtonOk.UseVisualStyleBackColor = false;
            this.PopUp_ButtonOk.Click += new System.EventHandler(this.PopUp_ButtonOk_Click);
            // 
            // PopUp_ButtonCancel
            // 
            this.PopUp_ButtonCancel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(238)))), ((int)(((byte)(95)))), ((int)(((byte)(91)))));
            this.PopUp_ButtonCancel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.PopUp_ButtonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            resources.ApplyResources(this.PopUp_ButtonCancel, "PopUp_ButtonCancel");
            this.PopUp_ButtonCancel.ForeColor = System.Drawing.SystemColors.Window;
            this.PopUp_ButtonCancel.Name = "PopUp_ButtonCancel";
            this.PopUp_ButtonCancel.UseMnemonic = false;
            this.PopUp_ButtonCancel.UseVisualStyleBackColor = false;
            this.PopUp_ButtonCancel.Click += new System.EventHandler(this.PopUp_ButtonCancel_Click);
            // 
            // Pop_Up_MotionSeat
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImage = global::Alstom.MotionSeatPlugin.Properties.Resources.PopUpMotionSeat;
            this.ControlBox = false;
            this.Controls.Add(this.PopUp_ButtonCancel);
            this.Controls.Add(this.PopUp_ButtonOk);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "Pop_Up_MotionSeat";
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.PopUp_MouseDown);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.PopUp_MouseMove);
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.PopUp_MouseUp);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button PopUp_ButtonOk;
        private System.Windows.Forms.Button PopUp_ButtonCancel;
    }
}