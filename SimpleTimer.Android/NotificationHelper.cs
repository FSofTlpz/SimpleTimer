using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Widget;
using AndroidX.Core.App;
using System;
using System.Collections.Generic;
using Xamarin.Essentials;

namespace SimpleTimer.Droid {

   /// <summary>
   /// zum einfacheren Umgang mit den Notifications
   /// </summary>
   class NotificationHelper {

      /// <summary>
      /// ID's der Kanäle
      /// </summary>
      public const string NOTIFICATION_CHANNELID_SUMMARY = "INFO";
      public const string NOTIFICATION_CHANNELID_ALARM = "ALARM";
      public const string NOTIFICATION_CHANNELID_ALARMING = "ALARMING";

      /// <summary>
      /// für die Daten die per Intent weitergereicht werden
      /// </summary>
      const string APPINTENT_EXTRA_TYPE = "type";
      const string APPINTENT_EXTRA_TITLE = "title";
      const string APPINTENT_EXTRA_TEXT = "text";
      const string APPINTENT_EXTRA_NO = "no";

      enum AppIntentType {
         unknown = 1234,
         SummaryTap,
         AlarmTap,
         AlarmCancel,
         AlarmingTap,
         AlarmingClose,
      }



      struct ChannelDefinition {
         public string Name;
         public int NotificationID;
         public int Icon;
      }

      /// <summary>
      /// Basis-Def. der verwendeten Kanäle
      /// </summary>
      readonly static Dictionary<string, ChannelDefinition> ChannelDefs = new Dictionary<string, ChannelDefinition> {
            {
               NOTIFICATION_CHANNELID_SUMMARY,
               new ChannelDefinition() {
                  Name = "Wecker",
                  Icon = Resource.Mipmap.clock,
                  NotificationID = 1000,
               }
            },
            {
               NOTIFICATION_CHANNELID_ALARM,
               new ChannelDefinition() {
                  Name = "Alarm",
                  Icon = Resource.Mipmap.clock,
                  NotificationID = 2000,
               }
            },
            {
               NOTIFICATION_CHANNELID_ALARMING,
               new ChannelDefinition() {
                  Name = "Alarmierung",
                  Icon = Resource.Mipmap.ringingclock,
                  NotificationID = 3000,
               }
            },
         };

      const string NOTIFICATIONGROUP = "MyAlarms";


      class BaseAppNotificationChannel {

         /// <summary>
         /// Name des Kanals (öffentlich sichtbar)
         /// </summary>
         public string ChannelName { get; protected set; }

         /// <summary>
         /// ID des Kanals
         /// </summary>
         public string ChannelID { get; protected set; }

         /// <summary>
         /// Basis-ID für die Notifications dieses Kanals
         /// </summary>
         public int NotificationID { get; protected set; }

         /// <summary>
         /// Ist Kanal schon registriert?
         /// </summary>
         public bool ChannelIsRegistered { get; protected set; }


         public BaseAppNotificationChannel(string channelID, string channelname, int notificationID) {
            ChannelID = channelID;
            ChannelName = channelname;
            NotificationID = notificationID;
            ChannelIsRegistered = false;
         }

         /// <summary>
         /// erzeugt einen <see cref="NotificationChannel"/> und registriert ihn im OS
         /// </summary>
         /// <param name="notificationManager"></param>
         /// <returns></returns>
         public NotificationChannel CreateAndRegisterChannel(NotificationManager notificationManager) {
            if (notificationManager != null) {
               notificationManager.DeleteNotificationChannel(ChannelID);

               NotificationChannel notificationChannel = createNotificationChannel();

               notificationManager.CreateNotificationChannel(notificationChannel);  // Kanal wird im System registriert
               ChannelIsRegistered = true;
               return notificationChannel;
            }
            return null;
         }

         /* NotificationImportance:
            IMPORTANCE_MAX          Unused.
            IMPORTANCE_HIGH         Higher notification importance: shows everywhere, makes noise and peeks. May use full screen intents.
            IMPORTANCE_DEFAULT      Default notification importance: shows everywhere, makes noise, but does not visually intrude.
            IMPORTANCE_LOW          Low notification importance: Shows in the shade, and potentially in the status bar (see shouldHideSilentStatusBarIcons()), 
                                    but is not audibly intrusive.
            IMPORTANCE_MIN          Min notification importance: only shows in the shade, below the fold. This should not be used with Service#startForeground(int, Notification) 
                                    since a foreground service is supposed to be something the user cares about so it does not make semantic sense to mark its notification as 
                                    minimum importance. If you do this as of Android version Build.VERSION_CODES.O, the system will show a higher-priority notification about 
                                    your app running in the background.
            IMPORTANCE_NONE         A notification with no importance: does not show in the shade.
            IMPORTANCE_UNSPECIFIED  Value signifying that the user has not expressed an importance. This value is for persisting preferences, and should never be associated with 
                                    an actual notification.
          */

         // NotificationManager#IMPORTANCE_DEFAULT should have a sound. Only modifiable before the channel is submitted to 

