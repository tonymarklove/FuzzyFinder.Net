using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Runtime.InteropServices;

namespace FuzzyFinder
{
    public partial class MainForm : Form
    {
        private PipeServer.Server[] pipes;
        private uint pipe = 0;
        private string[] filePaths;
        private Utilities.globalKeyboardHook hotkeys;
        private bool winKeyDown = false;
        private System.Threading.Timer timer = null;

        private int indexOfItemUnderMouseToDrag;
        private Rectangle dragBoxFromMouseDown;

        private RadioButton selectedRadio;

        public MainForm()
        {
            InitializeComponent();
            pipes = new PipeServer.Server[5];
            hotkeys = new Utilities.globalKeyboardHook();

            hotkeys.HookedKeys.Add(Keys.O);
            hotkeys.HookedKeys.Add(Keys.LWin);
            hotkeys.KeyDown += new KeyEventHandler(hotkeys_KeyDown);
            hotkeys.KeyUp += new KeyEventHandler(hotkeys_KeyUp);

            RestartFinder();

            selectedRadio = radioButton1;
            setButtonText(selectedRadio);
        }

        void hotkeys_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.LWin)
            {
                winKeyDown = true;
            }
            if (winKeyDown && e.KeyCode == Keys.O)
            {
                e.Handled = true;
            }
        }

        void hotkeys_KeyUp(object sender, KeyEventArgs e)
        {
            if (winKeyDown && e.KeyCode == Keys.O)
            {
                if (this.Handle != GetForegroundWindow())
                {
                    IntPtr ThreadID1 = GetWindowThreadProcessId(GetForegroundWindow(),
                                                                IntPtr.Zero);
                    IntPtr ThreadID2 = GetWindowThreadProcessId(this.Handle, IntPtr.Zero);

                    if (ThreadID1 != ThreadID2)
                    {
                        AttachThreadInput(ThreadID1, ThreadID2, 1);
                        SetForegroundWindow(this.Handle);
                        AttachThreadInput(ThreadID1, ThreadID2, 0);
                    }
                    else
                    {
                        SetForegroundWindow(this.Handle);
                    }

                    this.Show();
                    cmdLine.Focus();
                    cmdLine.Select(0, cmdLine.Text.Length);
                    winKeyDown = false;
                }
            }

            if (e.KeyCode == Keys.LWin)
            {
                winKeyDown = false;
            }
        }

        private void onChange()
        {
            if (listBox1.SelectedIndex < 0)
            {
                return;
            }

            string[] files = selectedFiles();

            foreach (string file in files)
            {
                string commandText = filePaths[listBox1.SelectedIndex];
                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.StartInfo.FileName = file;
                proc.StartInfo.UseShellExecute = true;
                try
                {
                    proc.Start();
                    this.Hide();
                }
                catch (Exception e)
                {
                    // Ignore failed process start.
                    MessageBox.Show(e.Message);
                }
            }
        }

        private void ClearFinder()
        {
            ClearFinder(true);
        }

        private void ClearFinder(bool hide)
        {
            if (hide) {
                this.Hide();
            }
            cmdLine.Clear();
            listBox1.Items.Clear();
        }

        private void RestartFinder()
        {
            ClearFinder(false);
            pipes[pipe] = new PipeServer.Server();
        }

        private void comboBox1_TextChanged(object sender, EventArgs e)
        {
            if (timer == null)
            {
                timer = new System.Threading.Timer(timerCallback, null, 250, Timeout.Infinite);
            }
            else
            {
                timer.Change(250, Timeout.Infinite);
            }
        }

        public void timerCallback(Object state)
        {
            this.Invoke(new TimerCallback(runFinder), state);
        }

        private void runFinder(Object obj)
        {
            string line = cmdLine.Text.Trim();

            if (line.Length < 1)
            {
                return;
            }
            string msg = pipes[pipe].RunFinder(line);
            msg = msg.Trim();
            string[] arr = msg.Split('\n');

            filePaths = new string[arr.Length];

            listBox1.Items.Clear();

            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = arr[i].Trim();
                string[] files = arr[i].Split('|');
                if (files.Length != 2) {
                    continue;
                }
                this.filePaths[i] = files[0];
                listBox1.Items.Add(files[1]);
            }

            if (listBox1.Items.Count > 0)
            {
                listBox1.SelectedIndex = 0;
            }
        }

        private void Form1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
            {
                onChange();
            }
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            int selectedIndex = listBox1.SelectedIndex;

            if (e.KeyCode == Keys.Down)
            {
                selectedIndex = (listBox1.SelectedIndex+1) % listBox1.Items.Count;
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Up)
            {
                selectedIndex = (listBox1.SelectedIndex - 1) % listBox1.Items.Count;
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                this.Hide();
            }
            else if (e.KeyCode == Keys.D && e.Control)
            {
                ChangeDirectory();
            }

            if (selectedIndex < -1 || selectedIndex >= listBox1.Items.Count)
            {
                selectedIndex = listBox1.Items.Count - 1;
            }

            if (selectedIndex != listBox1.SelectedIndex)
            {
                listBox1.ClearSelected();
                listBox1.SelectedIndex = selectedIndex;
            }
        }

        private void ChangeDirectory()
        {
            FolderBrowserDialog folder = new FolderBrowserDialog();
            folder.SelectedPath = pipes[pipe] != null ? pipes[pipe].Directory : System.IO.Directory.GetCurrentDirectory();
            if (folder.ShowDialog() == DialogResult.OK)
            {
                System.IO.Directory.SetCurrentDirectory(folder.SelectedPath);
                RestartFinder();
            }

            setButtonText(selectedRadio);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ChangeDirectory();
            GC.Collect();
        }

        private void listBox1_MouseDown(object sender, MouseEventArgs e)
        {
            // Get the index of the item the mouse is below.
            indexOfItemUnderMouseToDrag = listBox1.IndexFromPoint(e.X, e.Y);

            if (indexOfItemUnderMouseToDrag != ListBox.NoMatches)
            {

                // Remember the point where the mouse down occurred. The DragSize indicates
                // the size that the mouse can move before a drag event should be started.                
                Size dragSize = SystemInformation.DragSize;

                // Create a rectangle using the DragSize, with the mouse position being
                // at the center of the rectangle.
                dragBoxFromMouseDown = new Rectangle(new Point(e.X - (dragSize.Width / 2),
                                                               e.Y - (dragSize.Height / 2)), dragSize);
            }
            else
                // Reset the rectangle if the mouse is not over an item in the ListBox.
                dragBoxFromMouseDown = Rectangle.Empty;

        }

        private void listBox1_MouseUp(object sender, MouseEventArgs e)
        {
            // Reset the drag rectangle when the mouse button is raised.
            dragBoxFromMouseDown = Rectangle.Empty;
        }

        private void listBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (dragBoxFromMouseDown == Rectangle.Empty || dragBoxFromMouseDown.Contains(e.X, e.Y))
            {
                return;
            }

            DataObject file = new DataObject();
            file.SetData(DataFormats.FileDrop, selectedFiles());
            listBox1.DoDragDrop(file, DragDropEffects.All);
            dragBoxFromMouseDown = Rectangle.Empty;
        }

        private string[] selectedFiles()
        {
            string[] files = new string[listBox1.SelectedIndices.Count];
            for (int i = 0; i < listBox1.SelectedIndices.Count; i++)
            {
                int index = listBox1.SelectedIndices[i];
                files[i] = filePaths[index];
            }
            return files;
        }

        private void radioButton_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton button = (RadioButton)sender;
            if (button.Checked == true)
            {
                pipe = UInt32.Parse((string)button.Tag);
                selectedRadio = button;
                if (pipes[pipe] == null)
                {
                    ChangeDirectory();
                }
                else
                {
                    ClearFinder(false);
                }
            }
        }

        private void setButtonText(RadioButton button)
        {
            if (pipes[pipe] != null)
            {
                button.Text = System.IO.Path.GetFileName(pipes[pipe].Directory);
            }
        }

        private void Form1_Activated(object sender, EventArgs e)
        {
            cmdLine.Focus();
            cmdLine.Select(0, cmdLine.Text.Length);
        }

        private void listBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            onChange();
        }


        [DllImport("user32.dll", EntryPoint = "SystemParametersInfo")]
        public static extern bool SystemParametersInfo(uint uiAction, uint uiParam, uint pvParam, uint fWinIni);

        [DllImport("user32.dll", EntryPoint = "SetForegroundWindow")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("User32.dll", EntryPoint = "ShowWindowAsync")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int cmdShow);
        private const int WS_SHOWNORMAL = 1;

        [DllImport("User32.dll", EntryPoint = "BringWindowToTop")]
        private static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);
        [DllImport("user32.dll")]
        private static extern IntPtr AttachThreadInput(IntPtr idAttach, IntPtr idAttachTo, int fAttach);


    }
}
