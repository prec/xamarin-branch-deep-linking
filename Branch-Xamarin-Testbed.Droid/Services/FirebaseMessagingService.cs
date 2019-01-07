using Android.App;
using Android.Content;
using Android.Util;
using BranchXamarinTestbed.Droid;
using Firebase.Messaging;
using Xamarin.Essentials;

namespace Crowdkeep.Time.Droid.Services
{
    [Service]
    [IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
    public class MyFirebaseMessagingService : FirebaseMessagingService
    {
        const string TAG = "MyFirebaseMessagingService";
        public override void OnMessageReceived(RemoteMessage message)
        {
            Log.Debug(TAG, "From: " + message.From);

            var notificationManager = NotificationManager.FromContext(this);

            InitializeChannels(notificationManager);

            if (IsProductionNotification(message))
            {
                SendProductionNotification(message, notificationManager, this);
            }
            else
            {
                SendTestNotification(message, notificationManager);
            }
        }

        private static bool IsProductionNotification(RemoteMessage message)
        {
            return message.GetNotification() != null;
        }

        private void SendTestNotification(RemoteMessage message, NotificationManager notificationManager)
        {
            var notification = CreateNotification(message.Data["message"], message.Data["branch"], this);
            notificationManager.Notify(0, notification);
        }

        private static void SendProductionNotification(RemoteMessage message, NotificationManager manager, Context context)
        {
            Log.Debug(TAG, "Notification Message Body: " + message.GetNotification().Body);
            var notification = CreateNotification(message.GetNotification().Body, "https://testbed-xamarin.app.link/testlink", context);
            manager.Notify(0, notification);
        }

        private static void InitializeChannels(NotificationManager manager)
        {
            if (DeviceInfo.Version.Major < 8 || manager.GetNotificationChannel("default") != null)
            {
                return;
            }

            var channel = new NotificationChannel("default", "Default Channel", NotificationImportance.Default)
            {
                Description = "Default Channel"
            };

            manager.CreateNotificationChannel(channel);
        }

        private static Notification CreateNotification(string messageBody, string link, Context context)
        {
            var pendingIntent = SetupNotificationIntent(link, context);

            var notificationBuilder = new Notification.Builder(context, "default")
                .SetContentTitle("Message")
                .SetSmallIcon(Resource.Mipmap.Icon)
                .SetContentText(messageBody)
                .SetAutoCancel(true)
                .SetContentIntent(pendingIntent)
                .SetVisibility(NotificationVisibility.Public);

            return notificationBuilder.Build();
        }

        private static PendingIntent SetupNotificationIntent(string link, Context context)
        {
            var intent = new Intent(context, typeof(MainActivity));
            intent.PutExtra("branch", link);
            intent.PutExtra("branch_force_new_session", true);

            var pendingIntent = PendingIntent.GetActivity(context, 0, intent, PendingIntentFlags.UpdateCurrent);
            return pendingIntent;
        }
    }
}