         protected virtual NotificationChannel createNotificationChannel(NotificationImportance notificationImportance = NotificationImportance.Low) {
            NotificationChannel notificationChannel = new NotificationChannel(ChannelID,
                                                                              ChannelName,
                                                                              notificationImportance);
            return notificationChannel;
         }

         protected virtual NotificationCompat.Builder createNotificationBuilder(string title = null,
                                                                                string text = null,
                                                                                int notificationicon = -1) {
            NotificationCompat.Builder builder = new NotificationCompat.Builder(appcontext, ChannelID)
                               .SetOngoing(false);    // Set whether this is an "ongoing" notification. ???
                                                      //.SetProgress(100, 35, false)   // Set the progress this notification represents.
                                                      //.SetUsesChronometer(true)      // Show the Notification When field as a stopwatch. 
                                                      //.SetShowWhen(true)             // Control whether the timestamp set with setWhen is shown in the content view. 
                                                      //.SetWhen(?)                    // Add a timestamp pertaining to the notification(usually the time the event occurred). 
                                                      //.SetSound(alarmSound)          // This method was deprecated in API level 26 (BuildVersionCodes.O). use NotificationChannel#setSound(Uri, AudioAttributes) instead.
                                                      //.SetOnlyAlertOnce(true)

            if (title != null)
               builder.SetContentTitle(title);                     // Set the first line of text in the platform notification template. 
            if (text != null)
               builder.SetContentText(text);                       // Set the second line of text in the platform notification template. 
            if (notificationicon > 0)
               builder.SetSmallIcon(notificationicon); // Resource.Drawable.ic_mtrl_chip_close_circle);

            return builder;
         }

         /// <summary>
         /// erzeugt eine einfache Notification für diesen Kanal
         /// </summary>
         /// <param name="title"></param>
         /// <param name="text"></param>
         /// <param name="no"></param>
         /// <returns></returns>
         public virtual Notification CreateNotification(string title = null, string text = null, int icon = -1) {
            if (!ChannelIsRegistered)
               throw new Exception("Der NotificationChannel '" + ChannelName + "' ist noch nicht registriert.");

            NotificationCompat.Builder builder = createNotificationBuilder(title, text, icon);

            return builder.Build();
         }

         protected Intent getIntent4MainActivity(Dictionary<string, object> intentextras = null) {
            var intent = new Intent(appcontext, typeof(MainActivity));

            // https://developer.android.com/reference/android/content/Intent#FLAG_ACTIVITY_SINGLE_TOP
            intent.AddFlags(ActivityFlags.SingleTop);       // If set, the activity will not be launched if it is already running at the top of the history stack.

            // Two Intents are the same only when their    action, data, type, identity, class and categories    are all the same, 
            // not including the extra that we always put information into.

            if (intentextras != null)
               foreach (var item in intentextras) {
                  if (item.Value is string)
                     intent.PutExtra(item.Key, item.Value as string);
                  else if (item.Value is int)
                     intent.PutExtra(item.Key, (int)item.Value);
                  else if (item.Value is bool)
                     intent.PutExtra(item.Key, (bool)item.Value);
               }
            return intent;
         }

      }

      class AppNotificationChannel4Summary : BaseAppNotificationChannel {
         
         readonly Dictionary<string, object> intentextras = new Dictionary<string, object>() {
            { APPINTENT_EXTRA_TYPE, (int)AppIntentType.SummaryTap },
         };


         public AppNotificationChannel4Summary(string channelID, string channelname, int notificationID) :
            base(channelID, channelname, notificationID) { }

         protected override NotificationChannel createNotificationChannel(NotificationImportance notificationImportance) {
            NotificationChannel notificationChannel = base.createNotificationChannel(notificationImportance);

            notificationChannel.SetSound(null, null);
            notificationChannel.EnableLights(false);         // Sets whether notifications posted to this channel should display notification lights, on devices that support that feature. 
            notificationChannel.LockscreenVisibility = NotificationVisibility.Public;  // Sets whether notifications posted to this channel appear on the lockscreen or not, and if so, whether they appear in a redacted form. 
            notificationChannel.EnableVibration(false);     // Sets whether notification posted to this channel should vibrate.
            notificationChannel.SetShowBadge(true);         // Sets whether notifications posted to this channel can appear as application icon badges in a Launcher. 

            return notificationChannel;
         }

