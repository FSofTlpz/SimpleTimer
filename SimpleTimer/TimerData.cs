using System;
using System.ComponentModel;

namespace SimpleTimer {

   /// <summary>
   /// Daten eines Timers
   /// </summary>
   public class TimerData : IComparable, INotifyPropertyChanged {

      public static string StandardTitle = "neuer Wecker";

      public static string StandardSoundTitle = "Alarm";

      public static string StandardSoundUrl = "";

      public Guid Id {
         get;
         protected set;
      }

      #region bindable Properties

      TimeSpan _time;
      /// <summary>
      /// zeitl. Länge des Timers
      /// </summary>
      public TimeSpan Duration {
         get {
            return _time;
         }
         set {
            _time = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Duration)));
         }
      }

      string _name;
      /// <summary>
      /// Name eines Timers
      /// </summary>
      public string Name {
         get {
            return _name;
         }
         set {
            _name = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
         }
      }

      string _soundTitle;
      /// <summary>
      /// Name des Tones
      /// </summary>
      public string SoundTitle {
         get {
            return _soundTitle;
         }
         set {
            _soundTitle = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SoundTitle)));
         }
      }

      string _soundUrl;
      /// <summary>
      /// URL des Tones
      /// </summary>
      public string SoundUrl {
         get {
            return _soundUrl;
         }
         set {
            _soundUrl = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SoundUrl)));
         }
      }

      #endregion


      public TimerData(TimeSpan ts, string name, string soundtitle, string soundurl) {
         Id = Guid.NewGuid();
         Duration = new TimeSpan(ts.Ticks);
         Name = name;
         SoundTitle = soundtitle;
         SoundUrl = soundurl;
      }

      public TimerData(TimeSpan ts, string name) : this(ts, name, StandardSoundTitle, StandardSoundUrl) { }

      public TimerData(TimeSpan ts) : this(ts, StandardTitle, StandardSoundTitle, StandardSoundUrl) { }

      public TimerData() : this(new TimeSpan(0, 0, 0), StandardTitle, StandardSoundTitle, StandardSoundUrl) { }

      public TimerData(TimerData timerData) {
         Duration = new TimeSpan(timerData.Duration.Ticks);
         Name = timerData.Name;
         SoundTitle = timerData.SoundTitle;
         SoundUrl = timerData.SoundUrl;
      }

      public event PropertyChangedEventHandler PropertyChanged;

      public void SetSound(string soundtitle, string soundurl) {
         SoundTitle = soundtitle;
         SoundUrl = soundurl;
      }

      public int CompareTo(object obj) {
         if (obj is TimerData) {
            TimerData timerData = obj as TimerData;
            int result = Name.CompareTo(timerData.Name);
            if (result == 0) {
               if (Duration.Ticks == timerData.Duration.Ticks)
                  return 0;
               else if (Duration.Ticks > timerData.Duration.Ticks)
                  return 1;
               return -1;
            }
            return result;
         } else
            throw new Exception("Vergleich der Objekte nicht möglich.");
      }

      public override string ToString() {
         return string.Format("{0:D2}:{1:D2}:{2:D2} {3}", Duration.Hours, Duration.Minutes, Duration.Seconds, Name);
      }
   }
}
