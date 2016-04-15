using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Cors;
using Microsoft.Owin.Hosting;
using Owin;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace WPFServer
{
    /// <summary>
    /// WPF host for a SignalR server. The host can stop and start the SignalR
    /// server, report errors when trying to start the server on a URI where a
    /// server is already being hosted, and monitor when clients connect and disconnect. 
    /// The hub used in this server is a simple echo service, and has the same 
    /// functionality as the other hubs in the SignalR Getting Started tutorials.
    /// For simplicity, MVVM will not be used for this sample.
    /// </summary>
    public partial class MainWindow : Window
    {
        public IDisposable SignalR { get; set; }
        const string ServerURI = "http://localhost:8080";
        List<String> ClientList { get; set;}
        IHubContext context { get; set; }
        public MainWindow()
        {
            ClientList = new List<string>();
            InitializeComponent();
        }

        /// <summary>
        /// Calls the StartServer method with Task.Run to not
        /// block the UI thread. 
        /// </summary>
        private void ButtonStart_Click(object sender, RoutedEventArgs e)
        {
            WriteToConsole("Starting server...");
            ButtonStart.IsEnabled = false;            
            Task.Run(() => StartServer());
        }

        /// <summary>
        /// Stops the server and closes the form. Restart functionality omitted
        /// for clarity.
        /// </summary>
        private void ButtonStop_Click(object sender, RoutedEventArgs e)
        {
            SignalR.Dispose();
            Close();
        }

        /// <summary>
        /// Starts the server and checks for error thrown when another server is already 
        /// running. This method is called asynchronously from Button_Start.
        /// </summary>
        private void StartServer()
        {
            try
            {
                SignalR = WebApp.Start(ServerURI);
                context= GlobalHost.ConnectionManager.GetHubContext<MyHub>();
            }
            catch (TargetInvocationException)
            {
                WriteToConsole("A server is already running at " + ServerURI);
                this.Dispatcher.Invoke(() => ButtonStart.IsEnabled = true);
                return;
            }
            this.Dispatcher.Invoke(() => ButtonStop.IsEnabled = true);
            WriteToConsole("Server started at " + ServerURI);
        }
        ///This method adds a line to the RichTextBoxConsole control, using Dispatcher.Invoke if used
        /// from a SignalR hub thread rather than the UI thread.
        public void WriteToConsole(String message)
        {
            if (!(RichTextBoxConsole.CheckAccess()))
            {
                this.Dispatcher.Invoke(() =>
                    WriteToConsole(message)
                );
                return;
            }
            RichTextBoxConsole.AppendText(message + "\r");
        }

        public void AddClient(String ConnectionId)
        {
            try
            {
                ButtonShowClients.IsEnabled = true;
                ButtonExe.IsEnabled = true;
                ClientList.Add(ConnectionId);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }            
        }
        public void RemoveClient(String ConnectionId)
        {
            try
            {
                if (ClientList.Contains(ConnectionId))
                {
                    ClientList.Remove(ConnectionId);
                }
                if (ClientList.Count == 0)
                {
                    ButtonShowClients.IsEnabled = false;
                    ButtonExe.IsEnabled = false;
                }          
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void ButtonShowClients_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                String message = "";
                foreach(String s in ClientList)
                {
                    message += s + "\n";
                }
                WriteToConsole(message);
                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }

        private void ButtonExe_Click(object sender, RoutedEventArgs e)
        {
            //context.Clients.All.ExePaint();
            //context.Clients.Client(ClientList[0]).addMessage("sevidor","hola");

            context.Clients.Client(ClientList[0]).ServerOrderDisconnect();
        }
    }
    /// <summary>
    /// Used by OWIN's startup process. 
    /// </summary>
    class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.UseCors(CorsOptions.AllowAll);
            app.MapSignalR();
        }
    }
    /// <summary>
    /// Echoes messages sent using the Send message by calling the
    /// addMessage method on the client. Also reports to the console
    /// when clients connect and disconnect.
    /// </summary>
    public class MyHub : Hub
    {
        public void Send(string name, string message)
        {
            Clients.All.addMessage(name, message);
            Application.Current.Dispatcher.Invoke(() =>
                ((MainWindow)Application.Current.MainWindow).WriteToConsole(name+": "+message));
        }
        public override Task OnConnected()
        {
            //Use Application.Current.Dispatcher to access UI thread from outside the MainWindow class
            Application.Current.Dispatcher.Invoke(() => 
                ((MainWindow)Application.Current.MainWindow).WriteToConsole("Client connected: " + Context.ConnectionId));
            UserHandler.ConnectedIds.Add(Context.ConnectionId);
            //Save client
            Application.Current.Dispatcher.Invoke(() =>
                ((MainWindow)Application.Current.MainWindow).AddClient(Context.ConnectionId));

            return base.OnConnected();
        }
        public override Task OnDisconnected()
        {
            //Use Application.Current.Dispatcher to access UI thread from outside the MainWindow class
            Application.Current.Dispatcher.Invoke(() => 
                ((MainWindow)Application.Current.MainWindow).WriteToConsole("Client disconnected: " + Context.ConnectionId));
            UserHandler.ConnectedIds.Remove(Context.ConnectionId);
            //Remove client
            Application.Current.Dispatcher.Invoke(() =>
                ((MainWindow)Application.Current.MainWindow).RemoveClient(Context.ConnectionId));

            return base.OnDisconnected();
        }
    }
    public static class UserHandler
    {
        public static HashSet<string> ConnectedIds = new HashSet<string>();
    }
}