         /// <summary>
         /// erzeugt für einen Kanal den Builder
         /// </summary>
         /// <param name="title"></param>
         /// <param name="text"></param>
         /// <param name="notificationicon"></param>
         /// <returns></returns>
         protected override NotificationCompat.Builder createNotificationBuilder(string title,
                                                                                 string text,
                                                                                 int notificationicon) {
            var pendingIntent4Tap = PendingIntent.GetActivity(appcontext,
                                                              (int)(DateTime.Now.Ticks & 0x8fffffff),
                                                              getIntent4MainActivity(intentextras),
                                                              PendingIntentFlags.UpdateCurrent);

            NotificationCompat.Builder builder = new NotificationCompat.Builder(appcontext, ChannelName)
                                  .SetNotificationSilent()
                                  .SetOngoing(true)                     // true -> kann vom Anwender nicht beseitigt werden (Set whether this is an "ongoing" notification.)
                                  .SetGroupSummary(true)                // Set this notification to be the group summary for a group of notifications. 
                                  .SetGroup(NOTIFICATIONGROUP)
                                  .SetShowWhen(false)
                                  .SetStyle(new NotificationCompat.BigTextStyle().SetSummaryText("Kurzzeitwecker"))
                                  .SetContentIntent(pendingIntent4Tap)       // Aktion für das Tippen auf die Notif.

                                  //.SetDefaults((int)NotificationDefaults.All)
                                  .SetPriority(NotificationCompat.PriorityMax)
                                  .SetCategory(NotificationCompat.CategoryAlarm)
                                  .SetFullScreenIntent(pendingIntent4Tap, true);

            // Building channel if API verion is 26 or above
            if (global::Android.OS.Build.VERSION.SdkInt >= BuildVersionCodes.O)
               builder.SetChannelId(ChannelID);

            if (title != null)
               builder.SetContentTitle(title);                     // Set the first line of text in the platform notification template. 
            if (text != null)
               builder.SetContentText(text);                       // Set the second line of text in the platform notification template. 
            if (notificationicon > 0)
               builder.SetSmallIcon(notificationicon);

            return builder;
         }

      }

      class AppNotificationChannel4Alarm : BaseAppNotificationChannel {
         
         readonly Dictionary<string, object> intentextras = new Dictionary<string, object>() {
            { APPINTENT_EXTRA_TYPE, (int)AppIntentType.AlarmTap },
            { APPINTENT_EXTRA_NO, 0 },
            { APPINTENT_EXTRA_TITLE, "" },
            { APPINTENT_EXTRA_TEXT, "" },
         };

         public AppNotificationChannel4Alarm(string channelID, string channelname, int notificationID) :
            base(channelID, channelname, notificationID) { }

         protected override NotificationChannel createNotificationChannel(NotificationImportance notificationImportance) {
            NotificationChannel notificationChannel = base.createNotificationChannel(NotificationImportance.High);

            notificationChannel.SetSound(null, null);
            notificationChannel.EnableLights(false);         // Sets whether notifications posted to this channel should display notification lights, on devices that support that feature. 
            notificationChannel.LockscreenVisibility = NotificationVisibility.Public;  // Sets whether notifications posted to this channel appear on the lockscreen or not, and if so, whether they appear in a redacted form. 
            notificationChannel.EnableVibration(false);     // Sets whether notification posted to this channel should vibrate.
            notificationChannel.SetShowBadge(true);         // Sets whether notifications posted to this channel can appear as application icon badges in a Launcher. 

            return notificationChannel;
         }

         /// <summary>
         /// erzeugt für einen Kanal den Builder
         /// </summary>
         /// <param name="title"></param>
         /// <param name="text"></param>
         /// <param name="notificationicon"></param>
         /// <returns></returns>
         protected override NotificationCompat.Builder createNotificationBuilder(string title,
                                                                                 string text,
                                                                                 int notificationicon) {
            intentextras[APPINTENT_EXTRA_TYPE] = (int)AppIntentType.AlarmTap;
            var pendingIntent4Tap = PendingIntent.GetActivity(
                                                   appcontext,
                                                   (int)(DateTime.Now.Ticks & 0x8fffffff),
                                                   getIntent4MainActivity(intentextras),
                                                   PendingIntentFlags.UpdateCurrent);

            intentextras[APPINTENT_EXTRA_TYPE] = (int)AppIntentType.AlarmCancel;
            var pendingIntent4CancelTap = PendingIntent.GetActivity(
                                                   appcontext,
                                                   (int)(DateTime.Now.Ticks & 0x8fffffff),
                                                   getIntent4MainActivity(intentextras),
                                                   PendingIntentFlags.UpdateCurrent);

            NotificationCompat.Builder builder = new NotificationCompat.Builder(appcontext, ChannelName)
                                  .SetNotificationSilent()
                                  .SetOngoing(true)
                                  .SetProgress(100, 0, false)                 // Set the progress this notification represents.
                                  .SetGroup(NOTIFICATIONGROUP)
                                  .SetShowWhen(false)
                                  .AddAction(new NotificationCompat.Action(0, "Ausschalten", pendingIntent4CancelTap))
                                  .SetContentIntent(pendingIntent4Tap)       // Aktion für das Tippen auf die Notif.

                                  //.SetDefaults((int)NotificationDefaults.All)
                                  .SetPriority(NotificationCompat.PriorityMax)
                                  .SetCategory(NotificationCompat.CategoryAlarm)
                                  .SetFullScreenIntent(pendingIntent4CancelTap, true)
                                  ;

            // Building channel if API verion is 26 or above
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
               builder.SetChannelId(ChannelID);

            if (title != null)
               builder.SetContentTitle(title);                     // Set the first line of text in the platform notification template. 
            if (text != null)
               builder.SetContentText(text);                       // Set the second line of text in the platform notification template. 
            if (notificationicon > 0)
               builder.SetSmallIcon(notificationicon);

            return builder;
         }

