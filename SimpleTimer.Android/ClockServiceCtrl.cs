using System;
using System.Collections.Generic;
using System.Threading;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;

[assembly: Xamarin.Forms.Dependency(typeof(SimpleTimer.Droid.ClockServiceCtrl))]
namespace SimpleTimer.Droid {

   /// <summary>
   /// Interface zur Xamarin-Seite zum Steuern des Service
   /// </summary>
   internal class ClockServiceCtrl : IClockServiceCtrl {

      private static readonly Android.Content.Context appcontext = global::Android.App.Application.Context;


      /// <summary>
      /// für Events, die vom Service ausgelöst wurden
      /// </summary>
      public event EventHandler<ClockServiceCtrlMessage.MessageEventArgs> OnServiceMessage;

      event EventHandler<ClockServiceCtrlMessage.MessageEventArgs> IClockServiceCtrl.NativeMessage {
         add {
            OnServiceMessage += value;
         }

         remove {
            OnServiceMessage -= value;
         }
      }


      double _volume = 1.0;

      /// <summary>
      /// Lautstärke 0.0 .. 1.0
      /// </summary>
      public double Volume {
         get {
            return Interlocked.CompareExchange(ref _volume, 0, -1);  // Vgl. ist immer false -> Wert bleibt bestehen
         }
         set {
            value = Math.Max(0, Math.Min(1, value));
            Interlocked.Exchange(ref _volume, value);
         }
      }


      public ClockServiceCtrl() {
         SetServiceActive(appcontext, false);
         NotificationHelper.ClockServiceCtrl = this;
         Global.ClockServiceCtrl = this;
      }

      #region Message-Behandlung

      /// <summary>
      /// der native Service liefert eine Message, die dann an die eigentliche App weitergeleitet wird
      /// </summary>
      /// <param name="msg"></param>
      /// <param name="ralst"></param>
      public void OnClockServiceMessage(ClockService.ServiceMessage msg, IList<RunningTimer> ralst = null) {
         switch (msg) {
            case ClockService.ServiceMessage.StartService:
               sendMessage(ClockServiceCtrlMessage.Message.StartService, ralst);
               break;

            case ClockService.ServiceMessage.ShouldStopService:
               StopService();
               break;

            case ClockService.ServiceMessage.StopService:
               sendMessage(ClockServiceCtrlMessage.Message.StopService, ralst);
               break;

            case ClockService.ServiceMessage.RegisterRunningTimer:
               sendMessage(ClockServiceCtrlMessage.Message.RegisterAlarm, ralst);
               break;

            case ClockService.ServiceMessage.RemoveRunningTimer:
               sendMessage(ClockServiceCtrlMessage.Message.RemoveAlarm, ralst);
               break;

            case ClockService.ServiceMessage.Alarming:
               sendMessage(ClockServiceCtrlMessage.Message.Alarming, ralst);
               break;
         }
      }

      /// <summary>
      /// eine Notification-Message wird an die App weitergeleitet
      /// </summary>
      /// <param name="msg"></param>
      /// <param name="ra"></param>
      public void SendNotificationMessage(NotificationMessage.Message msg, RunningTimer ra) {
         switch (msg) {
            case NotificationMessage.Message.ShouldCancelAlarm:
               sendMessage(ClockServiceCtrlMessage.Message.ShouldRemoveAlarm, new List<RunningTimer>() { ra });
               break;

            case NotificationMessage.Message.ShouldStopAlarming:
               sendMessage(ClockServiceCtrlMessage.Message.ShouldStopAlarming, new List<RunningTimer>() { ra });
               break;

         }
      }

