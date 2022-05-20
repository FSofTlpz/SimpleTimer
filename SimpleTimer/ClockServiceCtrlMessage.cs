using System;
using System.Collections.Generic;

namespace SimpleTimer {

   /// <summary>
   /// Botschaften, die der native Service senden kann
   /// </summary>
   static public class ClockServiceCtrlMessage {
      public enum Message {
         /// <summary>
         /// der Service wird gerade gestartet
         /// </summary>
         StartService,
         /// <summary>
         /// der Service wird gerade gestopt
         /// </summary>
         StopService,
         /// <summary>
         /// der Service hat den Alarm in seine Liste übernommen
         /// </summary>
         RegisterAlarm,
         /// <summary>
         /// ein oder mehrere Alarme sind ausgelöst
         /// </summary>
         Alarming,
         /// <summary>
         /// ein Alarm wurde aus der akt. Liste entfernt
         /// </summary>
         RemoveAlarm,

         /// <summary>
         /// Anforderung, dass ein Alarm aus der Liste entfernt werden soll 
         /// </summary>
         ShouldRemoveAlarm,

         /// <summary>
         /// Anforderung, dass ein ausgelöster Alarm beendet werden soll 
         /// </summary>
         ShouldStopAlarming,
      }

      public class MessageEventArgs : EventArgs {

         public Message Message { get; private set; }

         /// <summary>
         /// Liste der betroffenen <see cref="RunningTimer"/>
         /// </summary>
         public List<RunningTimer> Alarms { get; private set; }


         public MessageEventArgs(Message message, IList<RunningTimer> alarms) {
            Message = message;
            if (alarms != null)
               Alarms = new List<RunningTimer>(alarms);
            else
               Alarms = new List<RunningTimer>();
         }

         public MessageEventArgs(Message message, RunningTimer alarm) :
            this(message, new List<RunningTimer>() { alarm }) { }

         public MessageEventArgs(Message message) {
            Message = message;
         }


         public override string ToString() {
            return string.Format("{0}, {1} Alarm/s {2}",
                                 Message,
                                 Alarms != null ? Alarms.Count : 0,
                                 Alarms != null && Alarms.Count > 0 ? "(" + Alarms[0].ToString() + ")" : "");
         }

      }

   }
}
