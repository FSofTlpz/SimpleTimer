using System;
using System.Collections.Generic;

namespace SimpleTimer {

   /// <summary>
   /// Controller für den Service
   /// </summary>
   public interface IClockServiceCtrl {

      /// <summary>
      /// Message vom nativen Service
      /// </summary>
      event EventHandler<ClockServiceCtrlMessage.MessageEventArgs> NativeMessage;

      bool IsServiceActive();

      /// <summary>
      /// registriert einen laufenden Wecker und startet bei Bedarf den Service
      /// </summary>
      /// <param name="runningAlarm"></param>
      /// <returns></returns>
      bool RegisterRunningTimer(RunningTimer runningAlarm);

      /// <summary>
      /// entfernt einen laufenden Wecker und beendet den Service falls er nicht mehr benötigt wird
      /// </summary>
      /// <param name="runningAlarm"></param>
      void RemoveRunningTimer(RunningTimer runningAlarm);

      /// <summary>
      /// entfernt einen laufenden Wecker und beendet den Service falls er nicht mehr benötigt wird
      /// </summary>
      /// <param name="no"></param>
      void RemoveRunningTimer(int no);

      /// <summary>
      /// entfernt die Arlamierungsanzeige
      /// </summary>
      /// <param name="no"></param>
      void RemoveAlarming(int no);

      /// <summary>
      /// liefert alle akt. laufenden Wecker
      /// </summary>
      /// <returns></returns>
      List<RunningTimer> GetRunningTimers();

      double GetVolume();

      void SetVolume(double volume);

      void PlayTestSound(double volume);

      void StopTestSound();


      int test(string txt);

   }

}