      void sendMessage(ClockServiceCtrlMessage.Message msg, IList<RunningTimer> ra = null) {
         if (msg == ClockServiceCtrlMessage.Message.Alarming ||
             msg == ClockServiceCtrlMessage.Message.ShouldStopAlarming) {  // nur 1x eine Info an die App senden

            if (ra.Count == 0)
               return;

            if (ra.Count > 0) {
               if (msg == ClockServiceCtrlMessage.Message.Alarming) {   // Spezialfall: Message kommt direkt vom Service
                  if (!Global.ActivityIsRunning) {
                     Global.StartActivity();                   // stellt sicher, dass das Event empfangen wird
                     System.Threading.Tasks.Task.Run(() => {

                        int count = 1;

                        while (!Global.ActivityIsRunning) {     // auf die Activity warten (falls sie erst neu gestartet werden muss)
                           Thread.Sleep(200);
                           count++;
                        }

                        alarmingDialogOnUiThread(ra, count);
                     });
                  } else {
                     alarmingDialogOnUiThread(ra, 0);
                  }
                  return;
               }
            }
         }
         OnServiceMessage?.Invoke(null, new ClockServiceCtrlMessage.MessageEventArgs(msg, ra));
      }

      void alarmingDialogOnUiThread(IList<RunningTimer> ralist,
                                    int waitcycles) {
         Global.MainActivity.RunOnUiThread(() => {

            DateTime now = DateTime.Now;
            foreach (RunningTimer ra in ralist) {
               string delaywarning = "";
               if (ra.TimerServiceStepDuration.TotalMilliseconds > 1.5 * ClockService.TIMERPERIOD) { // Zeit um 50% überschritten
                  delaywarning = "Doze (?) " + Math.Round(ra.TimerServiceStepDuration.TotalMilliseconds) + "ms > " + ClockService.TIMERPERIOD + "ms";
               }
               double delay4showing = Math.Round(now.Subtract(ra.EndTime).TotalMilliseconds);
               if (delay4showing > 1000) {
                  if (delaywarning != "")
                     delaywarning += "; ";

                  delaywarning += "Anzeigeverzögerung " + delay4showing + "ms";
               }

               //AlertDialog dialog = new AlertDialog.Builder(Global.MainActivity)
               //                                 .SetIcon(Resource.Mipmap.clock)
               //                                 .SetTitle("Kurzzeitwecker")
               //                                 .SetMessage(ra.Name + debuginfo)
               //                                 //.SetMessage(ra.Name)
               //                                 // Specifying a listener allows you to take an action before dismissing the dialog. The dialog is automatically dismissed when a dialog button is clicked.
               //                                 .SetPositiveButton("ausschalten", (senderAlert, args) => {
               //                                    RemoveAlarming(ra.NotificationID);
               //                                 })
               //                                 .SetCancelable(false) // kann nicht abgebrochen werden
               //                                 .Create();
               //dialog.Window.SetType(Android.Views.WindowManagerTypes.SystemAlert);
               //dialog.Show();

               new AlertDialog.Builder(Global.MainActivity)
                                 .SetIcon(Resource.Mipmap.clock)
                                 .SetTitle("Kurzzeitwecker")
                                 .SetMessage(ra.Name + (delaywarning == "" ?
                                                               "" : 
                                                               System.Environment.NewLine + System.Environment.NewLine + " [" + delaywarning + "]"))
                                 // Specifying a listener allows you to take an action before dismissing the dialog. The dialog is automatically dismissed when a dialog button is clicked.
                                 .SetPositiveButton("ausschalten", (senderAlert, args) => {
                                    RemoveAlarming(ra.NotificationID);
                                 })
                                 .SetCancelable(false) // kann nicht abgebrochen werden
                                 .Show();
            }
         });
         OnServiceMessage?.Invoke(null, new ClockServiceCtrlMessage.MessageEventArgs(ClockServiceCtrlMessage.Message.Alarming, ralist));
      }

      #endregion


      /// <summary>
      /// startet den Service falls er noch nicht aktiv ist
      /// </summary>
      /// <returns></returns>
      public bool StartService() {
         if (!IsServiceActive()) {
            var myserviceintent = new Intent(appcontext, Java.Lang.Class.FromType(typeof(ClockService)));

            ComponentName componentName;
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
               componentName = appcontext.StartForegroundService(myserviceintent);
            else
               componentName = appcontext.StartService(myserviceintent);

            SetServiceActive(appcontext, componentName != null);

            return componentName != null;
         }
         return true;
      }

