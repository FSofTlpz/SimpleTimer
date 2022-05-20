using System;

namespace SimpleTimer {
   public class PickRingtoneMessage {

      public enum Message {
         /// <summary>
         /// Auswahl erfolgt
         /// </summary>
         OK,
         /// <summary>
         /// Auswahl abgebrochen
         /// </summary>
         Cancel,
      }

      public class MessageEventArgs : EventArgs {

         public Message Message { get; private set; }

         public string Uri { get; private set; }

         public string Title { get; private set; }


         public MessageEventArgs(Message message, string uri = null, string title = null) {
            Message = message;
            Uri = uri;
            Title = title;
         }

         public override string ToString() {
            return string.Format("{0}: Uri={1}, Titel={2}",
                                 Message,
                                 Uri == null ? "" : Uri,
                                 Title == null ? "" : Title);
         }

      }


   }
}
