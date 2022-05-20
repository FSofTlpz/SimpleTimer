using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;

namespace SimpleTimer.Droid {

   [Service]
   public class ClockService : Service {

      public enum ServiceMessage {
         /// <summary>
         /// der Service wurde gestartet
         /// </summary>
         StartService,
         /// <summary>
         /// der Service soll angehalten werden
         /// </summary>
         ShouldStopService,
         /// <summary>
         /// der Service wurde angehalten
         /// </summary>
         StopService,
         /// <summary>
         /// 1 laufender Wecker wurde registriert
         /// </summary>
         RegisterRunningTimer,
         /// <summary>
         /// 1 laufender Wecker wurde entfernt
         /// </summary>
         RemoveRunningTimer,
         /// <summary>
         /// 1 oder mehrere Alarme wurde ausgelöst
         /// </summary>
         Alarming,
      }


      /// <summary>
      /// Titel für die Summen-Notification
      /// </summary>
      const string SUMMARYTITLE = "Kurzzeitwecker";

      /// <summary>
      /// Periode des Timers in ms
      /// </summary>
      public const int TIMERPERIOD = 100;

      /// <summary>
      /// Verzögerung in ms, um dem Service Zeit zum starten zu geben, ehe die ersten <see cref="RunningTimer"/> aktiv werden
      /// </summary>
      const int DELAYONSTART = 1000;

      /// <summary>
      /// zentraler Timer
      /// </summary>
      System.Threading.Timer timer = null;

      /// <summary>
      /// Anzahl der Perioden des Timers, die noch gewartet wird
      /// </summary>
      int delayPeriodCounter4Start = 0;

      /// <summary>
      /// für die (threadsichere) Abfrage, ob OnStartCommand() schon aufgerufen wurde
      /// </summary>
      long serviceIsStarted = 0;

      /// <summary>
      /// Liste aller laufenden Alarme
      /// </summary>
      readonly List<RunningTimer> runningTimers = new List<RunningTimer>();

      /// <summary>
      /// zur threadsicheren Bearbeitung von <see cref="runningTimers"/>
      /// </summary>
      readonly object runningTimers_locker = new object();

      /// <summary>
      /// Zähler, damit jede Notification eine eigene ID hat
      /// </summary>
      static public int runningTimerNotificationID = 0;

      /// <summary>
      /// WakeLock für die Laufzeit des Service als Versuch, den Doze-Mode zu verhindern, der zu extrem ungenauen Timern führt.
      /// </summary>
      private PowerManager.WakeLock wakeLock = null;


      public ClockService() {
         Global.ClockService = this;
      }

      /// <summary>
      /// The system invokes this method by calling startService() when another component (such as an activity) requests that the service be started. 
      /// When this method executes, the service is started and can run in the background indefinitely. If you implement this, it is your responsibility 
      /// to stop the service when its work is complete by calling stopSelf() or stopService(). If you only want to provide binding, you don't need to 
      /// implement this method.
      /// </summary>
      /// <param name="intent"></param>
      /// <param name="flags"></param>
      /// <param name="startId"></param>
      /// <returns></returns>
      public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId) {
         NotificationHelper.ResetAllChannels();

         // normale Notification erzeugen ...
         Notification summarynotif = NotificationHelper.CreateSummaryNotification(SUMMARYTITLE, "Text");

         // ... und starten
         StartForeground(NotificationHelper.SummaryNotificationID, summarynotif);

         // sofort Update der Daten und der Progressbar
         //updateInfoNotification(chargelevel);

         /*
START_NOT_STICKY        If the system kills the service after onStartCommand() returns, do not recreate the service unless there are pending intents to deliver. 
                        This is the safest option to avoid running your service when not necessary and when your application can simply restart any unfinished jobs.
START_STICKY            If the system kills the service after onStartCommand() returns, recreate the service and call onStartCommand(), but do not redeliver the last intent. 
                        Instead, the system calls onStartCommand() with a null intent unless there are pending intents to start the service. 
                        In that case, those intents are delivered. This is suitable for media players (or similar services) that are not executing commands 
                        but are running indefinitely and waiting for a job.
START_REDELIVER_INTENT  If the system kills the service after onStartCommand() returns, recreate the service and call onStartCommand() with the last intent that 
                        was delivered to the service. Any pending intents are delivered in turn. This is suitable for services that are actively performing a job 
                        that should be immediately resumed, such as downloading a file.
         */

         IsStarted = true;

         delayPeriodCounter4Start = DELAYONSTART / TIMERPERIOD;
         startStopSystemTimer();

         SendMessage2Controller(ServiceMessage.StartService);

         return StartCommandResult.Sticky;
      }

