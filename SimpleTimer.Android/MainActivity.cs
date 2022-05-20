
using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.Core.App;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleTimer.Droid {
   [Activity(Label = "SimpleTimer", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize)]
   public partial class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity {

      /// <summary>
      /// die App wird (neu) gestartet
      /// </summary>
      /// <param name="savedInstanceState"></param>
      protected override void OnCreate(Bundle savedInstanceState) {
         //TabLayoutResource = Resource.Layout.Tabbar;
         //ToolbarResource = Resource.Layout.Toolbar;
         base.OnCreate(savedInstanceState);

         if (Build.VERSION.SdkInt >= BuildVersionCodes.OMr1) {
            this.SetTurnScreenOn(true);
            /*
               Specifies whether the screen should be turned on when the Activity is resumed. Normally an activity will be transitioned 
               to the stopped state if it is started while the screen if off, but with this flag set the activity will cause the screen 
               to turn on if the activity will be visible and resumed due to the screen coming on. The screen will not be turned on if 
               the activity won't be visible after the screen is turned on. This flag is normally used in conjunction with the showWhenLocked flag 
               to make sure the activity is visible after the screen is turned on when the lockscreen is up. In addition, if this flag is set 
               and the activity calls KeyguardManager.requestDismissKeyguard(Activity, KeyguardManager.KeyguardDismissCallback) the screen will 
               turn on.
               This should be used instead of WindowManager.LayoutParams.FLAG_TURN_SCREEN_ON flag set for Windows. When using the 
               Window flag during activity startup, there may not be time to add it before the system stops your activity because 
               the screen has not yet turned on. This leads to a double life-cycle as it is then restarted.
             */
            this.SetShowWhenLocked(true);
            /*
               Specifies whether an Activity should be shown on top of the lock screen whenever the lockscreen is up and the activity is resumed. 
               Normally an activity will be transitioned to the stopped state if it is started while the lockscreen is up, but with this flag set 
               the activity will remain in the resumed state visible on-top of the lock screen.
               This should be used instead of WindowManager.LayoutParams.FLAG_SHOW_WHEN_LOCKED flag set for Windows. When using the Window flag 
               during activity startup, there may not be time to add it before the system stops your activity for being behind the lock-screen. 
               This leads to a double life-cycle as it is then restarted.
             */
         }

         Global.MainActivity = this;


         Xamarin.Essentials.Platform.Init(this, savedInstanceState);
         global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
         //LoadApplication(new App());

         // zusätzlich
         onCreateExtend(savedInstanceState,
                           new string[] {
                                 //Manifest.Permission.AccessNetworkState,
                                 //Manifest.Permission.WriteExternalStorage,    // u.a. für Cache
                                 Manifest.Permission.ReadExternalStorage,     // u.a. für Karten und Konfig.
                                 //Manifest.Permission.AccessFineLocation,      // GPS-Standort            ACHTUNG: Dieses Recht muss ZUSÄTZLICH im Manifest festgelegt sein, sonst wird es NICHT angefordert!
                           }
                        );
      }

      public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults) {
         Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

         // zusätzlich
         onRequestPermissionsResult(requestCode, permissions, grantResults);

         base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
      }


      /// <summary>
      /// an die noch aktive App wird ein Intent gesendet
      /// </summary>
      /// <param name="intent"></param>
      protected override void OnNewIntent(Intent intent) {
         System.Diagnostics.Debug.WriteLine(">>> OnNewIntent: " + intent.ToString());

         if (intent?.Extras != null) {    // i.A. von OnCreate() oder OnNewIntent() wird ein Intent geliefert und muss ev. ausgewertet werden
            NotificationHelper.IncomingAppIntent(intent);
         }
      }

      protected override void OnResume() { // unmittelbar bevor die Activitie im Vordergrund arbeitet
         base.OnResume();
         Global.ActivityIsRunning = true;
         System.Diagnostics.Debug.WriteLine(">>> OnResume");
      }

      protected override void OnPause() { // unmittelbar nach dem die Activitie in den Ruhezustand geht
         base.OnPause();
         Global.ActivityIsRunning = false;
         System.Diagnostics.Debug.WriteLine(">>> OnPause");
      }

      public override void OnAttachedToWindow() {
         base.OnAttachedToWindow();

         Window.AddFlags(WindowManagerFlags.ShowWhenLocked |
                         //WindowManagerFlags.KeepScreenOn |
                         WindowManagerFlags.DismissKeyguard |
                         WindowManagerFlags.TurnScreenOn);
      }

   }
}