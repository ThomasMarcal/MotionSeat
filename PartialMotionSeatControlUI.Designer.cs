namespace Alstom.MotionSeatPlugin
{
    partial class MotionSeatControl
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
            this.UpdateTimer = new System.Windows.Forms.Timer(this.components);
            this.TickIndicator = new System.Windows.Forms.Panel();
            this.TickLabel = new System.Windows.Forms.Label();
            this.ConsoleRedirectionLabel = new System.Windows.Forms.Label();
            this.MotionSeatData = new System.Windows.Forms.Label();
            this.SimulationLabel = new System.Windows.Forms.Label();
            this.StartStopButton = new System.Windows.Forms.Button();
            this.SimulationIndicator = new System.Windows.Forms.Panel();
            this.SysTrayIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.DetailCheckbox = new System.Windows.Forms.CheckBox();
            this.MonitoringTimer = new System.Windows.Forms.Timer(this.components);
            this.UseTCPCheckbox = new System.Windows.Forms.CheckBox();
            this.TCPLabel = new System.Windows.Forms.Label();
            this.TCPIndicator = new System.Windows.Forms.Panel();
            this.SeatUITooltip = new System.Windows.Forms.ToolTip(this.components);
            this.ResponseButton = new System.Windows.Forms.Button();
            this.MotionLockButton = new System.Windows.Forms.Button();
            this.IndicatorPanel = new System.Windows.Forms.Panel();
            this.RemoteLabel = new System.Windows.Forms.Label();
            this.RemoteIndicator = new System.Windows.Forms.Panel();
            this.RemoteTimer = new System.Windows.Forms.Timer(this.components);
            this.OverallLabel = new System.Windows.Forms.Label();
            this.isDBoxConnected = new System.Windows.Forms.Label();
            this.isCommunicatedLabel = new System.Windows.Forms.Label();
            this.StreamModeLabel = new System.Windows.Forms.Label();
            this.WeightsLabel = new System.Windows.Forms.Label();
            this.SpeedLabel = new System.Windows.Forms.Label();
            this.IndicatorPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // UpdateTimer
            // 
            this.UpdateTimer.Enabled = true;
            this.UpdateTimer.Interval = 20;
            this.UpdateTimer.Tick += new System.EventHandler(this.UpdateTimer_Tick);
            // 
            // TickIndicator
            // 
            this.TickIndicator.BackColor = System.Drawing.Color.DarkGray;
            this.TickIndicator.Location = new System.Drawing.Point(18, 23);
            this.TickIndicator.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.TickIndicator.Name = "TickIndicator";
            this.TickIndicator.Size = new System.Drawing.Size(20, 8);
            this.TickIndicator.TabIndex = 1;
            // 
            // TickLabel
            // 
            this.TickLabel.AutoSize = true;
            this.TickLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 7F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TickLabel.Location = new System.Drawing.Point(44, 14);
            this.TickLabel.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.TickLabel.Name = "TickLabel";
            this.TickLabel.Size = new System.Drawing.Size(120, 24);
            this.TickLabel.TabIndex = 2;
            this.TickLabel.Text = "Local Update";
            // 
            // ConsoleRedirectionLabel
            // 
            this.ConsoleRedirectionLabel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.ConsoleRedirectionLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 6.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ConsoleRedirectionLabel.Location = new System.Drawing.Point(688, 309);
            this.ConsoleRedirectionLabel.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.ConsoleRedirectionLabel.Name = "ConsoleRedirectionLabel";
            this.ConsoleRedirectionLabel.Size = new System.Drawing.Size(812, 121);
            this.ConsoleRedirectionLabel.TabIndex = 0;
            this.ConsoleRedirectionLabel.Text = "Console.Writeline() Redirection";
            // 
            // MotionSeatData
            // 
            this.MotionSeatData.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.MotionSeatData.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MotionSeatData.Location = new System.Drawing.Point(688, 109);
            this.MotionSeatData.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.MotionSeatData.Name = "MotionSeatData";
            this.MotionSeatData.Size = new System.Drawing.Size(422, 179);
            this.MotionSeatData.TabIndex = 3;
            this.MotionSeatData.Text = "MotionSeatData";
            // 
            // SimulationLabel
            // 
            this.SimulationLabel.AutoSize = true;
            this.SimulationLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 7F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SimulationLabel.Location = new System.Drawing.Point(680, 14);
            this.SimulationLabel.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.SimulationLabel.Name = "SimulationLabel";
            this.SimulationLabel.Size = new System.Drawing.Size(97, 24);
            this.SimulationLabel.TabIndex = 4;
            this.SimulationLabel.Text = "Simulation";
            // 
            // StartStopButton
            // 
            this.StartStopButton.BackColor = System.Drawing.Color.Transparent;
            this.StartStopButton.BackgroundImage = global::Alstom.MotionSeatPlugin.Properties.Resources.OFF;
            this.StartStopButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.StartStopButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.StartStopButton.FlatAppearance.BorderSize = 0;
            this.StartStopButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.StartStopButton.Location = new System.Drawing.Point(1604, 186);
            this.StartStopButton.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.StartStopButton.Name = "StartStopButton";
            this.StartStopButton.Size = new System.Drawing.Size(178, 203);
            this.StartStopButton.TabIndex = 18;
            this.SeatUITooltip.SetToolTip(this.StartStopButton, "Toggle ON/OFF the Seat");
            this.StartStopButton.UseVisualStyleBackColor = false;
            this.StartStopButton.Click += new System.EventHandler(this.StartStopButton_Click);
            // 
            // SimulationIndicator
            // 
            this.SimulationIndicator.BackColor = System.Drawing.Color.DarkGray;
            this.SimulationIndicator.Location = new System.Drawing.Point(656, 23);
            this.SimulationIndicator.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.SimulationIndicator.Name = "SimulationIndicator";
            this.SimulationIndicator.Size = new System.Drawing.Size(20, 8);
            this.SimulationIndicator.TabIndex = 3;
            // 
            // SysTrayIcon
            // 
            this.SysTrayIcon.Text = "Motion Seat Control Panel";
            this.SysTrayIcon.Visible = true;
            this.SysTrayIcon.DoubleClick += new System.EventHandler(this.SysTrayIcon_DoubleClick);
            // 
            // DetailCheckbox
            // 
            this.DetailCheckbox.AutoSize = true;
            this.DetailCheckbox.Checked = true;
            this.DetailCheckbox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.DetailCheckbox.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.DetailCheckbox.Location = new System.Drawing.Point(1592, 9);
            this.DetailCheckbox.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.DetailCheckbox.Name = "DetailCheckbox";
            this.DetailCheckbox.Size = new System.Drawing.Size(186, 33);
            this.DetailCheckbox.TabIndex = 22;
            this.DetailCheckbox.Text = "Show Details";
            this.SeatUITooltip.SetToolTip(this.DetailCheckbox, "Show more details");
            this.DetailCheckbox.UseVisualStyleBackColor = true;
            this.DetailCheckbox.CheckedChanged += new System.EventHandler(this.DetailCheckbox_CheckedChanged);
            // 
            // MonitoringTimer
            // 
            this.MonitoringTimer.Interval = 2000;
            // 
            // UseTCPCheckbox
            // 
            this.UseTCPCheckbox.AutoSize = true;
            this.UseTCPCheckbox.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.UseTCPCheckbox.Location = new System.Drawing.Point(1294, 9);
            this.UseTCPCheckbox.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.UseTCPCheckbox.Name = "UseTCPCheckbox";
            this.UseTCPCheckbox.Size = new System.Drawing.Size(263, 33);
            this.UseTCPCheckbox.TabIndex = 32;
            this.UseTCPCheckbox.Text = "Use TCP Monitoring";
            this.SeatUITooltip.SetToolTip(this.UseTCPCheckbox, "Use TCP monitoring info");
            this.UseTCPCheckbox.UseVisualStyleBackColor = true;
            this.UseTCPCheckbox.Visible = false;
            this.UseTCPCheckbox.CheckedChanged += new System.EventHandler(this.UseTCPCheckbox_CheckedChanged);
            // 
            // TCPLabel
            // 
            this.TCPLabel.AutoSize = true;
            this.TCPLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 7F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TCPLabel.Location = new System.Drawing.Point(472, 14);
            this.TCPLabel.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.TCPLabel.Name = "TCPLabel";
            this.TCPLabel.Size = new System.Drawing.Size(141, 24);
            this.TCPLabel.TabIndex = 4;
            this.TCPLabel.Text = "TCP Monitoring";
            // 
            // TCPIndicator
            // 
            this.TCPIndicator.BackColor = System.Drawing.Color.DarkGray;
            this.TCPIndicator.Location = new System.Drawing.Point(446, 23);
            this.TCPIndicator.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.TCPIndicator.Name = "TCPIndicator";
            this.TCPIndicator.Size = new System.Drawing.Size(20, 8);
            this.TCPIndicator.TabIndex = 3;
            // 
            // ResponseButton
            // 
            this.ResponseButton.BackgroundImage = global::Alstom.MotionSeatPlugin.Properties.Resources.Response0;
            this.ResponseButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.ResponseButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.ResponseButton.FlatAppearance.BorderSize = 0;
            this.ResponseButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.ResponseButton.Location = new System.Drawing.Point(388, 178);
            this.ResponseButton.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.ResponseButton.Name = "ResponseButton";
            this.ResponseButton.Size = new System.Drawing.Size(178, 203);
            this.ResponseButton.TabIndex = 20;
            this.SeatUITooltip.SetToolTip(this.ResponseButton, "Change the seat reaction time and smoothing");
            this.ResponseButton.Click += new System.EventHandler(this.ResponseButton_Click);
            // 
            // MotionLockButton
            // 
            this.MotionLockButton.BackgroundImage = global::Alstom.MotionSeatPlugin.Properties.Resources.MotionDisabled;
            this.MotionLockButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.MotionLockButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.MotionLockButton.FlatAppearance.BorderSize = 0;
            this.MotionLockButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.MotionLockButton.Location = new System.Drawing.Point(116, 178);
            this.MotionLockButton.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.MotionLockButton.Name = "MotionLockButton";
            this.MotionLockButton.Size = new System.Drawing.Size(178, 203);
            this.MotionLockButton.TabIndex = 19;
            this.SeatUITooltip.SetToolTip(this.MotionLockButton, "Allow the simulation to drive the seat");
            this.MotionLockButton.Click += new System.EventHandler(this.MotionButton_Click);
            // 
            // IndicatorPanel
            // 
            this.IndicatorPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.IndicatorPanel.Controls.Add(this.RemoteLabel);
            this.IndicatorPanel.Controls.Add(this.DetailCheckbox);
            this.IndicatorPanel.Controls.Add(this.RemoteIndicator);
            this.IndicatorPanel.Controls.Add(this.TickIndicator);
            this.IndicatorPanel.Controls.Add(this.TickLabel);
            this.IndicatorPanel.Controls.Add(this.TCPLabel);
            this.IndicatorPanel.Controls.Add(this.SimulationIndicator);
            this.IndicatorPanel.Controls.Add(this.UseTCPCheckbox);
            this.IndicatorPanel.Controls.Add(this.SimulationLabel);
            this.IndicatorPanel.Controls.Add(this.TCPIndicator);
            this.IndicatorPanel.Location = new System.Drawing.Point(64, 447);
            this.IndicatorPanel.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.IndicatorPanel.Name = "IndicatorPanel";
            this.IndicatorPanel.Size = new System.Drawing.Size(1790, 52);
            this.IndicatorPanel.TabIndex = 35;
            // 
            // RemoteLabel
            // 
            this.RemoteLabel.AutoSize = true;
            this.RemoteLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 7F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.RemoteLabel.Location = new System.Drawing.Point(244, 14);
            this.RemoteLabel.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.RemoteLabel.Name = "RemoteLabel";
            this.RemoteLabel.Size = new System.Drawing.Size(141, 24);
            this.RemoteLabel.TabIndex = 6;
            this.RemoteLabel.Text = "Remote Update";
            // 
            // RemoteIndicator
            // 
            this.RemoteIndicator.BackColor = System.Drawing.Color.DarkGray;
            this.RemoteIndicator.Location = new System.Drawing.Point(218, 23);
            this.RemoteIndicator.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.RemoteIndicator.Name = "RemoteIndicator";
            this.RemoteIndicator.Size = new System.Drawing.Size(20, 8);
            this.RemoteIndicator.TabIndex = 5;
            // 
            // RemoteTimer
            // 
            this.RemoteTimer.Enabled = true;
            this.RemoteTimer.Tick += new System.EventHandler(this.RemoteTimer_Tick);
            // 
            // OverallLabel
            // 
            this.OverallLabel.AutoSize = true;
            this.OverallLabel.Location = new System.Drawing.Point(1152, 184);
            this.OverallLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.OverallLabel.Name = "OverallLabel";
            this.OverallLabel.Size = new System.Drawing.Size(136, 25);
            this.OverallLabel.TabIndex = 36;
            this.OverallLabel.Text = "Overall State";
            this.OverallLabel.Visible = false;
            // 
            // isDBoxConnected
            // 
            this.isDBoxConnected.AutoSize = true;
            this.isDBoxConnected.Location = new System.Drawing.Point(1152, 109);
            this.isDBoxConnected.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.isDBoxConnected.Name = "isDBoxConnected";
            this.isDBoxConnected.Size = new System.Drawing.Size(181, 25);
            this.isDBoxConnected.TabIndex = 37;
            this.isDBoxConnected.Text = "DBOX Connected";
            this.isDBoxConnected.Visible = false;
            // 
            // isCommunicatedLabel
            // 
            this.isCommunicatedLabel.AutoSize = true;
            this.isCommunicatedLabel.Location = new System.Drawing.Point(1152, 148);
            this.isCommunicatedLabel.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.isCommunicatedLabel.Name = "isCommunicatedLabel";
            this.isCommunicatedLabel.Size = new System.Drawing.Size(160, 25);
            this.isCommunicatedLabel.TabIndex = 38;
            this.isCommunicatedLabel.Text = "Communication";
            this.isCommunicatedLabel.Visible = false;
            // 
            // StreamModeLabel
            // 
            this.StreamModeLabel.AutoSize = true;
            this.StreamModeLabel.Location = new System.Drawing.Point(1152, 222);
            this.StreamModeLabel.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.StreamModeLabel.Name = "StreamModeLabel";
            this.StreamModeLabel.Size = new System.Drawing.Size(140, 25);
            this.StreamModeLabel.TabIndex = 39;
            this.StreamModeLabel.Text = "Stream Mode";
            this.StreamModeLabel.Visible = false;
            // 
            // WeightsLabel
            // 
            this.WeightsLabel.AutoSize = true;
            this.WeightsLabel.Location = new System.Drawing.Point(1152, 258);
            this.WeightsLabel.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.WeightsLabel.Name = "WeightsLabel";
            this.WeightsLabel.Size = new System.Drawing.Size(90, 25);
            this.WeightsLabel.TabIndex = 40;
            this.WeightsLabel.Text = "Weights";
            this.WeightsLabel.Visible = false;
            // 
            // SpeedLabel
            // 
            this.SpeedLabel.AutoSize = true;
            this.SpeedLabel.BackColor = System.Drawing.Color.Transparent;
            this.SpeedLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SpeedLabel.Location = new System.Drawing.Point(692, 258);
            this.SpeedLabel.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.SpeedLabel.Name = "SpeedLabel";
            this.SpeedLabel.Size = new System.Drawing.Size(85, 29);
            this.SpeedLabel.TabIndex = 41;
            this.SpeedLabel.Text = "Speed";
            this.SpeedLabel.Visible = false;
            // 
            // MotionSeatControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.BackgroundImage = global::Alstom.MotionSeatPlugin.Properties.Resources.InterfaceMotionSeat;
            this.ClientSize = new System.Drawing.Size(1922, 578);
            this.ControlBox = false;
            this.Controls.Add(this.SpeedLabel);
            this.Controls.Add(this.WeightsLabel);
            this.Controls.Add(this.StreamModeLabel);
            this.Controls.Add(this.isCommunicatedLabel);
            this.Controls.Add(this.isDBoxConnected);
            this.Controls.Add(this.OverallLabel);
            this.Controls.Add(this.StartStopButton);
            this.Controls.Add(this.IndicatorPanel);
            this.Controls.Add(this.ResponseButton);
            this.Controls.Add(this.MotionLockButton);
            this.Controls.Add(this.MotionSeatData);
            this.Controls.Add(this.ConsoleRedirectionLabel);
            this.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Location = new System.Drawing.Point(700, 700);
            this.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.MaximizeBox = false;
            this.Name = "MotionSeatControl";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Motion Seat Control Panel";
            this.TopMost = true;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MotionSeatControl_FormClosing);
            this.Load += new System.EventHandler(this.MotionSeatControl_Load);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.TopBarPanel_MouseDown);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.TopBarPanel_MouseMove);
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.TopBarPanel_MouseUp);
            this.IndicatorPanel.ResumeLayout(false);
            this.IndicatorPanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Timer UpdateTimer;
        private System.Windows.Forms.Panel TickIndicator;
        private System.Windows.Forms.Label TickLabel;
        private System.Windows.Forms.Label ConsoleRedirectionLabel;
        private System.Windows.Forms.Label MotionSeatData;
        private System.Windows.Forms.Button StartStopButton;
        private System.Windows.Forms.Button MotionLockButton;
        private System.Windows.Forms.Button ResponseButton;
        private System.Windows.Forms.Label SimulationLabel;
        private System.Windows.Forms.Panel SimulationIndicator;
        private System.Windows.Forms.NotifyIcon SysTrayIcon;
        internal System.Windows.Forms.CheckBox DetailCheckbox;
        private System.Windows.Forms.Timer MonitoringTimer;
        internal System.Windows.Forms.CheckBox UseTCPCheckbox;
        private System.Windows.Forms.Label TCPLabel;
        private System.Windows.Forms.Panel TCPIndicator;
        private System.Windows.Forms.ToolTip SeatUITooltip;
        private System.Windows.Forms.Panel IndicatorPanel;
        private System.Windows.Forms.Timer RemoteTimer;
        private System.Windows.Forms.Label RemoteLabel;
        private System.Windows.Forms.Panel RemoteIndicator;
        public System.Windows.Forms.Label OverallLabel;
        private System.Windows.Forms.Label isDBoxConnected;
        private System.Windows.Forms.Label isCommunicatedLabel;
        private System.Windows.Forms.Label StreamModeLabel;
        private System.Windows.Forms.Label WeightsLabel;
        public System.Windows.Forms.Label SpeedLabel;
    }
}