      /// <summary>
      /// The system invokes this method to perform one-time setup procedures when the service is initially created (before it calls either onStartCommand() or onBind()). 
      /// If the service is already running, this method is not called.
      /// </summary>
      public override void OnCreate() {
         base.OnCreate();

         Task.Run(() => {
            PowerManager pm = (PowerManager)GetSystemService(Context.PowerService);
            /*
AcquireCausesWakeup     Turn the screen on when the wake lock is acquired.
                        Normally wake locks don't actually wake the device, they just cause the screen to remain on once it's already on. Think of the video player application as 
                        the normal behavior. Notifications that pop up and want the device to be on are the exception; use this flag to be like them.
                        Cannot be used with PARTIAL_WAKE_LOCK. 

Full                    Ensures that the screen and keyboard backlight are on at full brightness.        deprecated in API level 17

OnAfterRelease          When this wake lock is released, poke the user activity timer so the screen stays on for a little longer.
                        Will not turn the screen on if it is not already on. See ACQUIRE_CAUSES_WAKEUP if you want that.
                        Cannot be used with PARTIAL_WAKE_LOCK. 

Partial                 Ensures that the CPU is running; the screen and keyboard backlight will be allowed to go off.
                        If the user presses the power button, then the screen will be turned off but the CPU will be kept on until all partial wake locks have been released. 

ProximityScreenOff      Turns the screen off when the proximity sensor activates.
                        If the proximity sensor detects that an object is nearby, the screen turns off immediately. Shortly after the object moves away, the screen turns on again.
                        A proximity wake lock does not prevent the device from falling asleep unlike FULL_WAKE_LOCK, SCREEN_BRIGHT_WAKE_LOCK and SCREEN_DIM_WAKE_LOCK. If there is 
                        no user activity and no other wake locks are held, then the device will fall asleep (and lock) as usual. However, the device will not fall asleep while the 
                        screen has been turned off by the proximity sensor because it effectively counts as ongoing user activity.
                        Since not all devices have proximity sensors, use isWakeLockLevelSupported(int) to determine whether this wake lock level is supported.
                        Cannot be used with ACQUIRE_CAUSES_WAKEUP. 

ReleaseFlagWaitForNoProximity    Flag for WakeLock.release(int): Defer releasing a PROXIMITY_SCREEN_OFF_WAKE_LOCK wake lock until the proximity sensor indicates 
                        that an object is not in close proximity.

ScreenBright            Ensures that the screen is on at full brightness; the keyboard backlight will be allowed to go off.         deprecated in API level 15

ScreenDim               Ensures that the screen is on (but may be dimmed); the keyboard backlight will be allowed to go off.        deprecated in API level 17
             */
            wakeLock = pm.NewWakeLock(WakeLockFlags.Partial, this.PackageName);  // Creates a new wake lock with the specified level and flags.
            wakeLock.Acquire();  // Acquires the wake lock. Ensures that the device is on at the level requested when the wake lock was created.
         });

      }

