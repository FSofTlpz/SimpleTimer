
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace SimpleTimer {
   [XamlCompilation(XamlCompilationOptions.Compile)]
   public partial class VolumePage : ContentPage {
      public VolumePage() {
         InitializeComponent();
      }

      protected override void OnAppearing() {
         base.OnAppearing();
         sliderVolume.Value = MainPage.SavedVolume;
      }

      protected override void OnDisappearing() {
         base.OnDisappearing();
         MainPage.SavedVolume = sliderVolume.Value;
         ClockServiceCtrl.Volume= sliderVolume.Value;
      }

      private void sliderVolume_ValueChanged(object sender, ValueChangedEventArgs e) {
         double volume = (sender as Slider).Value;
         MainPage.SavedVolume = volume;
         ClockServiceCtrl.Volume = volume;
         
         ClockServiceCtrl.StopTestSound();
         ClockServiceCtrl.PlayTestSound(volume);
      }


   }
}