         /// <summary>
         /// erzeugt eine einfache Notification für diesen Kanal
         /// </summary>
         /// <param name="title"></param>
         /// <param name="text"></param>
         /// <param name="icon"></param>
         /// <param name="percent"></param>
         /// <returns></returns>
         public Notification CreateNotificationSpec(string title, string text, int icon, int percent, int no) {
            if (!ChannelIsRegistered)
               throw new Exception("Der NotificationChannel '" + ChannelName + "' ist noch nicht registriert.");

            intentextras[APPINTENT_EXTRA_NO] = no;
            intentextras[APPINTENT_EXTRA_TITLE] = title;
            intentextras[APPINTENT_EXTRA_TEXT] = text;

            NotificationCompat.Builder builder = createNotificationBuilder(title, text, icon);
            builder.SetProgress(100, percent, false);
            //builder.SetWhen(Java.Lang.JavaSystem.CurrentTimeMillis()); // Zeit ab dieser Aktualisierung wird angezeigt

            return builder.Build();
         }

      }

      class AppNotificationChannel4Alarming : BaseAppNotificationChannel {
         
         readonly Dictionary<string, object> intentextras = new Dictionary<string, object>() {
            { APPINTENT_EXTRA_TYPE, (int)AppIntentType.AlarmingTap },
            { APPINTENT_EXTRA_NO, 0 },
            { APPINTENT_EXTRA_TITLE, "" },
            { APPINTENT_EXTRA_TEXT, "" },
         };

         public AppNotificationChannel4Alarming(string channelID, string channelname, int notificationID) :
            base(channelID, channelname, notificationID) { }

         protected override NotificationChannel createNotificationChannel(NotificationImportance notificationImportance) {
            NotificationChannel notificationChannel = base.createNotificationChannel(NotificationImportance.High);

            //notificationChannel.SetSound(Android.Net.Uri.Parse("android.resource://" + appcontext.PackageName + "/" + Resource.Raw.MinAlarm),
            //                             new AudioAttributes.Builder()
            //                                      .SetUsage(AudioUsageKind.Alarm)
            //                                      .SetContentType(AudioContentType.Music)
            //                                      .Build());

            notificationChannel.EnableVibration(true);
            notificationChannel.ShouldVibrate();
            notificationChannel.SetVibrationPattern(new long[] { 100, 200, 300, 400, 500, 400, 300, 200, 400 });
            notificationChannel.EnableLights(true);         // Sets whether notifications posted to this channel should display notification lights, on devices that support that feature. 
            notificationChannel.ShouldShowLights();
            notificationChannel.LockscreenVisibility = NotificationVisibility.Public;  // Sets whether notifications posted to this channel appear on the lockscreen or not, and if so, whether they appear in a redacted form. 
            notificationChannel.SetShowBadge(true);         // Sets whether notifications posted to this channel can appear as application icon badges in a Launcher. 

            return notificationChannel;
         }

