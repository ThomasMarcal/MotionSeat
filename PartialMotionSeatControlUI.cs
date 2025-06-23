/*
  -----------------------------------------------------------------------------
  ⚠️  WARNING – DO NOT MODIFY THIS FILE UNLESS YOU KNOW WHAT YOU'RE DOING
  -----------------------------------------------------------------------------

===============================================================================
  MOTION SEAT UI CONTROLLER WITH SYSTEM TRAY SUPPORT - MotionSeatControl.cs
===============================================================================

  This Windows Forms class provides UI integration and system tray management
  for controlling a D-BOX motion seat. It interfaces with a MonitoringClient
  for TCP-based data exchange and exposes a compact interface through the
  system tray for background operations.

  -----------------------------------------------------------------------------
  FUNCTIONAL OVERVIEW:
  -----------------------------------------------------------------------------

  - Initializes and manages a system tray icon with a contextual menu
  - Allows toggling visibility of the main control form (Show/Hide)
  - Interacts with a D-BOX motion seat via button events
  - Manages TCP connection to receive/send monitoring data
  - Redirects console output to a form label for debugging purposes

  ----------------------------------------------------------------------------
  TECHNICAL DEPENDENCIES:
  ----------------------------------------------------------------------------

  - Requires DBOX SDK wrappers:
      --> DBox.MotionSeat
      --> DBox.TCP
  - Must be used with compatible SDK versions and DBOX seat hardware

  Do NOT remove or alter tray icon logic unless you understand Windows Forms
  NotifyIcon behavior. Improper disposal may leave ghost icons in the system tray.

  ----------------------------------------------------------------------------
  AUTHOR / MAINTAINER:
  ----------------------------------------------------------------------------

  - Developer: DAVAIL Nicolas (ALSTOM), MARCAL Thomas (ALSTOM), ALBE Alexis (DBOX)
  - Last updated: 18/04/2025
  - Project: Alstom T&S - DBOX Motion Seat Controller (C#)

===============================================================================
*/

using DBox.MotionSeat;
using DBox.TCP;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System;

namespace Alstom.MotionSeatPlugin
{
    /// <summary>
    /// Main form for controlling the motion seat via UI integration.
    /// </summary>
    internal partial class MotionSeatControl : Form
    {

        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private bool formVisible = false;

        private DebugForm debugForm;
        private ToolStripMenuItem debugMenuItem;


        /// <summary>
        /// Initializes the MotionSeatControl form.
        /// This constructor sets up the UI components, prevents the form from being shown on startup,
        /// configures the system tray icon and its context menu, and redirects console output to the UI.
        /// Note: Visibility is fully managed via the overridden SetVisibleCore method to suppress any initial display.
        /// </summary>
        public MotionSeatControl()
        {
            InitializeComponent();
        }

        /*
        /// <summary>
        /// Overrides the visibility logic to fully suppress the initial display of the form.
        /// </summary>
        protected override void SetVisibleCore(bool value)
        {
            if (!IsHandleCreated)
            {
                base.SetVisibleCore(false); // Never show the form initially
                CreateHandle();             // Ensure controls are created
                return;
            }

            base.SetVisibleCore(value);
        }
        */

        private void ToggleDebugWindow()
        {
            try
            {
                if (debugForm == null || debugForm.IsDisposed)
                {
                    debugForm = new DebugForm();
                    debugForm.FormClosed += (s, e) => debugForm = null; // auto-reset si X cliqué
                    debugForm.Show();
                }
                else if (debugForm.Visible)
                {
                    debugForm.Hide();
                }
                else
                {
                    debugForm.Show();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[DEBUG] Impossible de gérer DebugForm : " + ex.Message);
            }
        }


        private void InitializeSysTray()
        {
            trayMenu = new ContextMenuStrip();

            // Create the NotifyIcon
            try
            {
                trayIcon = new NotifyIcon
                {
                    Icon = Properties.Resources.Icon_MotionSeat,
                    Text = "Motion Seat Plugin",
                    ContextMenuStrip = trayMenu,
                    Visible = true
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors de la création de l'icône système : " + ex.Message);
            }

            trayIcon.DoubleClick += (s, e) =>
            {
                bool reallyVisible = this.Visible && this.Opacity > 0;

                if (reallyVisible)
                    Hide_UI();
                else
                    Show_UI();
            };


            // Affiche le menu contextuel à droite selon l’état actuel
            trayIcon.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtons.Right)
                {
                    // Reconstruit dynamiquement le menu
                    trayMenu.Items.Clear();

                    bool reallyVisible = this.Visible && this.Opacity > 0;

                    if (reallyVisible)
                        trayMenu.Items.Add("Hide", null, (s2, e2) => Hide_UI());
                    else
                        trayMenu.Items.Add("Show", null, (s2, e2) => Show_UI());

                    string label = (debugForm != null && debugForm.Visible) ? "Hide Debug Window" : "Show Debug Window";
                    trayMenu.Items.Add(label, null, (s2, e2) => ToggleDebugWindow());

                    trayMenu.Items.Add("Exit", null, (s2, e2) =>
                    {
                        trayIcon.Visible = false;
                        Application.Exit();
                    });

                    trayMenu.Show(Cursor.Position);
                }
            };
        }

