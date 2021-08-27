using Microsoft.Win32;

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace DTALauncherStub
{
    class Program
    {
        private const string RESOURCES = "Resources";
        private const int ERROR_CANCELLED_CODE = 1223;
        private const int NET_FRAMEWORK_45_RELEASE_KEY = 378389;

        private static void Main(string[] args)
        {
            var osVersion = GetOperatingSystemVersion();

            if (args != null)
            {
                foreach (string arg in args)
                {
                    switch (arg.ToUpper())
                    {
                        //case "-XNA":
                        //    RunXNA();
                        //    return;
                        //case "-OGL":
                        //    RunOGL();
                        //    return;
                        case "-FNA":
                            RunFNA();
                            return;
                        case "-DX":
                            RunDX();
                            return;
                    }
                }
            }

            RunUpdate();

            switch (osVersion)
            {
                case OSVersion.WINXP:
                case OSVersion.WINVISTA:
                    MessageBox.Show("We are sorry to inform you that your operating system version is too low to run this program, please upgrade your operating system", "Extreme Starry Client Launcher");
                    //if (!IsNetFramework4Installed())
                    //{
                    //    Application.Run(new NETFramework4MissingMessageForm());
                    //    break;
                    //}

                    //RunXNA();
                    break;
                case OSVersion.WIN7:
                case OSVersion.WIN810:
                    W7And10Autorun();
                    break;
                case OSVersion.UNIX:
                case OSVersion.UNKNOWN:
                    RunFNA();
                    break;
            }
        }

        private static void RunUpdate()
        {
            var log = new StringBuilder();
            try
            {
                const string UPDATE_FOLDER = "Update";
                var update = new DirectoryInfo(UPDATE_FOLDER);
                if (!update.Exists)
                    return;

                WaitProcArr(Process.GetProcessesByName("clientdx.exe"));
                WaitProcArr(Process.GetProcessesByName("clientfna.exe"));
                WaitProcArr(Process.GetProcessesByName("clientxna.exe"));
                WaitProcArr(Process.GetProcessesByName("clientogl.exe"));

                foreach (var f in update.GetFiles())
                {
                    log.AppendLine($"Move {f.FullName} to {Path.Combine(update.Parent.FullName, f.Name)}");
                    f.MoveTo(Path.Combine(update.Parent.FullName, f.Name));
                }
                foreach (var f in update.GetDirectories())
                {
                    log.AppendLine($"Move {f.FullName} to {Path.Combine(update.Parent.FullName, f.Name)}");
                    f.MoveTo(Path.Combine(update.Parent.FullName, f.Name));
                }
                update.Delete();

                void WaitProcArr(Process[] procs)
                {
                    if (procs.Length == 0)
                        return;
                    
                    foreach (var proc in procs)
                        proc.WaitForExit();
                }
            }
            catch (Exception e)
            {
                MessageBox.Show($"{log} : {e}", "Update Error");
            }
        }

        [Obsolete]
        private static void RunXNA()
        {
            if (!IsXNAFramework4Installed())
            {
                Application.Run(new XNAFrameworkMissingMessageForm());
                return;
            }

            StartProcess(RESOURCES + Path.DirectorySeparatorChar + "clientxna.exe");
        }
        [Obsolete]
        private static void RunOGL()
        {
            StartProcess(RESOURCES + Path.DirectorySeparatorChar + "clientogl.exe");
        }

        private static void RunFNA()
        {
            StartProcess(Path.GetFullPath(Path.Combine(RESOURCES, "clientfna.exe")));
        }

        private static void RunDX()
        {
            if (GetOperatingSystemVersion() == OSVersion.WIN7)
            {
                if (!IsNetFramework45Installed())
                {
                    Application.Run(new NETFramework45MissingMessageForm());
                    return;
                }
            }

            StartProcess(Path.GetFullPath(Path.Combine(RESOURCES, "clientdx.exe")));
        }

        private static void W7And10Autorun()
        {
            string dxFailFilePath = Path.GetFullPath(Path.Combine("Client", ".dxfail"));

            if (File.Exists(dxFailFilePath))
            {
                //if (IsXNAFramework4Installed())
                //{
                //    RunXNA();
                //    return;
                //}

                DialogResult dr = new IncompatibleGPUMessageForm().ShowDialog();
                if (dr == DialogResult.No)
                {
                    File.Delete(dxFailFilePath);
                    RunDX();
                }
                else if (dr == DialogResult.Yes)
                {
                    RunFNA();
                }

                return;
            }

            RunDX();
        }

        private static void StartProcess(string relativePath)
        {
            string completeFilePath = Environment.CurrentDirectory + Path.DirectorySeparatorChar + relativePath;

            if (!File.Exists(completeFilePath))
            {
                MessageBox.Show("Main client executable (" + relativePath + ") not found!",
                    "Client Launcher Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                Process proc = Process.Start(completeFilePath);
                proc.WaitForExit();
                if (proc.ExitCode != 0)
                {
                    MessageBox.Show("Extreme Starry Client has experienced abnormal exit behavior.", "Extreme Starry Client Launcher Error");
                }
            }
            catch (Win32Exception ex)
            {
                if (ex.NativeErrorCode == ERROR_CANCELLED_CODE)
                {
                    MessageBox.Show("Unable to launch the main client. It could be blocked by Windows SmartScreen."
                        + Environment.NewLine + Environment.NewLine +
                        "Please try to launch the following file manually: " + relativePath
                        + Environment.NewLine + Environment.NewLine +
                        "If the client still doesn't run, please contact the mod's authors for support.",
                        "Client Launcher Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
        }

        private static OSVersion GetOperatingSystemVersion()
        {
            Version osVersion = Environment.OSVersion.Version;

            if (Environment.OSVersion.Platform == PlatformID.Win32Windows)
                return OSVersion.WIN9X;

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                if (osVersion.Major < 5)
                    return OSVersion.UNKNOWN;

                if (osVersion.Major == 5)
                    return OSVersion.WINXP;

                if (osVersion.Minor > 1)
                    return OSVersion.WIN810;
                else if (osVersion.Minor == 0)
                    return OSVersion.WINVISTA;

                return OSVersion.WIN7;
            }

            int p = (int)Environment.OSVersion.Platform;

            if ((p == 4) || (p == 6) || (p == 128))
            {
                return OSVersion.UNIX;
            }

            return OSVersion.UNKNOWN;
        }
        [Obsolete]
        private static bool IsNetFramework4Installed()
        {
            try
            {
                RegistryKey ndpKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\v4\\Full");

                string installValue = ndpKey.GetValue("Install").ToString();

                if (installValue == "1")
                    return true;
            }
            catch
            {

            }

            return false;
        }

        private static bool IsNetFramework45Installed()
        {
            try
            {
                RegistryKey ndpKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\v4\\Full");

                string installValue = ndpKey.GetValue("Release").ToString();

                // https://docs.microsoft.com/en-us/dotnet/framework/migration-guide/how-to-determine-which-versions-are-installed#net_d
                if (Convert.ToInt32(installValue) >= NET_FRAMEWORK_45_RELEASE_KEY)
                    return true;
            }
            catch
            {

            }

            return false;
        }
        [Obsolete]
        private static bool IsXNAFramework4Installed()
        {
            try
            {
                RegistryKey xnaKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\XNA\\Framework\\v4.0");

                string installValue = xnaKey.GetValue("Installed").ToString();

                if (installValue == "1")
                    return true;
            }
            catch
            {

            }

            return false;
        }
    }

    enum OSVersion
    {
        UNKNOWN,
        WIN9X,
        WINXP,
        WINVISTA,
        WIN7,
        WIN810,
        UNIX
    }
}
