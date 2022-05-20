using System.Collections.Generic;
using Xamarin.Forms;

namespace SimpleTimer {
   class ClockServiceCtrl {

      public static bool IsServiceActive() {
         return DependencyService.Get<IClockServiceCtrl>().IsServiceActive();
      }

      public static bool RegisterRunningTimer(RunningTimer runningAlarm) {
         return DependencyService.Get<IClockServiceCtrl>().RegisterRunningTimer(runningAlarm);
      }

      public static void RemoveRunningTimer(RunningTimer runningAlarm) {
         DependencyService.Get<IClockServiceCtrl>().RemoveRunningTimer(runningAlarm);
      }

      public static void RemoveRunningTimer(int no) {
         DependencyService.Get<IClockServiceCtrl>().RemoveRunningTimer(no);
      }

      public static void RemoveAlarming(int no) {
         DependencyService.Get<IClockServiceCtrl>().RemoveAlarming(no);
      }

      public static List<RunningTimer> GetRunningTimers() {
         return DependencyService.Get<IClockServiceCtrl>().GetRunningTimers();
      }

      public static double Volume {
         get {
            return DependencyService.Get<IClockServiceCtrl>().GetVolume();
         }
         set {
            DependencyService.Get<IClockServiceCtrl>().SetVolume(value);
         }
      }

      public static void PlayTestSound(double volume) {
         DependencyService.Get<IClockServiceCtrl>().PlayTestSound(volume);
      }

      public static void StopTestSound() {
         DependencyService.Get<IClockServiceCtrl>().StopTestSound();
      }

   }
}