         /// <summary>
         /// erzeugt für einen Kanal den Builder
         /// </summary>
         /// <param name="title"></param>
         /// <param name="text"></param>
         /// <param name="notificationicon"></param>
         /// <returns></returns>
         protected override NotificationCompat.Builder createNotificationBuilder(string title,
                                                                                 string text,
                                                                                 int notificationicon) {
            intentextras[APPINTENT_EXTRA_TYPE] = (int)AppIntentType.AlarmingTap;
            var pendingIntent4Tap = PendingIntent.GetActivity(
                                                      appcontext,
                                                      (int)(DateTime.Now.Ticks & 0x8fffffff),
                                                      getIntent4MainActivity(intentextras),
                                                      PendingIntentFlags.CancelCurrent);

            intentextras[APPINTENT_EXTRA_TYPE] = (int)AppIntentType.AlarmingClose;
            var pendingIntent4Close = PendingIntent.GetActivity(
                                                      appcontext,
                                                      (int)(DateTime.Now.Ticks & 0x8fffffff),
                                                      getIntent4MainActivity(intentextras),
                                                      PendingIntentFlags.CancelCurrent);

            /*
Flag indicating that if the described PendingIntent already exists, the current one should be canceled before generating a new one. For use with getActivity(Context, int, Intent, int), getBroadcast(Context, int, Intent, int), and getService(Context, int, Intent, int).

You can use this to retrieve a new PendingIntent when you are only changing the extra data in the Intent; by canceling the previous pending intent, this ensures that only entities given the new data will be able to launch it. If this assurance is not an issue, consider FLAG_UPDATE_CURRENT.


            My suspicion is that, since the only thing changing in the Intent is the extras, the PendingIntent.getActivity(...) factory method is simply re-using the old intent as an optimization.
            */

            NotificationCompat.Builder builder = new NotificationCompat.Builder(appcontext, ChannelName)
                               /*
                               setFullScreenIntent (PendingIntent intent, boolean highPriority)
                               An intent to launch instead of posting the notification to the status bar. Only for use with extremely high-priority notifications 
                               demanding the user's immediate attention, such as an incoming phone call or alarm clock that the user has explicitly set to a particular time. 
                               If this facility is used for something else, please give the user an option to turn it off and use a normal notification, 
                               as this can be extremely disruptive.

                               On some platforms, the system UI may choose to display a heads-up notification, instead of launching this intent, while the user is using the device.

                               Parameters
                               intent         PendingIntent: The pending intent to launch.
                               highPriority   boolean: Passing true will cause this notification to be sent even if other notifications are suppressed. 
                               */

                               //.SetSound(Android.Net.Uri.Parse(soundurl)) depricated
                               .SetNotificationSilent()
                               .SetOngoing(true)                           // nicht durch "wegwischen" zu beenden
                               .SetDefaults((int)NotificationDefaults.All)
                               .AddAction(new NotificationCompat.Action(0, "Beenden", pendingIntent4Close))
                               .SetContentIntent(pendingIntent4Tap)       // Aktion für das Tippen auf die Notif.

                              .SetPriority(NotificationCompat.PriorityHigh)
                              .SetCategory(NotificationCompat.CategoryAlarm)
                              .SetFullScreenIntent(pendingIntent4Close, true)
                               ;

            // Building channel if API verion is 26 or above
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
               builder.SetChannelId(ChannelID);

            if (title != null)
               builder.SetContentTitle(title);                     // Set the first line of text in the platform notification template. 
            if (text != null)
               builder.SetContentText(text);                       // Set the second line of text in the platform notification template. 
            if (notificationicon > 0)
               builder.SetSmallIcon(notificationicon);

            return builder;
         }

         /// <summary>
         /// erzeugt eine einfache Notification für diesen Kanal
         /// </summary>
         /// <param name="title"></param>
         /// <param name="text"></param>
         /// <param name="icon"></param>
         /// <param name="no"></param>
         /// <returns></returns>
         public Notification CreateNotificationSpec(string title, string text, int icon, int no) {
            if (!ChannelIsRegistered)
               throw new Exception("Der NotificationChannel '" + ChannelName + "' ist noch nicht registriert.");

            intentextras[APPINTENT_EXTRA_NO] = no;
            intentextras[APPINTENT_EXTRA_TITLE] = title;
            intentextras[APPINTENT_EXTRA_TEXT] = text;

            return createNotificationBuilder(title, text, icon).Build();
         }
      }


      #region interne Funktionen

      private static readonly Context appcontext = global::Android.App.Application.Context;

      private static NotificationManager notificationManager = null;


      static NotificationHelper() {
         channels = new Dictionary<string, BaseAppNotificationChannel>();
         foreach (var item in ChannelDefs) {
            string id = item.Key;
            ChannelDefinition cd = item.Value;

            if (id == NOTIFICATION_CHANNELID_SUMMARY)
               channels.Add(id, new AppNotificationChannel4Summary(id, cd.Name, cd.NotificationID));
            else if (id == NOTIFICATION_CHANNELID_ALARM)
               channels.Add(id, new AppNotificationChannel4Alarm(id, cd.Name, cd.NotificationID));
            else if (id == NOTIFICATION_CHANNELID_ALARMING)
               channels.Add(id, new AppNotificationChannel4Alarming(id, cd.Name, cd.NotificationID));
         }
      }

      /// <summary>
      /// Liste der benötigten Kanäle
      /// </summary>
      static readonly Dictionary<string, BaseAppNotificationChannel> channels;

      /// <summary>
      /// Notification des angegebenen Kanals und der Nummer entfernen
      /// </summary>
      /// <param name="channelid"></param>
      /// <param name="no">bezogen auf die NotificationID des Kanals</param>
      static void removeNotification(string channelid, int no) {
         createAndRegisterChannels();
         notificationManager.Cancel(channels[channelid].NotificationID + no);
      }

      static void createAndRegisterChannels() {
         if (notificationManager == null) {
            notificationManager = appcontext.GetSystemService(Context.NotificationService) as NotificationManager;

            // Building channel if API verion is 26 or above
            if (global::Android.OS.Build.VERSION.SdkInt >= BuildVersionCodes.O)
               foreach (var item in channels)
                  item.Value.CreateAndRegisterChannel(notificationManager);
         }
      }

      static void changeChannelUI(string channelid) {
         createAndRegisterChannels();

         Intent intent = new Intent(Android.Provider.Settings.ActionChannelNotificationSettings);
         intent.PutExtra(Android.Provider.Settings.ExtraAppPackage, appcontext.PackageName);
         intent.PutExtra(Android.Provider.Settings.ExtraChannelId, channels[channelid].ChannelID);
         if ((Build.VERSION.SdkInt <= BuildVersionCodes.M) || (BuildVersionCodes.P <= Build.VERSION.SdkInt))
            intent.AddFlags(ActivityFlags.NewTask);
         appcontext.StartActivity(intent);
      }