      /// <summary>
      /// stopt den Service falls er aktiv ist
      /// </summary>
      /// <returns></returns>
      public bool StopService() {
         if (IsServiceActive()) {
            bool result = appcontext.StopService(new Android.Content.Intent(appcontext, typeof(ClockService)));

            SetServiceActive(appcontext, false);

            return result;
         }
         return true;
      }

      /// <summary>
      /// nur für die Abfrage aus der App
      /// </summary>
      /// <returns></returns>
      public bool IsServiceActive() {
         return GetServiceIsActive(appcontext);
      }

      /// <summary>
      /// registriert einen laufenden Wecker im <see cref="ClockService"/>
      /// </summary>
      /// <param name="runningAlarm"></param>
      /// <returns></returns>
      public bool RegisterRunningTimer(RunningTimer runningAlarm) {
         bool res = true;
         if (!IsServiceActive())
            res = StartService();

         if (res) {

            System.Threading.Tasks.Task.Run(() => {
               bool inserted = false;
               while (!inserted) {
                  if (Global.ClockService != null &&
                      Global.ClockService.IsStarted) {
                     res = Global.ClockService.InsertRunningTimer(runningAlarm);
                     inserted = true;
                  } else
                     Thread.Sleep(100);
               }

               if (res)
                  Toast.MakeText(appcontext, "'" + runningAlarm.Name + "'" + " gestartet", ToastLength.Long).Show();

            });

         }

         return res;
      }

      /// <summary>
      /// entfernt einen laufenden Wecker aus dem <see cref="ClockService"/>
      /// </summary>
      /// <param name="runningAlarm"></param>
      public void RemoveRunningTimer(RunningTimer runningAlarm) {
         Global.ClockService.RemoveRunningTimer(runningAlarm);
      }

      /// <summary>
      /// entfernt einen laufenden Wecker aus dem <see cref="ClockService"/>
      /// </summary>
      /// <param name="no"></param>
      public void RemoveRunningTimer(int no) {
         Global.ClockService.RemoveRunningTimer(no);
      }

      /// <summary>
      /// entfernt nur die Notification für diesen Alarm
      /// </summary>
      /// <param name="no"></param>
      public void RemoveAlarming(int no) {
         NotificationHelper.RemoveAlarmingNotification(no);
      }

      /// <summary>
      /// liefert eine Liste mit Kopien der akt. laufenden Wecker
      /// </summary>
      /// <returns></returns>
      public List<RunningTimer> GetRunningTimers() {
         if (!IsServiceActive())
            return new List<RunningTimer>();
         return Global.ClockService.GetRunningTimers();
      }


      #region Speichern/Lesen spezieller privater (Android-)Vars

      const string SERVICEISACTIVE = "ServiceIsActive";

      /// <summary>
      /// 
      /// </summary>
      /// <param name="context">wenn null, dann wird automatisch der App-Context verwendet</param>
      /// <returns></returns>
      public bool GetServiceIsActive(Context context) {
         if (context != null)
            return GetPrivateData(context, SERVICEISACTIVE, false);
         else
            return GetPrivateData(SERVICEISACTIVE, false);
      }

      private void SetServiceActive(Context context = null, bool on = true) {
         if (context != null)
            SetPrivateData(context, SERVICEISACTIVE, on);
         else
            SetPrivateData(SERVICEISACTIVE, on);
      }


      const string NOTIFICATIONCHANNELBASEID = "NotificationChannelBaseID";

      public string NotificationChannelBaseID {
         get {
            return GetPrivateData(NOTIFICATIONCHANNELBASEID, "");
         }
         set {
            SetPrivateData(NOTIFICATIONCHANNELBASEID, value);
         }
      }

      #endregion

