using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DodPlaygroundNetFramework
{
    public partial class DodForm : Form
    {
        double lastTime = 0.0;
        DateTime beginningOfTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public DodForm()
        {
            InitializeComponent();
            TimeSpan t = (DateTime.UtcNow - beginningOfTime);
            lastTime = t.TotalSeconds;
            Application.Idle += HandleApplicationIdle;
        }

        private void HandleApplicationIdle(object sender, EventArgs e)
        {
            while (IsApplicationIdle())
            {
                WorldUpdate();
            }
        }

        static int frameCount = 0;
        static int totalFrameCount = 0;
        static double frameTimes = 0;

        public void WorldUpdate()
        {
            double lastLastTime = lastTime;
            TimeSpan t = (DateTime.UtcNow - beginningOfTime);
            lastTime = t.TotalSeconds;
            float deltaTime = (float)(lastTime - lastLastTime);

            Stopwatch sw = new Stopwatch();
            sw.Start();
            Program.game_update(Program.sprite_data, lastTime, deltaTime);
            sw.Stop();
            TimeSpan ts = sw.Elapsed;

            // Display only on frames that are powers of 2 (and average frames up to that point)
            frameTimes += ts.TotalMilliseconds;
            frameCount++;
            totalFrameCount++;
            if ((totalFrameCount & (totalFrameCount - 1)) == 0 && totalFrameCount > 4)
            {
                Console.WriteLine($"Update Time: {(frameTimes) / frameCount}");

                frameTimes = 0;
                frameCount = 0;
            }
        }

        bool IsApplicationIdle()
        {
            NativeMessage result;
            return PeekMessage(out result, IntPtr.Zero, (uint)0, (uint)0, (uint)0) == 0;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct NativeMessage
        {
            public IntPtr Handle;
            public uint Message;
            public IntPtr WParameter;
            public IntPtr LParameter;
            public uint Time;
            public Point Location;
        }

        [DllImport("user32.dll")]
        public static extern int PeekMessage(out NativeMessage message, IntPtr window, uint filterMin, uint filterMax, uint remove);
    }
}