      #endregion


      /// <summary>
      /// es kann nur diesen einen Controller und einen dazugehörigen Service geben (wird im Konstruktor von <see cref="ClockServiceCtrl"/> gesetzt)
      /// </summary>
      static public ClockServiceCtrl ClockServiceCtrl;


      #region spez. Notifications erzeugen, ändern und löschen

      /// <summary>
      /// Notification-ID für die Summen-Notification (wird im Service benötigt)
      /// </summary>
      public static int SummaryNotificationID {
         get {
            return ChannelDefs[NOTIFICATION_CHANNELID_SUMMARY].NotificationID;
         }
      }

      /// <summary>
      /// beruht auf der Änderung der Kanal-ID's
      /// </summary>
      public static void ResetAllChannels() {
         if (notificationManager != null) {
            string oldbaseid = ClockServiceCtrl.NotificationChannelBaseID;
            int id = (oldbaseid == "" ? 1000 : Convert.ToInt32(oldbaseid)) + 1;
            ClockServiceCtrl.NotificationChannelBaseID = id.ToString();

            foreach (var item in channels) {
               BaseAppNotificationChannel channel = item.Value;
               string channelid = channel.ChannelID;
               notificationManager.DeleteNotificationChannel(channelid);

               if (channel is AppNotificationChannel4Summary)
                  channel = new AppNotificationChannel4Summary(channelid,
                                                               ChannelDefs[channelid].Name,
                                                               ChannelDefs[channelid].NotificationID);
               else if (channel is AppNotificationChannel4Alarm)
                  channel = new AppNotificationChannel4Alarm(channelid,
                                                             ChannelDefs[channelid].Name,
                                                             ChannelDefs[channelid].NotificationID);
               else if (channel is AppNotificationChannel4Alarming)
                  channel = new AppNotificationChannel4Alarming(channelid,
                                                                ChannelDefs[channelid].Name,
                                                                ChannelDefs[channelid].NotificationID);

               channel.CreateAndRegisterChannel(notificationManager);
            }
         }
      }

      /// <summary>
      /// Gibt es Notifications für diesen Kanal?
      /// </summary>
      /// <param name="channelid"></param>
      /// <returns></returns>
      public static bool ExistsActiveNotifications(string channelid) {
         Android.Service.Notification.StatusBarNotification[] statusBarNotification = notificationManager.GetActiveNotifications();
         if (statusBarNotification != null) {
            foreach (var item in statusBarNotification) {
               Notification notification = item.Notification;
               if (notification.ChannelId == channelid) {
                  return true;
               }
            }
         }
         return false;
      }

      /// <summary>
      /// liefert die Liste aller aktiver Notification des Kanals mit der entsprechenden ID
      /// </summary>
      /// <param name="channelid"></param>
      /// <returns></returns>
      //   public static List<Notification> GetActiveNotifications(string channelid) {
      //   List<Notification> notifications = new List<Notification>();
      //   Android.Service.Notification.StatusBarNotification[] statusBarNotification = notificationManager.GetActiveNotifications();
      //   if (statusBarNotification != null) {
      //      foreach (var item in statusBarNotification) {
      //         Notification notification = item.Notification;
      //         if (notification.ChannelId == channelid)
      //            notifications.Add(notification);
      //      }
      //   }
      //   return notifications;
      //}

      /// <summary>
      /// liefert die Liste der "Nummern" aller aktiver Notification des Kanals mit der entsprechenden ID
      /// </summary>
      /// <param name="channelid"></param>
      /// <returns></returns>
      //public static List<int> GetNo4ActiveNotifications(string channelid) {
      //   List<Notification> notifications = GetActiveNotifications(channelid);
      //   List<int> nolst = new List<int>();
      //   foreach (Notification notification in notifications) {
      //      if (notification.Extras != null) {
      //         int no = notification.Extras.GetInt(NOTIFICATION_EXTRA_NO, -1);
      //         if (no >= 0)
      //            nolst.Add(no);
      //      }
      //   }
      //   return nolst;
      //}


      /// <summary>
      /// Erzeugung einer Summen-Notification (für StartForeground() des Service)
      /// </summary>
      /// <param name="title"></param>
      /// <param name="text"></param>
      /// <returns></returns>
      public static Notification CreateSummaryNotification(string title, string text) {
         createAndRegisterChannels();
         return channels[NOTIFICATION_CHANNELID_SUMMARY].CreateNotification(title,
                                                                            text,
                                                                            ChannelDefs[NOTIFICATION_CHANNELID_SUMMARY].Icon);
      }

