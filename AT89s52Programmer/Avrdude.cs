using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Resources;
using System.Diagnostics;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Collections;
using System.Security.Cryptography;

namespace AT89s52Programmer
{
    public class AvrdudeInfoArgs : EventArgs
    {
        public string Info { get; set; }

        public AvrdudeInfoArgs()
        {
            Info = string.Empty;
        }
    }

    public class AvrdudeErrorArgs : EventArgs
    {
        public string Message { get; set; }

        public AvrdudeErrorArgs()
        {
            Message = string.Empty;
        }
    }

    public class Avrdude
    {
        private readonly string _tempPath;
        private readonly Process _cmdProcess;
        public event EventHandler<AvrdudeInfoArgs> AvrdudeInfo;
        public event EventHandler<AvrdudeErrorArgs> AvrdudeError;
        private string _prevFileHash;

        public Avrdude()
        {
            _tempPath = copyResources();

            _cmdProcess = new Process();
            _cmdProcess.StartInfo = new ProcessStartInfo() 
            {
                FileName = "cmd.exe",
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = _tempPath
            };

            _cmdProcess.OutputDataReceived += (s, e) =>
                AvrdudeInfo?.Invoke(this, new AvrdudeInfoArgs() { Info = e.Data ?? "" });

            _cmdProcess.ErrorDataReceived += (s, e) =>
                AvrdudeInfo?.Invoke(this, new AvrdudeInfoArgs() { Info = e.Data ?? "" });

            _cmdProcess.Start();

            _cmdProcess.BeginOutputReadLine();
            _cmdProcess.BeginErrorReadLine();
        }

        public void WriteFile(string serialPort, string hexFilePath)
        {
            var fileHash = getFileHash(hexFilePath);

            if (_prevFileHash != fileHash)
            {
                _cmdProcess.StandardInput.WriteLine($"avrdude.exe -c avrisp -p at89s52 -C config.conf -P {serialPort} -b 19200 -U flash:w:\"{hexFilePath}\":i");

                _prevFileHash = fileHash;
            }
        }

        private string copyResources()
        {
            var folderPath = $@"{Path.GetTempPath()}\avrdude";

            if (Directory.Exists(folderPath))
            {
                try
                {
                    Directory.Delete(folderPath, true);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex.Message);
                }
            }

            Directory.CreateDirectory(folderPath);

            using var avrdudeStream = new MemoryStream(Properties.Resources.avrdude);
            using var configStream = new MemoryStream(Properties.Resources.config);

            if (avrdudeStream == null)
            {
                throw new Exception("Could no load avrdude.exe");
            }

            if (configStream == null)
            {
                throw new Exception("Could no load config.conf");
            }

            using var avrdudeFile = File.Open($@"{folderPath}\avrdude.exe", FileMode.CreateNew, FileAccess.Write);
            using var configFile = File.Open($@"{folderPath}\config.conf", FileMode.CreateNew, FileAccess.Write);

            avrdudeStream!.CopyTo(avrdudeFile);
            configStream!.CopyTo(configFile);

            avrdudeFile.Close();
            avrdudeStream.Close();

            configFile.Close();
            configStream.Close();

            return folderPath;
        }

        private string getFileHash(string filePath)
        {
            using var fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var md5 = MD5.Create();

            var bytes = md5.ComputeHash(fs);

            return BitConverter.ToString(bytes);
        }
    }
}
