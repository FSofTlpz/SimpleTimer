using System;

namespace SimpleTimer {
   static public class NotificationMessage {

      public enum Message {
         /// <summary>
         /// ein Alarm soll vorzeitig beendet werden
         /// </summary>
         ShouldCancelAlarm,
         /// <summary>
         /// eine Alarmierung soll beendet werden
         /// </summary>
         ShouldStopAlarming,
      }

      public class MessageEventArgs : EventArgs {

         public Message Message { get; private set; }

         public int No { get; private set; }

         public string Title { get; private set; }

         public string Text { get; private set; }

         public MessageEventArgs(Message msg, int no, string title, string text) {
            Message = msg;
            No = no;
            Title = title;
            Text = text;
         }

         public override string ToString() {
            return Message + ", " + No;
         }

      }

   }
}
