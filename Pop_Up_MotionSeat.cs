using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

namespace Alstom.MotionSeatPlugin
{
    public partial class Pop_Up_MotionSeat : Form
    {
        private MotionSeatControl _control;

        // Constructeur rendu internal pour que l'accessibilité du paramètre soit cohérente.
        internal Pop_Up_MotionSeat(MotionSeatControl control)
        {
            InitializeComponent();
            _control = control;

            this.FormClosing += PopUp_FormClosing;
            this.StartPosition = FormStartPosition.Manual;

            //if known, use last window position
            if (File.Exists("MotionSeatPopUpSaved\\last.txt"))
            {
                Console.WriteLine("Found ./MotionSeatPopUpSaved/last.txt : old location will be used if possible.");
                string content = File.ReadAllText("MotionSeatPopUpSaved\\last.txt");
                string[] parse = content.Split(',');
                try
                { this.Location = new Point(Int32.Parse(parse[0]), Int32.Parse(parse[1])); }
                catch { Console.WriteLine("Cannot apply old location. Default one will be used."); }
            }
            else
            {
                Console.WriteLine($"No saved location found. Defaulting to 0,0.");
                this.Location = new Point(0, 0);
            }
        }

        private void PopUp_ButtonOk_Click(object sender, EventArgs e)
        {
            // Assurez-vous que la propriété AllowToStart est accessible (public ou internal) dans MotionSeatControl.
            _control.allowToStart = true;
            this.Close();
        }

        private void PopUp_ButtonCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void PopUp_FormClosing(object sender, FormClosingEventArgs e)
        {
            //keep a track of the last position
            int posX = this.Location.X;
            int posY = this.Location.Y;
            Directory.CreateDirectory("MotionSeatPopUpSaved");
            File.WriteAllText("MotionSeatPopUpSaved\\last.txt", $"{posX},{posY}");
            Console.WriteLine($"Form closing - Saving position <{posX},{posY}> to ./MotionSeatPopUpSaved/last.txt");

        }

        #region moving the UI

        private bool mouse_down;
        private Point delta;
        private void PopUp_MouseDown(object sender, MouseEventArgs e)
        {
            if (Control.ModifierKeys == (Keys.Control | Keys.Shift))
            {
                mouse_down = true;
                delta = new Point(this.Location.X - Cursor.Position.X, this.Location.Y - Cursor.Position.Y);
            }
        }

        private void PopUp_MouseUp(object sender, MouseEventArgs e)
        {
            mouse_down = false;
        }

        private void PopUp_MouseMove(object sender, MouseEventArgs e)
        {
            if (mouse_down)
            {
                this.Location = new Point(Cursor.Position.X + delta.X, Cursor.Position.Y + delta.Y);
            }
        }


        #endregion
    }
}