      #region allg. Funktionen zum Speichern/Lesen privater (Android-)Vars

      const string PREFFILE = "servicevars";

      static void SetPrivateData(string varname, bool value) {
         SetPrivateData(appcontext, varname, value);
      }

      static void SetPrivateData(string varname, int value) {
         SetPrivateData(appcontext, varname, value);
      }

      static void SetPrivateData(string varname, string value) {
         SetPrivateData(appcontext, varname, value);
      }

      static void SetPrivateData(string varname, float value) {
         SetPrivateData(appcontext, varname, value);
      }

      static bool GetPrivateData(string varname, bool defvalue) {
         return GetPrivateData(appcontext, varname, defvalue);
      }

      static int GetPrivateData(string varname, int defvalue) {
         return GetPrivateData(appcontext, varname, defvalue);
      }

      static string GetPrivateData(string varname, string defvalue) {
         return GetPrivateData(appcontext, varname, defvalue);
      }

      static float GetPrivateData(string varname, float defvalue) {
         return GetPrivateData(appcontext, varname, defvalue);
      }


      static void SetPrivateData(Context context, string varname, bool value) {
         var pref = context.GetSharedPreferences(PREFFILE, FileCreationMode.Private);
         var editor = pref.Edit();
         editor.PutBoolean(varname, value);
         editor.Apply();
      }

      static void SetPrivateData(Context context, string varname, int value) {
         var pref = context.GetSharedPreferences(PREFFILE, FileCreationMode.Private);
         var editor = pref.Edit();
         editor.PutInt(varname, value);
         editor.Apply();
      }

      static void SetPrivateData(Context context, string varname, string value) {
         var pref = context.GetSharedPreferences(PREFFILE, FileCreationMode.Private);
         var editor = pref.Edit();
         editor.PutString(varname, value);
         editor.Apply();
      }

      static void SetPrivateData(Context context, string varname, float value) {
         var pref = context.GetSharedPreferences(PREFFILE, FileCreationMode.Private);
         var editor = pref.Edit();
         editor.PutFloat(varname, value);
         editor.Apply();
      }

      static bool GetPrivateData(Context context, string varname, bool defvalue) {
         var pref = context.GetSharedPreferences(PREFFILE, FileCreationMode.Private);
         return pref.GetBoolean(varname, defvalue);
      }

      static int GetPrivateData(Context context, string varname, int defvalue) {
         var pref = context.GetSharedPreferences(PREFFILE, FileCreationMode.Private);
         return pref.GetInt(varname, defvalue);
      }

      static string GetPrivateData(Context context, string varname, string defvalue) {
         var pref = context.GetSharedPreferences(PREFFILE, FileCreationMode.Private);
         return pref.GetString(varname, defvalue);
      }

      static float GetPrivateData(Context context, string varname, float defvalue) {
         var pref = context.GetSharedPreferences(PREFFILE, FileCreationMode.Private);
         return pref.GetFloat(varname, defvalue);
      }

      #endregion

      public double GetVolume() {
         return Volume;
      }

      public void SetVolume(double volume) {
         Volume = volume;
      }

      public void PlayTestSound(double volume) {
         StopTestSound();
         NotificationHelper.PlayDefaultNotificationSound((float)volume);
      }

      public void StopTestSound() {
         NotificationHelper.StopDefaultNotificationSound();
      }


      public int test(string txt) {


         //AlertDialog alertDialog = new AlertDialog.Builder(Global.MainActivity)
         //      .SetIcon(Resource.Mipmap.clock)
         //      .SetTitle("Are you sure to Exit")
         //      .SetMessage("Exiting will call finish() method")
         //      // Specifying a listener allows you to take an action before dismissing the dialog. The dialog is automatically dismissed when a dialog button is clicked.
         //      .SetPositiveButton("Yes", (senderAlert, args) => {
         //      })
         //      .SetNegativeButton("No", (senderAlert, args) => { })
         //      .Show();



         return 0;
      }

   }

}