      /// <summary>
      /// Aktualisierung der Summen-Notification
      /// </summary>
      /// <param name="title"></param>
      /// <param name="text"></param>
      public static void UpdateSummaryNotification(string title, string text) {
         createAndRegisterChannels();
         string channelid = NOTIFICATION_CHANNELID_SUMMARY;
         AppNotificationChannel4Summary channel = channels[channelid] as AppNotificationChannel4Summary;
         notificationManager.Notify(channel.NotificationID,
                                    channel.CreateNotification(title,
                                                               text,
                                                               ChannelDefs[channelid].Icon));
      }

      /// <summary>
      /// entfernt die Summen-Notification
      /// </summary>
      public static void RemoveSummaryNotification() {
         removeNotification(NOTIFICATION_CHANNELID_SUMMARY, 0);
      }


      /// <summary>
      /// Anzeige der Alarm-Notification
      /// </summary>
      /// <param name="no"></param>
      /// <param name="title"></param>
      /// <param name="text"></param>
      public static void ShowAlarmNotification(int no, string title, string text) {
         createAndRegisterChannels();
         notificationManager.Cancel(channels[NOTIFICATION_CHANNELID_ALARM].NotificationID + no);
         notifyAlarmNotification(no, title, text, 0);

         Global.MainActivity.RunOnUiThread(() => {
            Toast.MakeText(appcontext, "Wecker '" + title + "' gestartet", ToastLength.Short).Show();
         });
      }

      /// <summary>
      /// Aktualisierung der Alarm-Notification
      /// </summary>
      /// <param name="no"></param>
      /// <param name="title"></param>
      /// <param name="text"></param>
      /// <param name="percent"></param>
      public static void UpdateAlarmNotification(int no, string title, string text, int percent) {
         createAndRegisterChannels();
         notifyAlarmNotification(no, title, text, percent);
      }

      /// <summary>
      /// zeigt eine Alarm-Notification an (oder ersetzt sie)
      /// </summary>
      /// <param name="no"></param>
      /// <param name="title"></param>
      /// <param name="text"></param>
      /// <param name="percent"></param>
      static protected void notifyAlarmNotification(int no, string title, string text, int percent) {
         string channelid = NOTIFICATION_CHANNELID_ALARM;
         Notification notification = (channels[channelid] as AppNotificationChannel4Alarm).CreateNotificationSpec(title,
                                                                                                                  text,
                                                                                                                  ChannelDefs[channelid].Icon,
                                                                                                                  percent,
                                                                                                                  no);
         notificationManager.Notify(ChannelDefs[channelid].NotificationID + no,
                                    notification);
      }

      /// <summary>
      /// entfernen einer ev. vorhandenen Alarm-Notification
      /// </summary>
      public static void RemoveAlarmNotification(int no) {
         removeNotification(NOTIFICATION_CHANNELID_ALARM, no);
      }


      /// <summary>
      /// Anzeige der Alarmierungs-Notification
      /// </summary>
      /// <param name="no"></param>
      /// <param name="title"></param>
      /// <param name="text"></param>
      /// <param name="soundurl"></param>
      /// <param name="volume">0.0 .. 1.0</param>
      public static void ShowAlarmingNotification(int no, string title, string text, string soundurl, double volume) {
         wakeUp();

         //text += " (wakeUp-delay=" + Math.Round(DateTime.Now.Subtract(dt0).TotalMilliseconds) + "ms)";

         string channelid = NOTIFICATION_CHANNELID_ALARMING;
         Notification notification = (channels[channelid] as AppNotificationChannel4Alarming).CreateNotificationSpec(title,
                                                                                                                     text,
                                                                                                                     ChannelDefs[channelid].Icon,
                                                                                                                     no);
         notification.Flags |= NotificationFlags.Insistent;    // the audio will be repeated until the notification is cancelled or the notification window is opened.

         //notification.Extras = new Bundle();
         //notification.Extras.PutInt(NOTIFICATION_EXTRA_NO, no);

         int id = channels[channelid].NotificationID + no;
         notificationManager.Cancel(id);
         notificationManager.Notify(id, notification);

         if (soundurl != null)
            playNotifyRingtone(Android.Net.Uri.Parse(soundurl), no, (float)volume);
      }

      /// <summary>
      /// entfernen einer ev. vorhandenen Alarming-Notification
      /// </summary>
      public static void RemoveAlarmingNotification(int no) {
         removeNotification(NOTIFICATION_CHANNELID_ALARMING, no);
         stopNotifyRingtone(no);
      }


