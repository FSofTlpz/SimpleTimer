using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace SimpleTimer {
   public partial class App : Application {

      public App() {
         InitializeComponent();

         MainPage = new MainPage();
      }

      public App(object androidactivity) : this() {
         InitializeComponent();

         MainPage = new NavigationPage(new MainPage() { 
            AndroidActivity = androidactivity,
         }) {
            //BarBackgroundColor = Color.LightGreen,
            //BarTextColor = Color.Black,
         };
      }


      protected override void OnStart() {
      }

      protected override void OnSleep() {
      }

      protected override void OnResume() {
      }
   }
}
