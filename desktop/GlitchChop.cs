// Glitch Chop desktop shell: a native Windows window hosting the sampler (glitch-chop.html)
// in WebView2. Everything ships inside the one .exe: the page, the WebView2 SDK assemblies and
// the native loader are embedded resources, unpacked to %LOCALAPPDATA%\GlitchChop on start.
// Written for the C# 5 compiler that ships with .NET Framework 4.x, so no newer syntax.
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

[assembly: AssemblyTitle("Glitch Chop")]
[assembly: AssemblyProduct("Glitch Chop")]
[assembly: AssemblyDescription("Vocal chop sampler and glitch melody generator")]
[assembly: AssemblyVersion("1.1.0.0")]
[assembly: AssemblyFileVersion("1.1.0.0")]

namespace GlitchChop
{
    static class Program
    {
        public static string AppDir;

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern IntPtr LoadLibrary(string path);

        [STAThread]
        static void Main()
        {
            AppDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GlitchChop");
            Directory.CreateDirectory(AppDir);

            // The WebView2 managed assemblies are embedded; hand them to the runtime on request.
            AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
            {
                byte[] b = ReadRes(new AssemblyName(e.Name).Name + ".dll");
                return b == null ? null : Assembly.Load(b);
            };

            try
            {
                // WebView2Loader.dll is native and must match the process architecture. Loading it by
                // full path first means the SDK's DllImport("WebView2Loader.dll") finds it already in memory.
                // PROCESSOR_ARCHITECTURE reflects this process (AMD64, x86, ARM64), not the machine
                string pa = (Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE") ?? "").ToUpperInvariant();
                string arch = pa == "ARM64" ? "arm64" : Environment.Is64BitProcess ? "x64" : "x86";
                string bin = Path.Combine(AppDir, "bin", arch);
                Directory.CreateDirectory(bin);
                string loader = Path.Combine(bin, "WebView2Loader.dll");
                byte[] dll = ReadRes("WebView2Loader." + arch + ".dll");
                if (!File.Exists(loader) || new FileInfo(loader).Length != dll.Length) File.WriteAllBytes(loader, dll);
                if (LoadLibrary(loader) == IntPtr.Zero) throw new Exception("Couldn't load WebView2Loader.dll (error " + Marshal.GetLastWin32Error() + ").");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Glitch Chop couldn't start.\n\n" + ex.Message, "Glitch Chop", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Run();
        }

        // Kept out of Main so MainForm (which touches WebView2 types) is only JIT-compiled
        // after the AssemblyResolve handler above is in place.
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Run()
        {
            Application.Run(new MainForm());
        }

        public static byte[] ReadRes(string name)
        {
            using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
            {
                if (s == null) return null;
                using (MemoryStream m = new MemoryStream())
                {
                    s.CopyTo(m);
                    return m.ToArray();
                }
            }
        }
    }

    class MainForm : Form
    {
        const string Host = "glitchchop.example";
        readonly WebView2 web;

        [DllImport("dwmapi.dll")]
        static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        public MainForm()
        {
            Text = "Glitch Chop";
            ClientSize = new Size(1280, 880);
            MinimumSize = new Size(760, 560);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(0x16, 0x11, 0x1a);
            try
            {
                using (MemoryStream s = new MemoryStream(Program.ReadRes("app.ico"))) Icon = new Icon(s);
            }
            catch { }

            web = new WebView2();
            web.Dock = DockStyle.Fill;
            web.DefaultBackgroundColor = BackColor;
            Controls.Add(web);
            Load += async (s, e) => await InitAsync();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            int on = 1; // DWMWA_USE_IMMERSIVE_DARK_MODE: dark title bar to match the app
            try { DwmSetWindowAttribute(Handle, 20, ref on, 4); } catch { }
        }

        async Task InitAsync()
        {
            try
            {
                string site = Path.Combine(Program.AppDir, "app");
                Directory.CreateDirectory(site);
                File.WriteAllBytes(Path.Combine(site, "index.html"), Program.ReadRes("index.html"));

                CoreWebView2EnvironmentOptions opts = new CoreWebView2EnvironmentOptions("--autoplay-policy=no-user-gesture-required");
                CoreWebView2Environment env = await CoreWebView2Environment.CreateAsync(null, Path.Combine(Program.AppDir, "WebView2Data"), opts);
                await web.EnsureCoreWebView2Async(env);

                CoreWebView2 cw = web.CoreWebView2;
                cw.Settings.AreDevToolsEnabled = false;
                cw.Settings.AreDefaultContextMenusEnabled = false;
                cw.Settings.IsStatusBarEnabled = false;
                cw.Settings.IsZoomControlEnabled = false;
                cw.Settings.AreBrowserAcceleratorKeysEnabled = false;
                cw.Settings.IsSwipeNavigationEnabled = false;
                cw.DocumentTitleChanged += (s, e) => { Text = "Glitch Chop"; };
                cw.DownloadStarting += OnDownloadStarting;
                cw.NewWindowRequested += (s, e) =>
                {
                    e.Handled = true; // links never open a second WebView window; real web links go to the default browser
                    if (e.Uri.StartsWith("https://") || e.Uri.StartsWith("http://")) OpenExternal(e.Uri);
                };
                cw.NavigationStarting += (s, e) =>
                {
                    if (!e.Uri.StartsWith("https://" + Host + "/") && !e.Uri.StartsWith("blob:") && !e.Uri.StartsWith("data:"))
                    {
                        e.Cancel = true;
                        if (e.Uri.StartsWith("https://") || e.Uri.StartsWith("http://")) OpenExternal(e.Uri);
                    }
                };
                cw.SetVirtualHostNameToFolderMapping(Host, site, CoreWebView2HostResourceAccessKind.Deny);
                cw.Navigate("https://" + Host + "/index.html");
            }
            catch (WebView2RuntimeNotFoundException)
            {
                DialogResult r = MessageBox.Show(
                    "Glitch Chop needs the Microsoft Edge WebView2 Runtime, which is missing on this PC.\n\nOpen Microsoft's download page now?",
                    "Glitch Chop", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (r == DialogResult.Yes) OpenExternal("https://developer.microsoft.com/microsoft-edge/webview2/");
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Glitch Chop couldn't start.\n\n" + ex.Message, "Glitch Chop", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }
        }

        // Exports arrive as page downloads; turn each into a normal Windows "Save as" dialog.
        void OnDownloadStarting(object sender, CoreWebView2DownloadStartingEventArgs e)
        {
            CoreWebView2Deferral deferral = e.GetDeferral();
            BeginInvoke(new Action(() =>
            {
                try
                {
                    using (SaveFileDialog dlg = new SaveFileDialog())
                    {
                        string name = Path.GetFileName(e.ResultFilePath);
                        bool zip = name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);
                        dlg.Title = zip ? "Save chops" : "Save melody";
                        dlg.FileName = name;
                        dlg.Filter = zip ? "Zip archive (*.zip)|*.zip" : "WAV audio (*.wav)|*.wav";
                        dlg.InitialDirectory = Path.GetDirectoryName(e.ResultFilePath);
                        if (dlg.ShowDialog(this) == DialogResult.OK)
                        {
                            e.ResultFilePath = dlg.FileName;
                            e.Handled = true; // skip the browser-style download flyout
                        }
                        else e.Cancel = true;
                    }
                }
                finally { deferral.Complete(); }
            }));
        }

        static void OpenExternal(string url)
        {
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); } catch { }
        }
    }
}
