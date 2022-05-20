using System;
using System.Collections.Generic;

namespace SimpleTimer {

   /// <summary>
   /// Daten eines "gestarteten" Weckers
   /// </summary>
   public class RunningTimer {
      /// <summary>
      /// ID des Weckers (stammt von der ursprünglichen Definition eines Weckers)
      /// </summary>
      public Guid Id {
         get;
         protected set;
      }

      /// <summary>
      /// Name des Weckers (stammt von der ursprünglichen Definition eines Weckers)
      /// </summary>
      public string Name {
         get;
         protected set;
      }

      /// <summary>
      /// gewünschter Alarmzeitpunkt des Weckers
      /// </summary>
      public DateTime EndTime {
         get;
         protected set;
      }

      /// <summary>
      /// gewünschte Laufzeit des Weckers (nur zur Info)
      /// </summary>
      public TimeSpan Duration {
         get;
         protected set;
      }

      /// <summary>
      /// i.A. Sounddatei
      /// </summary>
      public string SoundUrl {
         get;
         protected set;
      }

      /// <summary>
      /// wird intern im Service verwendet
      /// </summary>
      public int NotificationID {
         get;
         set;
      }

      DateTime dt4Step = DateTime.MinValue;

      /// <summary>
      /// um Verzögerungen im ClockService zu erkennen (z.B. durch den Doze-Modus, der eigentlich nicht auftreten sollte!)
      /// </summary>
      public TimeSpan TimerServiceStepDuration {
         get;
         private set;
      } = TimeSpan.Zero;



      RunningTimer(Guid id, string name, DateTime end, TimeSpan duration, string soundurl) {
         Id = new Guid(id.ToByteArray());
         Name = name;
         EndTime = new DateTime(end.Ticks);
         NotificationID = -1;
         Duration = duration;
         SoundUrl = soundurl;
      }

      /// <summary>
      /// die <see cref="EndTime"/> wird intern aus der akt. Zeit berechnet
      /// </summary>
      /// <param name="id"></param>
      /// <param name="name"></param>
      /// <param name="duration"></param>
      /// <param name="soundurl"></param>
      public RunningTimer(Guid id, string name, TimeSpan duration, string soundurl) {
         Id = new Guid(id.ToByteArray());
         Name = name;
         EndTime = DateTime.Now.Add(duration);
         NotificationID = -1;
         Duration = duration;
         SoundUrl = soundurl;
      }

      /// <summary>
      /// ein <see cref="RunningTimer"/> ohne <see cref="EndTime"/>, <see cref="Duration"/> und <see cref="SoundUrl"/>
      /// </summary>
      /// <param name="id"></param>
      /// <param name="name"></param>
      public RunningTimer(Guid id, string name) :
         this(id, name, DateTime.MinValue, TimeSpan.Zero, "") { }

      /// <summary>
      /// erzeugt eine Kopie
      /// </summary>
      /// <param name="rt"></param>
      public RunningTimer(RunningTimer rt) :
         this(rt.Id, rt.Name, rt.EndTime, rt.Duration, rt.SoundUrl) {
         NotificationID = rt.NotificationID;
      }

      /// <summary>
      /// setzt im ClockService die <see cref="TimerServiceStepDuration"/>
      /// </summary>
      /// <param name="now"></param>
      public void CalculateTimerServiceStepDuration(DateTime now) {
         if (dt4Step != DateTime.MinValue)
            TimerServiceStepDuration = now.Subtract(dt4Step);
         dt4Step = now;
      }



      public override string ToString() {
         return string.Format("{0}: {1}; {2}; {3}",
                              Id,
                              Name,
                              EndTime.ToString("G"),
                              NotificationID);
      }

   }
}
