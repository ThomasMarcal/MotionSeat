using Alstom.MotionSeatPlugin;
using DBox.MotionSeat;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace Alstom.MotionBridgeMimic
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Créer l’objet motion seat avant de lancer le formulaire
            MotionSeat motionSeat = new MotionSeat();

            // Passer cet objet au constructeur de MotionSeatControl
            Application.Run(new MotionSeatControl(motionSeat));
        }

    }
}