        public void LogToDebug(string msg)
        {
            try
            {
                if (debugForm != null && !debugForm.IsDisposed)
                    debugForm.Append(msg);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[DEBUG] Erreur dans LogToDebug : " + ex.Message);
            }
        }



        /// <summary>
        /// Applique l'état d'affichage de l'UI selon le nom de la machine locale.
        /// Affiche si LocalName commence par "INST", cache sinon.
        /// </summary>
        private void ApplyInitialUIVisibility()
        {
            string name = StaticClientConfig.LocalName?.Trim().ToUpperInvariant();

            bool show = name != null && name.StartsWith("INST");

            this.Opacity = show ? 1 : 0;
            this.ShowInTaskbar = true;
            this.Show();
        }





        private MonitoringClient monitoringClient;

        private async void StartStopButton_Click(object sender, EventArgs e)
        {
            await StartOrStopSeat();
        }

        private void MotionButton_Click(object sender, EventArgs e)
        {
            EnableOrDisableMotionAtIndex(Control.ModifierKeys == (Keys.Control | Keys.Alt | Keys.Shift));//debug feature
        }

        private async void ResponseButton_Click(object sender, EventArgs e)
        {
            await UpdateSeatInfoWithIndex(); //be sure to be up to date before using the Sensitivity state.
            switch (Reactivity)
            {
                case ReactivityLevels.SECURE:
                    SetReactivityAtIndex(ReactivityLevels.NORMAL);
                    break;
                case ReactivityLevels.NORMAL:
                    SetReactivityAtIndex(ReactivityLevels.HIGH);
                    break;
                case ReactivityLevels.HIGH:
                    SetReactivityAtIndex(ReactivityLevels.NORMAL);
                    break;
                case ReactivityLevels.ERROR:
                    SetReactivityAtIndex(ReactivityLevels.SECURE);
                    break;
                default:
                    SetReactivityAtIndex(ReactivityLevels.SECURE);
                    break;
            }
        }

        /// <summary>
        /// Temporary feature to test the bump
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// 
        /*
        private void EventsButton_Click(object sender, EventArgs e)
        {
            uint event_mask = (uint)DBoxEventMask.NONE;
            if (SwitchEventCheckbox.Checked) { event_mask+= (uint)DBoxEventMask.SWITCH; }
            if (BogieHitCheckBox.Checked) { event_mask += (uint)DBoxEventMask.HIT; }
            if (BogieContinousCheckbox.Checked) { event_mask += (uint)DBoxEventMask.CONTINUOUS; }
            if (BogieSkidCheckbox.Checked) { event_mask += (uint)DBoxEventMask.SKID; }
            if (TrainImpactCheckBox.Checked) { event_mask += (uint)DBoxEventMask.IMPACT; }
            if (EnvironmentalCheckBox.Checked) { event_mask += (uint)DBoxEventMask.ENV; }
            DoLocalEvent((DBoxEventMask)event_mask);
        }
        */

        private enum StartPhase { OFF, STARTING, ON };


        private void UpdateStartStopButtonGraphics(StartPhase aspect)
        {
            switch (aspect)
            {
                case StartPhase.OFF:
                    StartStopButton.BackgroundImage = systemOff;
                    break;
                case StartPhase.STARTING:
                    StartStopButton.BackgroundImage = systemStarting;
                    break;
                case StartPhase.ON:
                    StartStopButton.BackgroundImage = systemOn;
                    break;
            }

        }



        private void UpdateMotionLockButtonGraphics(bool locked)
        {
            MotionLockButton.BackgroundImage = locked ? motionOff : motionOn;
        }



        private void UpdateResponseButtonGraphics(ReactivityLevels reactivity)
        {
            if (!MotionFlag)
            {
                ResponseButton.BackgroundImage = response0;
            }
            else
            {
                switch (reactivity)
                {
                    case ReactivityLevels.SECURE:
                        ResponseButton.BackgroundImage = response0b;
                        break;
                    case ReactivityLevels.NORMAL:
                        ResponseButton.BackgroundImage = response1;
                        break;
                    case ReactivityLevels.HIGH:
                        ResponseButton.BackgroundImage = response2;
                        break;
                    case ReactivityLevels.ERROR:
                        ResponseButton.BackgroundImage = response0;
                        break;
                }
            }
        }

        /// <summary>
        /// Toggle on or off the small scenario light on the Form.
        /// </summary>
        /// <param name="on"></param>
        internal void ToggleSimulationLight(bool on)
        {
            SimulationIndicator.BackColor = on ? color2 : color1;
        }


