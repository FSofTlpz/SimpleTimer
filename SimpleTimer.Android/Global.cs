using Android.Content;
using System.Threading;

namespace SimpleTimer.Droid {
   static class Global {

      static public MainActivity MainActivity;

      static object _clockService = null;

      static public ClockService ClockService {
         get {
            return Interlocked.Exchange(ref _clockService, _clockService) as ClockService;
         }
         set {
            Interlocked.Exchange(ref _clockService, value);
         }
      }

      static object _clockServiceCtrl = null;

      static public ClockServiceCtrl ClockServiceCtrl {
         get {
            return Interlocked.Exchange(ref _clockServiceCtrl, _clockServiceCtrl) as ClockServiceCtrl;
         }
         set {
            Interlocked.Exchange(ref _clockServiceCtrl, value);
         }
      }

      /// <summary>
      /// falls die App nicht im Vordergrund läuft, wird sie gestartet
      /// <para>
      /// https://developer.android.com/reference/android/content/Intent#FLAG_ACTIVITY_NEW_TASK
      /// </para>
      /// </summary>
      static public void StartActivity() {
         var intent = new Intent(Android.App.Application.Context, typeof(MainActivity));
         intent.AddFlags(ActivityFlags.SingleTop);    // If set, the activity will not be launched if it is already running at the top of the history stack.
         intent.AddFlags(ActivityFlags.NewTask);      // If set, this activity will become the start of a new task on this history stack.
         Android.App.Application.Context.StartActivity(intent);
      }


      static public bool ActivityIsRunning;

   }
}