      /// <summary>
      /// The system invokes this method by calling bindService() when another component wants to bind with the service (such as to perform RPC). 
      /// In your implementation of this method, you must provide an interface that clients use to communicate with the service by returning an IBinder. 
      /// You must always implement this method; however, if you don't want to allow binding, you should return null.
      /// </summary>
      /// <param name="intent"></param>
      /// <returns></returns>
      public override IBinder OnBind(Intent intent) {
         return null;
      }

      /// <summary>
      /// The system invokes this method when the service is no longer used and is being destroyed. Your service should implement this to clean up any resources 
      /// such as threads, registered listeners, or receivers. This is the last call that the service receives.
      /// </summary>
      public override void OnDestroy() {
         startStopSystemTimer();

         List<int> notificationID = new List<int>();
         lock (runningTimers_locker) {
            for (int i = 0; i < runningTimers.Count; i++)
               notificationID.Add(runningTimers[i].NotificationID);
            runningTimers.Clear();
         }

         foreach (var item in notificationID) {
            NotificationHelper.RemoveAlarmNotification(item);
         }

         NotificationHelper.RemoveSummaryNotification();

         IsStarted = false;

         if (wakeLock != null) {
            wakeLock.Release(); // This method releases your claim to the CPU or screen being on. The screen may turn off shortly after you release the wake lock, or it may not if there are other wake locks still held. 
            wakeLock = null;
         }

         base.OnDestroy();

         SendMessage2Controller(ServiceMessage.StopService);
      }

      public bool IsStarted {
         get {
            return Interlocked.Read(ref serviceIsStarted) != 0;
         }
         protected set {
            Interlocked.Exchange(ref serviceIsStarted, value ? 1 : 0);
         }
      }

      void SendMessage2Controller(ServiceMessage msg, RunningTimer ra) {
         SendMessage2Controller(msg, new List<RunningTimer>() { ra });
      }

      void SendMessage2Controller(ServiceMessage msg, IList<RunningTimer> ralst = null) {
         if (Global.ClockServiceCtrl != null)
            Global.ClockServiceCtrl.OnClockServiceMessage(msg, ralst);
      }

      /// <summary>
      /// fügt einen neuen laufenden Wecker in die Liste ein
      /// </summary>
      /// <param name="runningAlarm"></param>
      /// <returns></returns>
      public bool InsertRunningTimer(RunningTimer runningAlarm) {
         int pos = 0;
         lock (runningTimers_locker) {
            for (; pos < runningTimers.Count; pos++) {
               if (runningTimers[pos].EndTime > runningAlarm.EndTime)
                  break;
            }
            runningAlarm.NotificationID = Interlocked.Increment(ref runningTimerNotificationID);
            runningTimers.Insert(pos, runningAlarm);
         }

         if (Interlocked.Read(ref serviceIsStarted) != 0) {
            showRunningTimer(runningAlarm);
            updateSummaryNotification();
            startStopSystemTimer();
         }
         SendMessage2Controller(ServiceMessage.RegisterRunningTimer, runningAlarm);
         return true;
      }

      /// <summary>
      /// entfernt einen laufenden Wecker aus der Liste
      /// </summary>
      /// <param name="runningAlarm"></param>
      public void RemoveRunningTimer(RunningTimer runningAlarm) {
         int notificationID = -1;
         lock (runningTimers_locker) {
            for (int i = 0; i < runningTimers.Count; i++) {
               if (runningTimers[i].Id == runningAlarm.Id &&
                   runningTimers[i].EndTime == runningAlarm.EndTime) {
                  notificationID = runningTimers[i].NotificationID;
                  runningTimers.RemoveAt(i);
                  break;
               }
            }
         }
         if (notificationID >= 0) {
            NotificationHelper.RemoveAlarmNotification(notificationID);
            updateSummaryNotification();
            SendMessage2Controller(ServiceMessage.RemoveRunningTimer, runningAlarm);
         }

         startStopSystemTimer();
      }

