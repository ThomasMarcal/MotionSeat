using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Alstom.MotionSeatPlugin
{

    /// <summary>
    /// Some static utility methods and classes
    /// </summary>
    internal static class Utility
    {
        /// <summary>
        /// Retry until a condition is true.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="actionToDo">What to do until condition not valid</param>
        /// <param name="actionToGetValue">How to get the value to compare to the enum <paramref name="conditionTheValueShouldMatch"/></param>
        /// <param name="conditionTheValueShouldMatch">The enum value you should match with the return of the <paramref name="actionToGetValue"/></param>
        /// <param name="maxAttempts">number of maximum attempts before returning false</param>
        /// <param name="delay_ms">delay between attempts</param>
        /// <param name="attemptMessage">message to print in console when re-trying</param>
        /// <returns>true if the equality is matched within the max attempts, false in other cases.</returns>
        public static async Task<bool> TryAsyncUntilEnumCondition<T>(Action actionToDo, Func<T> actionToGetValue, T conditionTheValueShouldMatch, int maxAttempts, int delay_ms, string attemptMessage) where T : Enum
        {
            maxAttempts = Clamp(1, maxAttempts, 20);
            delay_ms = Clamp(50, delay_ms, 5000);

            int attempts = 0;
            while (!actionToGetValue().Equals(conditionTheValueShouldMatch))
            {
                Console.WriteLine($"{attemptMessage}(attempt {attempts}/{maxAttempts})...");
                attempts++;
                actionToDo();
                await Task.Delay(delay_ms);
                if (attempts > maxAttempts)
                { Console.WriteLine($"{attemptMessage} : MAX ATTEMPTS REACHED"); return false; }
            }
            return true;
        }


        /// <summary>
        /// Clamp a <see cref="double"/> value between min and max
        /// </summary>
        /// <returns>The clamped value</returns>
        public static double Clamp(double min, double value, double max)
        {
            return Math.Max(Math.Min(max, value), min);
        }

        /// <summary>
        /// Clamp a <see cref="int"/> value between min and max
        /// </summary>
        /// <returns>The clamped value</returns>
        public static int Clamp(int min, int value, int max)
        {
            return Math.Max(Math.Min(max, value), min);
        }

        /// <summary>
        /// Clamp a <see cref="ushort"/> value between min and max
        /// </summary>
        /// <returns>The clamped value</returns>
        public static ushort Clamp(ushort min, ushort value, ushort max)
        {
            return Math.Max(Math.Min(max, value), min);
        }

        public static int LerpInt(int a, int b, float factor)
        {
            return (int)((1f - factor) * a + (factor * b));
        }

        /// <summary>
        /// A console redirection that copy the console content to one Label and into a txt file.
        /// </summary>
        public class ConsoleRedirectionWriter : TextWriter
        {
            private readonly Label consoleLabel;
            private readonly TextWriter defaultConsoleOutput;
            private string previousvalue1;
            private string previousvalue2;
            private string previousvalue3;

            private static readonly string logPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}/MotionSeatPlugin/{DateTime.Now.Year}-{DateTime.Now.Month}-{DateTime.Now.Day}-log.txt";
            private readonly bool canLog = false;

            /// <summary>
            /// Create a new <see cref="TextWriter"/> that output to the specified Label additionnaly to the current output.
            /// </summary>
            /// <param name="consoleLabel">The label to use as additionnal output</param>
            public ConsoleRedirectionWriter(Label consoleLabel)
            {
                this.consoleLabel = consoleLabel;
                defaultConsoleOutput = Console.Out;
                Directory.CreateDirectory(Path.GetDirectoryName(logPath));
                try
                {
                    File.AppendAllText(logPath, $"\n>> {DateTime.Now.ToShortDateString()} : Console Output Redirection : OK\n");
                    canLog = true;
                }
                catch { canLog = false; }
            }

            public override void WriteLine(string value)
            {
                // 1) Mettre à jour le Label de manière thread-safe
                UpdateLabelThreadSafe(value);

                // 2) Conserver l’historique des 3 dernières lignes
                previousvalue3 = previousvalue2;
                previousvalue2 = previousvalue1;
                previousvalue1 = value;

                // 3) Écrire dans la console standard
                defaultConsoleOutput.WriteLine(value);

                // 4) Écrire dans le fichier de log, si possible
                if (canLog)
                {
                    try
                    {
                        File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} : {value}\n");
                    }
                    catch
                    {
                        // En cas d’erreur d’écriture disque, on n’interrompt pas l’application
                    }
                }
            }

            /// <summary>
            /// Met à jour le texte du Label de manière sécurisée (thread-safe).
            /// </summary>
            private void UpdateLabelThreadSafe(string newLine)
            {
                // Construire la nouvelle valeur du Label à afficher
                string updatedText = $"{previousvalue3}\n{previousvalue2}\n{previousvalue1}\n{newLine}";

                if (consoleLabel.InvokeRequired)
                {
                    // Si on est sur un thread non-UI, on invoque l’action sur le thread UI
                    consoleLabel.Invoke((Action)(() => consoleLabel.Text = updatedText));
                }
                else
                {
                    // Si on est déjà sur le thread UI, on écrit directement
                    consoleLabel.Text = updatedText;
                }
            }


            public override Encoding Encoding
            {
                get { return Encoding.ASCII; }
            }
        }
    }
}
