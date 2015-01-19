using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Windows;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Notification;
using Microsoft.Phone.Shell;
using Newtonsoft.Json;
using System.Text;

namespace WPCordovaClassLib.Cordova.Commands
{
    public class PushPlugin : BaseCommand
    {
        private const string InvalidRegistrationError = "Unable to open a channel with the specified name. The most probable cause is that you have already registered a channel with a different name. Call unregister(old-channel-name) or uninstall and redeploy your application.";
        private const string MissingChannelError = "Couldn't find a channel with the specified name.";
        private Options pushOptions;
        HttpNotificationChannel pushChannel = null;
        public void register(string options)
        {
            try
            {
                if(!TryDeserializeOptions(options, out this.pushOptions))
                {
                    this.DispatchCommandResult(new PluginResult(PluginResult.Status.JSON_EXCEPTION));
                    return;
                }

                pushChannel = HttpNotificationChannel.Find(this.pushOptions.ChannelName);
                if(pushChannel == null)
                {
                    pushChannel = new HttpNotificationChannel(this.pushOptions.ChannelName);
                    pushChannel.ChannelUriUpdated += new EventHandler<NotificationChannelUriEventArgs>(PushChannel_ChannelUriUpdated);
                    pushChannel.ErrorOccurred += new EventHandler<NotificationChannelErrorEventArgs>(PushChannel_ErrorOccurred);

                    // Register for this notification only if you need to receive the notifications while your application is running.
                    pushChannel.ShellToastNotificationReceived += new EventHandler<NotificationEventArgs>(PushChannel_ShellToastNotificationReceived);

                    try
                    {
                        pushChannel.Open();
                    }
                    catch(InvalidOperationException)
                    {
                        this.DispatchCommandResult(new PluginResult(PluginResult.Status.ERROR, InvalidRegistrationError));
                        return;
                    }
                }
                else
                {
                    // The channel was already open, so just register for all the events.
                    pushChannel.ChannelUriUpdated += new EventHandler<NotificationChannelUriEventArgs>(PushChannel_ChannelUriUpdated);
                    pushChannel.ErrorOccurred += new EventHandler<NotificationChannelErrorEventArgs>(PushChannel_ErrorOccurred);

                    // Register for this notification only if you need to receive the notifications while your application is running.
                    pushChannel.ShellToastNotificationReceived += new EventHandler<NotificationEventArgs>(PushChannel_ShellToastNotificationReceived);

                    var result = new RegisterResult
                    {
                        ChannelName = this.pushOptions.ChannelName,
                        Uri = pushChannel.ChannelUri == null ? String.Empty: pushChannel.ChannelUri.ToString()
                    };

                    this.DispatchCommandResult(new PluginResult(PluginResult.Status.OK, result));
                }
                // Bind this new channel for toast events.
                if(pushChannel.IsShellToastBound)
                    Console.WriteLine("Already Bound to Toast");
                else
                    pushChannel.BindToShellToast();
                if(pushChannel.IsShellTileBound)
                    Console.WriteLine("Already Bound to Tile");
                else
                    pushChannel.BindToShellTile();
            }
            catch(Exception ex)
            {
                this.DispatchCommandResult(new PluginResult(PluginResult.Status.ERROR, ex.Message));
            }

        }

        public void unregister(string options)
        {
            Options unregisterOptions;
            if(!TryDeserializeOptions(options, out unregisterOptions))
            {
                this.DispatchCommandResult(new PluginResult(PluginResult.Status.JSON_EXCEPTION));
                return;
            }
            var pushChannel = HttpNotificationChannel.Find(unregisterOptions.ChannelName);
            if(pushChannel != null)
            {
                pushChannel.UnbindToShellTile();
                pushChannel.UnbindToShellToast();
                pushChannel.Close();
                pushChannel.Dispose();
                this.DispatchCommandResult(new PluginResult(PluginResult.Status.OK, "Channel " + unregisterOptions.ChannelName + " is closed!"));
            }
            else
            {
                this.DispatchCommandResult(new PluginResult(PluginResult.Status.ERROR, MissingChannelError));
            }
        }