      /// <summary>
      /// entfernt einen laufenden Wecker aus der Liste
      /// </summary>
      /// <param name="alarmNo"></param>
      public void RemoveRunningTimer(int alarmNo) {
         RunningTimer alarm = null;
         lock (runningTimers_locker) {
            for (int i = 0; i < runningTimers.Count; i++) {
               if (runningTimers[i].NotificationID == alarmNo) {
                  alarm = runningTimers[i];
                  break;
               }
            }
         }

         if (alarm != null)
            RemoveRunningTimer(alarm);
      }

      /// <summary>
      /// liefert eine Liste mit Kopien der akt. laufenden Wecker
      /// </summary>
      /// <returns></returns>
      public List<RunningTimer> GetRunningTimers() {
         List<RunningTimer> lst = new List<RunningTimer>();
         lock (runningTimers_locker) {
            for (int i = 0; i < runningTimers.Count; i++)
               lst.Add(new RunningTimer(runningTimers[i]));
         }
         return lst;
      }

      /// <summary>
      /// liefert den laufenden Wecker mit der Nummer (oder null)
      /// </summary>
      /// <param name="no"></param>
      /// <returns></returns>
      public RunningTimer GetRunningTimer(int no) {
         lock (runningTimers_locker) {
            for (int i = 0; i < runningTimers.Count; i++)
               if (runningTimers[i].NotificationID == no)
                  return new RunningTimer(runningTimers[i]);
         }
         return null;
      }


      void startRegisteredTimers() {
         lock (runningTimers_locker) {
            if (runningTimers.Count > 0) {
               foreach (var alarm in runningTimers)
                  showRunningTimer(alarm);
               updateSummaryNotification();
               startStopSystemTimer();
            }
         }
      }

      void startStopSystemTimer() {
         int alarms = 0;
         lock (runningTimers_locker)
            alarms = runningTimers.Count;

         if (alarms > 0 && !timerIsRunning)
            startTimer(TIMERPERIOD);

         if (alarms == 0 &&
             timerIsRunning &&
             !NotificationHelper.ExistsActiveNotifications(NotificationHelper.NOTIFICATION_CHANNELID_ALARMING))
            stopTimer();
      }

      /// <summary>
      /// der Timer wird gestartet
      /// </summary>
      /// <param name="period">Intervall in ms</param>
      void startTimer(int period) {
         stopTimer();
         timer = new System.Threading.Timer(onTimerStep, null, 0, period);
      }

      /// <summary>
      /// der Timer wird gestoppt
      /// </summary>
      void stopTimer() {
         if (timerIsRunning) {
            timer.Change(System.Threading.Timeout.Infinite, 0);
            timer.Dispose();
            timer = null;
            SendMessage2Controller(ServiceMessage.ShouldStopService);
         }
      }

      bool timerIsRunning {
         get {
            return timer != null;
         }
      }

      readonly ManualResetEvent onTimerStep_IsFree_Event = new ManualResetEvent(true);


      /// <summary>
      /// Timerintervall abgelaufen
      /// </summary>
      /// <param name="state"></param>
      void onTimerStep(object state) {
         if (!onTimerStep_IsFree_Event.WaitOne((3 * TIMERPERIOD) / 4))   // wartet max. TIMERPERIOD ms auf die Freigabe der Funktion
            return;
         else
            onTimerStep_IsFree_Event.Reset(); // Zugang zur Funktion ist jetzt gesperrt

         // Verzögerung der Anzeige beim Start des Service
         if (delayPeriodCounter4Start > 0) {

            if (delayPeriodCounter4Start-- <= 1)
               startRegisteredTimers();

         } else {

            DateTime now = DateTime.Now;

            // für alle abgelaufenen Alarme den Alarm auslösen
            List<RunningTimer> endedRunningAlarms = null;
            bool alarmlistchangedorempty = false;
            lock (runningTimers_locker) {

               if (runningTimers.Count == 0)
                  alarmlistchangedorempty = true;
               else
                  for (int i = 0; i < runningTimers.Count; i++) {
                     if (runningTimers[i].EndTime <= now) {
                        runningTimers[i].CalculateTimerServiceStepDuration(now);
                        if (endedRunningAlarms == null)
                           endedRunningAlarms = new List<RunningTimer>();
                        endedRunningAlarms.Add(runningTimers[i]);
                        runningTimers.RemoveAt(i);       // aus der Liste löschen

                        alarmlistchangedorempty = true;
                     } else {
                        runningTimers[i].CalculateTimerServiceStepDuration(now);
                     }
                  }
            }

            if (alarmlistchangedorempty &&
                endedRunningAlarms != null)
               showAlarming(endedRunningAlarms);

            if ((now.Millisecond / 100) % 10 == 0) // Akt. aller Notifications (wenn die 1/10s auf 0 steht -> 1x je Sekunde)
               updateRunningTimers();

            if (alarmlistchangedorempty)
               startStopSystemTimer();

         }

         onTimerStep_IsFree_Event.Set(); // Zugang zur Funktion ist jetzt wieder freigegeben
      }

