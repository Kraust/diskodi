using MahApps.Metro.Controls;
using System;
using System.Diagnostics;
using System.Net;
using System.Timers;
using System.Windows;
using IniParser;
using System.IO;
using IniParser.Model;

namespace RPC_Link {

  public partial class MainWindow : MetroWindow {

    const string sDefaultKodiAddr = "localhost";
    const string sDefaultKodiPort = "8080";

    public MainWindow() {
      InitializeComponent();
      mwMainWindow.Title += " v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.Major.ToString() + 
        "." + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.Minor.ToString();
      Init_Settings();
    }

    protected override void OnStateChanged(EventArgs e) {
      if (WindowState == WindowState.Minimized)
        this.Hide();
      base.OnStateChanged(e);
    }

    private void Init_Settings() {
      FileIniDataParser iniParser = new FileIniDataParser();
      IniData iniData = new IniData();

      Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RPC-Link"));

      try {
        iniData = iniParser.ReadFile(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RPC-Link", "settings.ini"));

        tbKodiAddr.Text = iniData["Settings"]["tbKodiAddr"];
        tbKodiPort.Text = iniData["Settings"]["tbKodiPort"];
      }
      catch (Exception ex) {
        // new file...
        tbKodiAddr.Text = sDefaultKodiAddr;
        tbKodiPort.Text = sDefaultKodiPort;

        iniData["Settings"]["tbKodiAddr"] = tbKodiAddr.Text;
        iniData["Settings"]["tbKodiPort"] = tbKodiPort.Text;

        iniParser.WriteFile(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RPC-Link", "settings.ini"), iniData);
      }
    }

    // Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RPC-Link", "settings.ini")
    private void Save_Settings(object sender, RoutedEventArgs e) {
      FileIniDataParser iniParser = new FileIniDataParser();
      IniData iniData = new IniData();

      try {
        iniData = iniParser.ReadFile(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RPC-Link", "settings.ini"));
      }
      catch (Exception ex) {
        // new file...
      }

      iniData["Settings"]["tbKodiAddr"] = tbKodiAddr.Text;
      iniData["Settings"]["tbKodiPort"] = tbKodiPort.Text;

      iniParser.WriteFile(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RPC-Link", "settings.ini"), iniData);
    }

    private void Show_Window(object sender, RoutedEventArgs e) {
      this.Show();
      this.Activate();
    }
  }

  public partial class App : Application {

    static string KodiAddr = "localhost";
    static string KodiPort = "8080";

    private static string APPLICATION_ID = "381938792329904129";
    private static string TEST_URL = @"http://" + KodiAddr + ":" + KodiPort  + "/jsonrpc?request={%22jsonrpc%22:%20%222.0%22,%20%22method%22:%20%22Player.GetItem%22,%20%22params%22:%20{%20%22properties%22:%20[%22title%22,%20%22album%22,%20%22artist%22,%20%22season%22,%20%22episode%22,%20%22duration%22,%20%22showtitle%22,%20%22tvshowid%22,%20%22thumbnail%22,%20%22file%22,%20%22fanart%22,%20%22streamdetails%22],%20%22playerid%22:%201%20},%20%22id%22:%20%22VideoGetItem%22}";


    private void Application_Startup(object sender, StartupEventArgs e) {
      Timer tUpdate = new Timer(10000);
      tUpdate.Elapsed += HandleTimer;
      tUpdate.AutoReset = true;
      tUpdate.Start();
    }

    public static bool bEnable = false;
    public static bool bInitialized = false;
    DiscordRpc.EventHandlers handlers;
    DiscordRpc.RichPresence discordPresence;

    public static void Discord_RPC_Shutdown() {
      DiscordRpc.RichPresence discordPresence = new DiscordRpc.RichPresence();
      discordPresence.details = "";
      discordPresence.state = "";
      discordPresence.largeImageKey = "";

      DiscordRpc.UpdatePresence(ref discordPresence);
      DiscordRpc.Shutdown();
      bEnable = false;
      bInitialized = false;
    }

    private void HandleTimer(Object source, ElapsedEventArgs e) {
      string title = "";
      string episodeName = "";

      if (Process.GetProcessesByName("kodi").Length > 0) {
        Debug.WriteLine("Seen Kodi");
        bEnable = true;
      }
      else {
        if(bEnable) {
          Debug.WriteLine("Shutting down Discord RPC");
          Discord_RPC_Shutdown();
        }
        bEnable = false;
      }

      if (bEnable) {
        if (!bInitialized) {
          Debug.WriteLine("Starting up Discord RPC");
          handlers = new DiscordRpc.EventHandlers();
          discordPresence = new DiscordRpc.RichPresence();
          DiscordRpc.Initialize(APPLICATION_ID, ref handlers, true, null);
          bInitialized = true;
        }

        using (WebClient wc = new WebClient()) {
          try {
            var json = wc.DownloadString(TEST_URL);
            dynamic parsed = Newtonsoft.Json.JsonConvert.DeserializeObject(json);

            title = parsed.result.item.showtitle;
            episodeName = parsed.result.item.title;

            if ((title == "") && (episodeName == "")) {
              title = "In Menus";
            }
          }
          catch {
            Debug.WriteLine("Unable to Connect to Remote Server");
          }
        }

        discordPresence.details = title;
        discordPresence.state = episodeName;
        discordPresence.largeImageKey = "diskodi_logo";
        DiscordRpc.UpdatePresence(ref discordPresence);

      }
    }

    private void Application_Exit(object sender, ExitEventArgs e) {
      Debug.WriteLine("Application_Exit");
      if (bEnable) {
        DiscordRpc.RichPresence discordPresence = new DiscordRpc.RichPresence();
        discordPresence.details = "";
        discordPresence.state = "";
        discordPresence.largeImageKey = "";

        DiscordRpc.UpdatePresence(ref discordPresence);
        DiscordRpc.Shutdown();
      }
    }
  }
}