        void PushChannel_ChannelUriUpdated(object sender, NotificationChannelUriEventArgs e)
        {
            // return uri to js
            var result = new RegisterResult
            {
                ChannelName = this.pushOptions.ChannelName,
                Uri = e.ChannelUri == null ? String.Empty: e.ChannelUri.ToString()
            };
            this.ExecuteCallback(this.pushOptions.UriChangedCallback, JsonConvert.SerializeObject(result));
        }

        void PushChannel_ErrorOccurred(object sender, NotificationChannelErrorEventArgs e)
        {
            // call error handler and return uri
            var err = new RegisterError
            {
                Code = e.ErrorCode.ToString(),
                Message = e.Message
            };
            this.ExecuteCallback(this.pushOptions.ErrorCallback, JsonConvert.SerializeObject(err));
        }

        void PushChannel_ShellToastNotificationReceived(object sender, NotificationEventArgs e)
        {
            StringBuilder message = new StringBuilder();
            string relativeUri = string.Empty;

            message.AppendFormat("Received New Messages at {0}:\n", DateTime.Now.ToShortTimeString());

            // Parse out the information that was part of the message.
            foreach(string key in e.Collection.Keys)
            {
                if(string.Compare(
                    key,
                    "wp:Param",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.CompareOptions.IgnoreCase) == 0)
                {
                    relativeUri = e.Collection[key];
                }
                else
                {
                    message.AppendFormat("{0} \n", e.Collection[key]);
                }
            }

            // Display a dialog of all the fields in the toast.
            Deployment.Current.Dispatcher.BeginInvoke(() => MessageBox.Show(message.ToString()));
        }

        void ExecuteCallback(string callback, string callbackResult)
        {
            try
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    PhoneApplicationFrame frame;
                    PhoneApplicationPage page;
                    CordovaView cView;

                    if(TryCast(Application.Current.RootVisual, out frame) &&
                        TryCast(frame.Content, out page) &&
                        TryCast(page.FindName("CordovaView"), out cView))
                    {
                        cView.Browser.Dispatcher.BeginInvoke(() =>
                        {
                            try
                            {
                                cView.Browser.InvokeScript("execScript", callback + "(" + callbackResult + ")");
                                Debug.WriteLine(callbackResult);
                            }
                            catch(Exception ex)
                            {
                                Debug.WriteLine("ERROR: Exception in InvokeScriptCallback :: " + ex.Message);
                            }
                        });
                    }
                });
            }
            catch(Exception ex)
            {
                this.ExecuteCallback(this.pushOptions.UriChangedCallback, ex.Message);
            }

        }
        static bool TryDeserializeOptions<T>(string options, out T result) where T : class
        {
            result = null;
            try
            {
                var args = JsonConvert.DeserializeObject<string[]>(options);
                result = JsonConvert.DeserializeObject<T>(args[0]);
                return true;
            }
            catch
            {
                return false;
            }
        }

        static bool TryCast<T>(object obj, out T result) where T : class
        {
            result = obj as T;
            return result != null;
        }

        [DataContract]
        public class Options
        {
            [DataMember(Name = "channelName", IsRequired = true)]
            public string ChannelName { get; set; }

            [DataMember(Name = "ecb", IsRequired = false)]
            public string NotificationCallback { get; set; }

            [DataMember(Name = "errcb", IsRequired = false)]
            public string ErrorCallback { get; set; }

            [DataMember(Name = "uccb", IsRequired = false)]
            public string UriChangedCallback { get; set; }
        }

        [DataContract]
        public class RegisterResult
        {
            [DataMember(Name = "uri", IsRequired = true)]
            public string Uri { get; set; }

            [DataMember(Name = "channel", IsRequired = true)]
            public string ChannelName { get; set; }
        }

        [DataContract]
        public class PushNotification
        {
            public PushNotification()
            {
                this.JsonContent = new Dictionary<string, object>();
            }

            [DataMember(Name = "jsonContent", IsRequired = true)]
            public IDictionary<string, object> JsonContent { get; set; }

            [DataMember(Name = "type", IsRequired = true)]
            public string Type { get; set; }
        }

        [DataContract]
        public class RegisterError
        {
            [DataMember(Name = "code", IsRequired = true)]
            public string Code { get; set; }

            [DataMember(Name = "message", IsRequired = true)]
            public string Message { get; set; }
        }
    }
}