using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using HY.MAUI.Interfaces;

namespace HY.MAUI.Platforms.Android
{
    [Activity(Theme = "@style/Maui.SplashTheme", ResizeableActivity = true, MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            CreateNotificationFromIntent(Intent);
        }

        protected override void OnNewIntent(Intent? intent)
        {
            base.OnNewIntent(intent);

            CreateNotificationFromIntent(intent);
        }

        static void CreateNotificationFromIntent(Intent intent)
        {
            if (intent?.Extras != null)
            {
                string title = intent.GetStringExtra(NotificationManagerService.TitleKey);
                string message = intent.GetStringExtra(NotificationManagerService.MessageKey);

                var service = IPlatformApplication.Current.Services.GetService<INotificationManagerService>();
                service.ReceiveNotification(title, message);
            }
        }

        
        
        //private static bool _leftShiftDown;
        //private static bool _rightShiftDown;

        //internal static bool IsPhysicalShiftPressed => _leftShiftDown || _rightShiftDown;

        //public override bool DispatchKeyEvent(KeyEvent? e)
        //{
        //    if (e != null)
        //    {
        //        bool isDown = e.Action == KeyEventActions.Down;
        //        bool isUp = e.Action == KeyEventActions.Up;

        //        if (e.KeyCode == Keycode.ShiftLeft)
        //        {
        //            if (isDown)
        //                _leftShiftDown = true;
        //            else if (isUp)
        //                _leftShiftDown = false;
        //        }
        //        else if (e.KeyCode == Keycode.ShiftRight)
        //        {
        //            if (isDown)
        //                _rightShiftDown = true;
        //            else if (isUp)
        //                _rightShiftDown = false;
        //        }
        //    }

        //    return base.DispatchKeyEvent(e);
        //}

        //protected override void OnStop()
        //{
        //    _leftShiftDown = false;
        //    _rightShiftDown = false;

        //    base.OnStop();
        //}




    }
}
