using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Alstom.MotionSeatPlugin
{
    public partial class DebugForm : Form
    {
        public DebugForm()
        {
            InitializeComponent();
        }

        int MaxLines = 15;

        public void Append(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => Append(message)));
                return;
            }

            // Nettoyage si trop de lignes
            var lines = logBox.Lines.ToList();
            lines.Add(message);

            if (lines.Count > MaxLines)
                lines = lines.Skip(lines.Count - MaxLines).ToList();

            logBox.Lines = lines.ToArray();
            logBox.SelectionStart = logBox.Text.Length;
            logBox.ScrollToCaret();
        }

    }
}