        private void LockUI()
        {
            StartStopButton.Enabled = false;
            MotionLockButton.Enabled = false;
            ResponseButton.Enabled = false;
            //EventsButton.Enabled = false;
        }


        private void UnlockUI()
        {
            StartStopButton.Enabled = true;
            MotionLockButton.Enabled = true;
            ResponseButton.Enabled = true;
            //EventsButton.Enabled = true;
        }

        /// <summary>
        /// Hide the UI and the taskbar icon, but do not close the form.
        /// </summary>
        internal void Hide_UI()
        {
            this.Opacity = 0;
            this.Show();                   // ⚠️ Show() ici évite Hide() qui casse tout
            this.ShowInTaskbar = true;     // ← tu veux que l’icône reste dans la taskbar
            formVisible = false;
        }

        /// <summary>
        /// Show the UI and the taskbar icon.
        /// </summary>
        internal void Show_UI()
        {
            this.Opacity = 1;
            this.Show();                   // Affiche la fenêtre
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
            formVisible = true;
        }

        /// <summary>
        /// Called when the systray icon is double-clicked
        /// </summary>
        private void SysTrayIcon_DoubleClick(object sender, EventArgs e)
        {
            if (!this.Visible)
            { Show_UI(); Console.WriteLine("(systray_doubleclick) Window is shown again."); }
            else
            { Hide_UI(); Console.WriteLine("(systray_doubleclick) Window has been hidden."); }
        }



        /// <summary>
        /// Redirect the console output to the dedicated Label on the control form (still output to console)
        /// </summary>
        private void RedirectConsoleOutput()
        {
            Console.SetOut(new Utility.ConsoleRedirectionWriter(ConsoleRedirectionLabel));
        }

        private void MotionSeatControl_FormClosing(object sender, FormClosingEventArgs e)
        {
            //SysTrayIcon.Visible = false;

            //keep a track of the last position
            int posX = this.Location.X;
            int posY = this.Location.Y;
            Directory.CreateDirectory("MotionSeatPluginSaved");
            File.WriteAllText("MotionSeatPluginSaved\\last.txt", $"{posX},{posY}");
            Console.WriteLine($"Form closing - Saving position <{posX},{posY}> to ./MotionSeatPluginSaved/last.txt");

        }


        private void DetailCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            MotionSeatData.Visible = DetailCheckbox.Checked;
            ConsoleRedirectionLabel.Visible = DetailCheckbox.Checked;
            SpeedLabel.Visible = DetailCheckbox.Checked;
            OverallLabel.Visible = DetailCheckbox.Checked;
            isDBoxConnected.Visible = DetailCheckbox.Checked;
            isCommunicatedLabel.Visible = DetailCheckbox.Checked;
            StreamModeLabel.Visible = DetailCheckbox.Checked;
            WeightsLabel.Visible = DetailCheckbox.Checked;
            UseTCPCheckbox.Visible = DetailCheckbox.Checked;
        }

        #region moving the UI

        private bool mouse_down;
        private Point delta;
        private void TopBarPanel_MouseDown(object sender, MouseEventArgs e)
        {
            if (Control.ModifierKeys == (Keys.Control | Keys.Shift))
            {
                mouse_down = true;
                delta = new Point(this.Location.X - Cursor.Position.X, this.Location.Y - Cursor.Position.Y);
            }
        }

        private void TopBarPanel_MouseUp(object sender, MouseEventArgs e)
        {
            mouse_down = false;
        }

        private void TopBarPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (mouse_down)
            {
                this.Location = new Point(Cursor.Position.X + delta.X, Cursor.Position.Y + delta.Y);
            }
        }


        #endregion

        private async void UseTCPCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            if (UseTCPCheckbox.Checked)
            {
                if (monitoringClient == null)
                {
                    monitoringClient = new MonitoringClient("127.0.0.1", 40001, true);
                }

                bool isConnected = await monitoringClient.Connect();
                if (isConnected)
                {
                    Console.WriteLine("Connexion TCP réussie !");
                    MonitoringTimer.Enabled = true;
                }
                else
                {
                    Console.WriteLine("Échec de la connexion TCP.");
                    UseTCPCheckbox.Checked = false; // Désactiver la case si la connexion échoue
                }
            }
            else
            {
                MonitoringTimer.Enabled = false;
                if (monitoringClient != null)
                {
                    monitoringClient.Disconnect();
                    Console.WriteLine("Déconnexion TCP.");
                }
            }
        }


        private void StartCheckButton_Click(object sender, EventArgs e)
        {
            allowToStart = true;
            //StartCheckButton.Visible = false;
        }

        private void MotionSeatControl_Load(object sender, EventArgs e)
        {
            StaticClientConfig.LoadFromIniFile();

            debugForm = new DebugForm();
            debugForm.Hide();

            this.DetailCheckbox.Checked = false;
            UpdateSeatInfoWithIndex();
            ConfigureDialogsBasedOnClientName();
            InitializeSysTray();
            ApplyInitialUIVisibility();
        }
    }
}
