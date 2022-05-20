using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace SimpleTimer {
   public partial class MainPage : ContentPage {

      const string TITLE = "Kurzzeitwecker, © by FSofT 11.10.2021";

      public object AndroidActivity { get; set; }


      /// <summary>
      /// Liste aller konfigurierten Timer
      /// </summary>
      public ObservableCollection<TimerData> AllTimers { get; private set; }

      public Command<TimerData> StartTimerCommand { get; private set; }
      public Command<TimerData> MoveDownTimerCommand { get; private set; }
      public Command<TimerData> MoveUpTimerCommand { get; private set; }
      public Command<TimerData> ConfigTimerCommand { get; private set; }
      public Command<TimerData> DeleteTimerCommand { get; private set; }
      public Command AddTimerCommand { get; private set; }

      /// <summary>
      /// Index des Standardsounds (muss auf die Dateien in den Resoucen abgestimmt sein!)
      /// </summary>
      const int standardSoundIndex = 4;   // Klingel2.mp3
      /// <summary>
      /// Sound für den Start eines Timers
      /// </summary>
      const int sound4StartIndex = 0;     // Ding.mp3

      string standardSoundTitle;

      string standardSoundURL;

      /// <summary>
      /// nach OnAppearing true, nach OnDisappearing false
      /// </summary>
      bool isActiv = false;

      public static double SavedVolume {
         get {
            return Preferences.Get("VOL", 1.0); ;
         }
         set {
            Preferences.Set("VOL", value);
         }
      }

      readonly List<string> internalAudiofiles = new List<string>();

      readonly ManualResetEvent mreWait4LoadInternalAudiofiles = new ManualResetEvent(false);

      string soundfile4start = null;


      public MainPage() {
         InitializeComponent();

         StartTimerCommand = new Command<TimerData>(onStartTimer);
         MoveDownTimerCommand = new Command<TimerData>(onMoveDownTimer);
         MoveUpTimerCommand = new Command<TimerData>(onMoveUpTimer);
         ConfigTimerCommand = new Command<TimerData>(onConfigTimer);
         DeleteTimerCommand = new Command<TimerData>(onDeleteTimer);
         AddTimerCommand = new Command(onAddTimer);

         DependencyService.Get<IClockServiceCtrl>().NativeMessage += OnNativeMessage;

         UnpackAndRegisterMusic().Start();

         TimerData.StandardSoundTitle = standardSoundTitle;
         TimerData.StandardSoundUrl = standardSoundURL;

         AllTimers = new ObservableCollection<TimerData>(new List<TimerData>());
         BindingContext = this;

         loadData();
      }

      /// <summary>
      /// speichert die als Resourcen gelieferten mp3-Dateien im internen (App-privaten) Speicher
      /// </summary>
      Task UnpackAndRegisterMusic() {
         Task t = new Task(() => {
            string basepath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyMusic); // intern, z.B.: "/data/user/0/com.fsoft.simpletimer/files/Music"

            if (!Directory.Exists(basepath))
               Directory.CreateDirectory(basepath);

            Assembly ass = GetType().Assembly;

            SortedDictionary<string, string> audiofilenames = new SortedDictionary<string, string>();
            foreach (string resname in ass.GetManifestResourceNames()) {
               string ext = resname.Substring(resname.LastIndexOf('.') + 1);
               if (ext.ToLower() == "mp3") {
                  int start = -1;
                  for (int i = resname.Length - 5; i >= 0; i--) {
                     if (resname[i] == '.') {
                        start = i + 1;
                        break;
                     }
                  }
                  if (start >= 0)
                     audiofilenames.Add(resname, resname.Substring(start));
               }
            }

            int idx = -1;
            foreach (var item in audiofilenames) {
               idx++;
               string filename = item.Value;
               string title = filename.Substring(0, filename.LastIndexOf('.')); // fkt. unter Android 10 wahrscheinlich nicht -> Dateiname verwenden
               string srcfilename = Path.Combine(basepath, filename);

               if (standardSoundIndex == idx) {
                  standardSoundTitle = title;
                  standardSoundURL = srcfilename;
               }

               if (sound4StartIndex == idx)
                  soundfile4start = srcfilename;

               if (!File.Exists(srcfilename)) {
                  using (Stream stream = ass.GetManifestResourceStream(item.Key)) {
                     using (FileStream fs = new FileStream(srcfilename, FileMode.Create)) {
                        stream.CopyTo(fs);
                     }
                  }
               }
               internalAudiofiles.Add(srcfilename);
            }

            mreWait4LoadInternalAudiofiles.Set();

         });

         return t;
      }

      protected override void OnAppearing() {
         base.OnAppearing();
         Title = TITLE + " (v" + Xamarin.Essentials.AppInfo.VersionString + ")";
         isActiv = true;
         Debug.WriteLine(">>> OnAppearing " + isActiv);
      }

      protected override void OnDisappearing() {
         base.OnDisappearing();
         isActiv = false;
         Debug.WriteLine(">>> OnDisappearing " + isActiv);
      }

      /// <summary>
      /// Message vom Service (oder einer Notification)
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void OnNativeMessage(object sender, ClockServiceCtrlMessage.MessageEventArgs e) {
         switch (e.Message) {
            //case ClockServiceCtrlMessage.Message.StartService:
            //case ClockServiceCtrlMessage.Message.StopService:
            //case ClockServiceCtrlMessage.Message.RegisterAlarm:
            //case ClockServiceCtrlMessage.Message.RemoveAlarm:
            //   break;

            case ClockServiceCtrlMessage.Message.ShouldRemoveAlarm:
               if (e.Alarms.Count > 0) {
                  Task<bool> question = FSofTUtils.Xamarin.Helper.MessageBox(this, "Wecker abschalten", e.Alarms[0].Name, "ja", "nein");
                  Task.Run(() => {
                     if (question.Result) {
                        ClockServiceCtrl.RemoveRunningTimer(e.Alarms[0].NotificationID);
                     }
                     //Xamarin.Essentials.MainThread.BeginInvokeOnMainThread(() => {
                     //});
                  });
               }
               break;


               /* Bei ClockServiceCtrlMessage.Message.Alarming, d.h. der direkten Info vom Service, gelingt keine Anzeige der Messagebox.
                * Wenn die Activity bereits läuft, erfolgt keine Anzeige.
                * Merkwürdigerweise klappt es, wenn die Activity erst gestartet werden muss.
                * 
                * Android-Seite:
                  if (msg == ClockServiceCtrlMessage.Message.Alarming) {   // Spezialfall: Message kommt direkt vom Service
                     if (!Global.ActivityIsRunning) {
                        Global.StartActivity();                   // stellt sicher, dass das Event empfangen wird
                        System.Threading.Tasks.Task.Run(() => {
                           while (!Global.ActivityIsRunning)      // auf die Activity warten (falls sie erst neu gestartet werden muss)
                              Thread.Sleep(200);

                           OnServiveMessage?.Invoke(null, new ClockServiceCtrlMessage.MessageEventArgs(msg, ra));
                        });
                     } else
                        OnServiveMessage?.Invoke(null, new ClockServiceCtrlMessage.MessageEventArgs(msg, ra));

                  Deshalb wird dieser Part völlig nach Android verlegt!
                */
               //case ClockServiceCtrlMessage.Message.Alarming:
               //case ClockServiceCtrlMessage.Message.ShouldStopAlarming:
               //   foreach (var alarm in e.Alarms) {
               //      Task.Run(() => {
               //         while (!isActiv)   // auf die Activity warten (falls sie erst neu gestartet werden muss)
               //            Thread.Sleep(200);

               //         Xamarin.Essentials.MainThread.BeginInvokeOnMainThread(() => {
               //            FSofTUtils.Xamarin.Helper.MessageBox(this, "Kurzzeitwecker", alarm.Name, "ausschalten").Wait();
               //            ClockServiceCtrl.RemoveAlarming(alarm.NotificationID);
               //         });

               //         //Device.BeginInvokeOnMainThread(() => {
               //         //   FSofTUtils.Xamarin.Helper.MessageBox(this, "Kurzzeitwecker", alarm.Name, "ausschalten").Wait();
               //         //   ClockServiceCtrl.RemoveAlarming(alarm.NotificationID);
               //         //});
               //      });
               //   }
               //   break;
         }
      }

      /// <summary>
      /// ein Alarm wird gestartet
      /// </summary>
      /// <param name="td"></param>
      void onStartTimer(TimerData td) {
         ClockServiceCtrl.RegisterRunningTimer(new RunningTimer(td.Id, td.Name, td.Duration, td.SoundUrl));
         if (!string.IsNullOrEmpty(soundfile4start))
            FSofTUtils.Xamarin.Helper.PlaySound(soundfile4start);
      }

      void onMoveDownTimer(TimerData td) {
         int idx = AllTimers.IndexOf(td);
         if (0 <= idx && idx < AllTimers.Count - 1) {
            AllTimers.RemoveAt(idx);
            AllTimers.Insert(idx + 1, td);
            TimerList.ScrollTo(idx + 1);
            saveData();
         }
      }

      void onMoveUpTimer(TimerData td) {
         int idx = AllTimers.IndexOf(td);
         if (0 < idx) {
            AllTimers.RemoveAt(idx);
            AllTimers.Insert(idx - 1, td);
            TimerList.ScrollTo(idx - 1);
            saveData();
         }
      }

      async void onAddTimer() {
         await editTimerData(new TimerData());
      }

      async void onConfigTimer(TimerData td) {
         await editTimerData(td);
      }

      async void onDeleteTimer(TimerData td) {
         if (await FSofTUtils.Xamarin.Helper.MessageBox(this,
                                                        "Achtung",
                                                        "Alarmzeit '" + td.Name + "' löschen?",
                                                        "ja",
                                                        "nein")) {
            AllTimers.Remove(td);
            saveData();
         }
      }

      /// <summary>
      /// ermöglicht das Verändern der Daten in <see cref="TimerData"/>
      /// </summary>
      /// <param name="timerData"></param>
      /// <returns></returns>
      async private Task editTimerData(TimerData timerData) {
         TimerConfig timerConfig;
         try {
            mreWait4LoadInternalAudiofiles.WaitOne();

            timerConfig = new TimerConfig() {
               TimerData = timerData,
               InternalAudiofiles = internalAudiofiles,
            };
            timerConfig.SaveEvent += TimerConfig_SaveEvent;

            await Navigation.PushAsync(timerConfig);

         } catch (Exception ex) {
            timerConfig = null;
            await FSofTUtils.Xamarin.Helper.MessageBox(this, "Fehler", ex.Message);
         }
      }

      /// <summary>
      /// Die Daten des Timers sollen gespeichert werden oder ein neuer Timer soll eingefügt werden.
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void TimerConfig_SaveEvent(object sender, EventArgs e) {
         TimerConfig timerConfig = sender as TimerConfig;

         for (int i = 0; i < AllTimers.Count; i++) {
            if (AllTimers[i].Id == timerConfig.TimerData.Id) {   // vorhandener Timer geändert
               saveData();
               return;
            }
         }

         // neuer Timer
         AllTimers.Add(timerConfig.TimerData);
         TimerList.ScrollTo(AllTimers.Count - 1);
         saveData();
      }

      /// <summary>
      /// speichert die Daten in den Xamarin.Essentials.Preferences
      /// </summary>
      void saveData() {
         double vol = SavedVolume;

         Preferences.Clear();
         for (int i = 0; i < AllTimers.Count; i++) {
            TimerData td = AllTimers[i];
            string value = td.Name + System.Environment.NewLine +
                           td.Duration.Ticks.ToString() + System.Environment.NewLine +
                           td.SoundTitle + System.Environment.NewLine +
                           td.SoundUrl;
            Preferences.Set("TD" + i.ToString(), value);
         }

         SavedVolume = vol;
      }

      /// <summary>
      /// liest die Daten aus den Xamarin.Essentials.Preferences
      /// </summary>
      void loadData() {
         AllTimers.Clear();

         //createTestAlarms();

         for (int i = 0; ; i++) {
            string value = Preferences.Get("TD" + i.ToString(), null);
            if (value == null)
               break;

            string[] dat = value.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            if (dat.Length == 4) {
               AllTimers.Add(new TimerData(new TimeSpan(Convert.ToInt64(dat[1])), dat[0], dat[2], dat[3]));
            }
         }

         if (AllTimers.Count == 0) {   // 1 Wecker wird immer automatisch erzeugt
            AllTimers.Add(new TimerData(new TimeSpan(0, 3, 30), "Der Tee ist fertig."));
         }

         ClockServiceCtrl.Volume = SavedVolume;
      }

      void createTestAlarms() {
         AllTimers.Add(new TimerData(new TimeSpan(0, 0, 5), "sehr kurz"));
         AllTimers.Add(new TimerData(new TimeSpan(0, 0, 10), "etwas kurz"));
         AllTimers.Add(new TimerData(new TimeSpan(0, 0, 20), "kurz"));
         AllTimers.Add(new TimerData(new TimeSpan(0, 2, 0), "2 min und superduper schnulli bully fixi foxy blub paloma qwurbel gnazie schnipsie schnups"));
         AllTimers.Add(new TimerData(new TimeSpan(0, 2, 05), "2:05 min sdfg"));
         AllTimers.Add(new TimerData(new TimeSpan(0, 2, 10), "2:10 min ghj"));
         AllTimers.Add(new TimerData(new TimeSpan(0, 2, 20), "2:20 min asdf"));
         AllTimers.Add(new TimerData(new TimeSpan(0, 2, 30), "2:30 min wergh"));
         AllTimers.Add(new TimerData(new TimeSpan(0, 2, 40), "2:40 min hjkl"));
         AllTimers.Add(new TimerData(new TimeSpan(1, 2, 40), "1:2:40 min"));
         AllTimers.Add(new TimerData(new TimeSpan(19, 18, 17), "nichts"));
         AllTimers.Add(new TimerData(new TimeSpan(2, 2, 40), "2:2:40 min"));
         AllTimers.Add(new TimerData(new TimeSpan(3, 2, 40), "3:2:40 min"));
         AllTimers.Add(new TimerData(new TimeSpan(4, 2, 40), "4:2:40 min sadf södl wer qe4 f trtz6ug whrtz tr hr kzku zuk hr etueg r6u o th8 o"));
         AllTimers.Add(new TimerData(new TimeSpan(5, 2, 40), "5:2:40 min"));
         AllTimers.Add(new TimerData(new TimeSpan(6, 2, 40), "6:2:40 min"));
         AllTimers.Add(new TimerData(new TimeSpan(7, 2, 40), "7:2:40 min"));
      }

      async private void ToolbarItem_Info_Clicked(object sender, EventArgs e) {
         //DependencyService.Get<IClockServiceCtrl>().test("A");

         try {
            await Navigation.PushAsync(new InfoPage());
         } catch (Exception ex) {
            await FSofTUtils.Xamarin.Helper.MessageBox(this, "Fehler", ex.Message);
         }
      }

      async private void ToolbarItem_Volume_Clicked(object sender, EventArgs e) {
         try {
            await Navigation.PushAsync(new VolumePage());
         } catch (Exception ex) {
            await FSofTUtils.Xamarin.Helper.MessageBox(this, "Fehler", ex.Message);
         }
      }

   }
}
