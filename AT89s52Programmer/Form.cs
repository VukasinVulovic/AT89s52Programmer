using System.IO.Ports;
using System.Linq;
using System.Windows.Forms;

namespace AT89s52Programmer
{
    public partial class Form : System.Windows.Forms.Form
    {
        private readonly Avrdude _avrdude;
        private bool shouldWatch = false;
        private System.Windows.Forms.Timer _timer;

        public Form()
        {
            InitializeComponent();

            _timer = new System.Windows.Forms.Timer();
            _timer.Tick += (s, e) => setPorts();
            _timer.Interval = 100;
            _timer.Start();


            folderBrowserDialog.RootFolder = Environment.SpecialFolder.Desktop;
            fileSystemWatcher.Filter = "*.hex";

            _avrdude = new Avrdude();

            _avrdude.AvrdudeInfo += (s, ev) => addToTextbox(ev.Info);
            _avrdude.AvrdudeError += (s, ev) => addToTextbox(ev.Message);

            fileSystemWatcher.Path = folderBrowserDialog.SelectedPath;

            fileSystemWatcher.Changed += (s, ev) => onFile(ev.FullPath);
            fileSystemWatcher.Created += (s, ev) => onFile(ev.FullPath);

        }

        private void setPorts()
        {
            var ports = SerialPort.GetPortNames();

            if (ports.Length > 0)
            {
                portCmb.Items.Clear();
                portCmb.Items.AddRange(ports.Except(portCmb.Items.Cast<string>()).ToArray());
                portCmb.Text = ports[0];
            }
        }

        delegate void SetTextBoxCallback(string text);

        private void addToTextbox(string text)
        {
            if (rtbOutput.InvokeRequired)
            {
                var d = new SetTextBoxCallback(addToTextbox);
                Invoke(d, new object[] { text });
            }
            else
            {
                rtbOutput.Text += text;
                rtbOutput.SelectionStart = rtbOutput.Text.Length;
                rtbOutput.ScrollToCaret();
            }
        }

        private void openFolderBtn(object sender, EventArgs e)
        {
            if (!shouldWatch)
            {
                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    fileSystemWatcher.Path = folderBrowserDialog.SelectedPath;
                    fileSystemWatcher.IncludeSubdirectories = true;
                    fileSystemWatcher.EnableRaisingEvents = true;
                }
            }    
            
            openHexFileBtn.Text = shouldWatch ? "Start" : "Stop";
            shouldWatch = !shouldWatch;
        }

        private void onFile(string filePath)
        {
            if (portCmb.Text.Length == 0)
            {
                return;
            }

            if (File.Exists(filePath))
            {
                statusLabel.Text = $"Found file {Path.GetFileName(filePath)}.";

                _avrdude.WriteFile(portCmb.Text, filePath);
            }
        }

        private void showInfo()
        {
            statusLabel.Text = $"Path: {fileSystemWatcher.Path}; Seial port: {portCmb.Text}";
        }
    }
}