      /// <summary>
      /// an die App wurde ein Intent geschickt (durch tippen auf auf eine Notification)
      /// </summary>
      /// <param name="intent"></param>
      public static void IncomingAppIntent(Android.Content.Intent intent) {
         if (intent?.Extras != null) {
            AppIntentType appIntentType = (AppIntentType)intent.GetIntExtra(APPINTENT_EXTRA_TYPE, (int)AppIntentType.unknown);
            if (appIntentType != AppIntentType.unknown) {
               int no = intent.GetIntExtra(APPINTENT_EXTRA_NO, -1);
               if (no >= 0) {
                  string title = intent.GetStringExtra(APPINTENT_EXTRA_TITLE);
                  string text = intent.GetStringExtra(APPINTENT_EXTRA_TEXT);

                  System.Diagnostics.Debug.WriteLine(">>> ... " + appIntentType + ", " + title + ", " + text);

                  RunningTimer ra;
                  switch (appIntentType) {
                     case AppIntentType.AlarmTap:
                        break;

                     case AppIntentType.AlarmCancel:
                        ra = Global.ClockService.GetRunningTimer(no);
                        if (ra == null)
                           ra = new RunningTimer(Guid.NewGuid(), title) {
                              NotificationID = no,
                           };
                        ClockServiceCtrl.SendNotificationMessage(NotificationMessage.Message.ShouldCancelAlarm, ra);
                        break;

                     // Die beiden folgenden Botschaften werden z.Z. NICHT ausgewertet. Sie sind nicht nötig, da schon davor die Alarmierungsbotschaft vom Service 
                     // ausgewertet wird.
                     case AppIntentType.AlarmingClose:
                     case AppIntentType.AlarmingTap:
                        ClockServiceCtrl.SendNotificationMessage(NotificationMessage.Message.ShouldStopAlarming,
                                                                 new RunningTimer(Guid.NewGuid(), title) {
                                                                    NotificationID = no,
                                                                 });
                        break;
                  }
               }
            }
         }
      }

      #endregion

      static void wakeUp() {
         PowerManager pm = appcontext.GetSystemService(Context.PowerService) as PowerManager;
         /*
         public boolean isInteractive ()
         Returns true if the device is in an interactive state.
         When this method returns true, the device is awake and ready to interact with the user (although this is not a guarantee that the user is actively 
         interacting with the device just this moment).
         ...
          */
         if (!pm.IsInteractive) {   // screen is off
            PowerManager.WakeLock wl = pm.NewWakeLock(WakeLockFlags.ScreenDim | WakeLockFlags.AcquireCausesWakeup,
                                                      "myApp:notificationLock");       // Your class name (or other tag) for debugging purposes.
            /*
                  public static final int SCREEN_DIM_WAKE_LOCK        This constant was deprecated in API level 17.
                  Most applications should use WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON instead of this type of wake lock, as it will be correctly managed by the platform as the 
                  user moves between applications and doesn't require a special permission

                  public static final int ACQUIRE_CAUSES_WAKEUP
                  Wake lock flag: Turn the screen on when the wake lock is acquired.
                  Normally wake locks don't actually wake the device, they just cause the screen to remain on once it's already on. Think of the video player application as the 
                  normal behavior. Notifications that pop up and want the device to be on are the exception; use this flag to be like them. 

             */
            wl.Acquire(3000); // The timeout after which to release the wake lock, in milliseconds.
            wl.Release();
         }
      }

      #region Sound als Ringtone abspielen

      static readonly Dictionary<int, Ringtone> activeRingtones = new Dictionary<int, Ringtone>();


      static Ringtone testRingtone;

      /// <summary>
      /// den Standard-Ton 1x zum Test ausgeben
      /// </summary>
      /// <param name="volume"></param>
      public static void PlayDefaultNotificationSound(float volume) {
         StopDefaultNotificationSound();

         testRingtone = RingtoneManager.GetRingtone(appcontext,
                                                    RingtoneManager.GetDefaultUri(RingtoneType.Notification));
         if (testRingtone != null) {
            testRingtone.AudioAttributes = new AudioAttributes.Builder()
                                     .SetUsage(AudioUsageKind.Alarm)
                                     .SetContentType(AudioContentType.Music)
                                     .Build();
            testRingtone.Volume = volume;
            testRingtone.Play();
         }
      }

      public static void StopDefaultNotificationSound() {
         if (testRingtone != null &&
             testRingtone.IsPlaying) {
            testRingtone.Stop();
            testRingtone.Dispose();
            testRingtone = null;
         }
      }




      /// <summary>
      /// spielt den Ton mit der URI ab
      /// </summary>
      /// <param name="uri"></param>
      /// <param name="no"></param>
      /// <param name="volume">0.0 .. 1.0</param>
      static void playNotifyRingtone(Android.Net.Uri uri, int no, float volume = 1.0F) {
         Ringtone rt = RingtoneManager.GetRingtone(appcontext, uri);
         if (rt != null) {
            rt.AudioAttributes = new AudioAttributes.Builder()
                                     .SetUsage(AudioUsageKind.Alarm)
                                     .SetContentType(AudioContentType.Music)
                                     .Build();
            rt.Looping = true;
            rt.Volume = volume;
            rt.Play();
            activeRingtones.Add(no, rt);
         }
      }

      static void stopNotifyRingtone(int no) {
         if (activeRingtones.TryGetValue(no, out Ringtone rt)) {
            activeRingtones.Remove(no);
            if (rt != null) {
               rt.Stop();
               rt.Dispose();
            }
         }
      }

      #endregion

   }
}