      /// <summary>
      /// ein laufender Wecker wird als Notification angezeigt (Name als Titel und Restzeit als Text)
      /// </summary>
      /// <param name="ra"></param>
      void showRunningTimer(RunningTimer ra) {
         NotificationHelper.ShowAlarmNotification(ra.NotificationID,
                                                  ra.Name,
                                                  "[Restzeit " + getDurationAsString(ra.Duration) + "]");
      }

      string getDurationAsString(TimeSpan ts) {
         return (ts.TotalSeconds < 60 ?
                        ts.Seconds.ToString() + "s" :
                 ts.TotalSeconds < 3600 ?
                        ts.Minutes.ToString() + ":" + ts.Seconds.ToString("D2") + "min" :
                        ts.Hours.ToString() + ":" + ts.Minutes.ToString("D2") + ":" + ts.Seconds.ToString("D2"));
      }

      /// <summary>
      /// die Notification aller laufenden Wecker wird akt.
      /// </summary>
      void updateRunningTimers() {
         DateTime now = DateTime.Now;
         lock (runningTimers_locker) {
            foreach (RunningTimer alarm in runningTimers) {
               TimeSpan ts = alarm.EndTime.Subtract(now);   // restl. Zeit

               NotificationHelper.UpdateAlarmNotification(alarm.NotificationID,
                                                          alarm.Name,
                                                          "[Restzeit " + getDurationAsString(ts) + "]",
                                                          (int)Math.Round(100 * (alarm.Duration.TotalSeconds - ts.TotalSeconds) / alarm.Duration.TotalSeconds)); // Prozent
            }
         }
         updateSummaryNotification();
      }

      /// <summary>
      /// alle nicht mehr benötigten Notifications entfernen, zusätzlich benötigten Notications hinzufügen und bei den anderen Update der Anzeige
      /// </summary>
      void updateSummaryNotification() {
         NotificationHelper.UpdateSummaryNotification(null, null);
      }

      /// <summary>
      /// die laufenden Weckern, die ihren Endzeitpunkt erreicht oder überschritten haben, werden als Notification angezeigt und an den <see cref="ClockServiceCtrl"/> geliefert
      /// </summary>
      /// <param name="endedRunningAlarms"></param>
      void showAlarming(IList<RunningTimer> endedRunningAlarms) {
         foreach (var alarm in endedRunningAlarms) {
            NotificationHelper.RemoveAlarmNotification(alarm.NotificationID);
            NotificationHelper.ShowAlarmingNotification(alarm.NotificationID,
                                                        getDurationAsString(alarm.Duration) + " sind um!",
                                                        alarm.Name,
                                                        alarm.SoundUrl,
                                                        Global.ClockServiceCtrl != null ? Global.ClockServiceCtrl.Volume : 1);
         }
         SendMessage2Controller(ServiceMessage.Alarming, endedRunningAlarms);
      }

   }
}
