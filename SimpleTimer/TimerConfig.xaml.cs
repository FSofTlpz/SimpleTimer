using FSofTUtils.Xamarin.Control;
using FSofTUtils.Xamarin.Page;
using System;
using System.Collections.Generic;
using System.Threading;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace SimpleTimer {
   [XamlCompilation(XamlCompilationOptions.Compile)]
   public partial class TimerConfig : ContentPage {

      public TimerData TimerData = null;

      public IList<string> InternalAudiofiles;


      #region Binding-Vars

      public static BindableProperty TimerNameProperty = BindableProperty.Create(
         nameof(TimerName),
         typeof(string),
         typeof(TimerConfig),
         "",
         BindingMode.TwoWay);

      public string TimerName {
         get => GetValue(TimerNameProperty) as string;
         set => SetValue(TimerNameProperty, value);
      }

      public static BindableProperty SoundTitleProperty = BindableProperty.Create(
         nameof(SoundTitle),
         typeof(string),
         typeof(TimerConfig),
         "",
         BindingMode.TwoWay);

      public string SoundTitle {
         get => GetValue(SoundTitleProperty) as string;
         set => SetValue(SoundTitleProperty, value);
      }

      #endregion

      TimeSpan timerDuration;

      string soundUrl;

      /// <summary>
      /// Die Zeit des TimeWheel ist während der Init. NICHT setzbar. Zur Steuerung wird das Event verwendet.
      /// </summary>
      readonly ManualResetEvent manualResetEventIsSettable = new ManualResetEvent(false);

      public event EventHandler<EventArgs> SaveEvent;


      public TimerConfig() {
         InitializeComponent();

         TimeWheel.TimeSettableStatusChangedEvent += TimeWheel_TimeSettableStatusChangedEvent;
      }

      private void TimeWheel_TimeSettableStatusChangedEvent(object sender, TimeWheelView.TimeSettableStatusChangedEventArgs args) {
         if (args.IsSettable)
            manualResetEventIsSettable.Set();
         else
            manualResetEventIsSettable.Reset();
      }


      bool firstAppearing = true;

      protected override void OnAppearing() {
         base.OnAppearing();

         if (firstAppearing) {
            Title = "Timer bearbeiten";
            TimerName = TimerData.Name.Trim();
            timerDuration = TimerData.Duration;
            SoundTitle = TimerData.SoundTitle;
            soundUrl = TimerData.SoundUrl;

            firstAppearing = false;
         }


         TimeWheel.TimeSpan = timerDuration;

         //System.Threading.Tasks.Task.Run(() => {
         //   manualResetEventIsSettable.WaitOne();     // warten, bis das TimeWheel bereit ist
         //   Xamarin.Essentials.MainThread.BeginInvokeOnMainThread(() => {  // synchronisiert mit dem Hauptthread (UI !)
         //      TimeWheel.TimeSpan = timerDuration;
         //   });
         //});

      }

      protected override void OnDisappearing() {
         base.OnDisappearing();

         timerDuration = TimeWheel.TimeSpan;
      }

      private void ButtonSave_Clicked(object sender, EventArgs e) {
         TimerData.Name = TimerName.Trim();
         TimerData.Duration = TimeWheel.TimeSpan;
         TimerData.SetSound(SoundTitle, soundUrl);
         SaveEvent?.Invoke(this, new EventArgs());
         Navigation.PopAsync();
      }

      private async void ButtonSound_Clicked(object sender, EventArgs e) {
         //Native.StartPickRingtone(soundUrl);
         SoundPickerPage soundPickerPage = new SoundPickerPage();
         soundPickerPage.AddAdditionalAudiofiles(InternalAudiofiles);
         soundPickerPage.InitSound(soundUrl);
         soundPickerPage.CloseEvent += SoundPickerPage_CloseEvent;

         await Navigation.PushAsync(soundPickerPage);
      }

      private void SoundPickerPage_CloseEvent(object sender, SoundPickerPage.CloseEventArgs e) {
         if (e.NativeSoundData != null) {
            soundUrl = e.NativeSoundData.Data;
            SoundTitle = e.NativeSoundData.Title;
         }
      }

   }
}