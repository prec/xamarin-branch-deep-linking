using System;
using Android.App;
using Android.Util;
using Firebase.Iid;
using WindowsAzure.Messaging;

namespace Crowdkeep.Time.Droid
{
    [Service]
    [IntentFilter(new[] { "com.google.firebase.INSTANCE_ID_EVENT" })]
    public class MyFirebaseIdService : FirebaseInstanceIdService
    {
        private const string Tag = "MyFirebaseIIDService";
        private NotificationHub _hub;

        public override void OnTokenRefresh()
        {
            var refreshedToken = FirebaseInstanceId.Instance.Token;
            Log.Debug(Tag, "FCM token: " + refreshedToken);
            SendRegistrationToServer(refreshedToken);
        }

        private void SendRegistrationToServer(string token)
        {
            if (string.IsNullOrEmpty(AppConstants.NotificationHubName) ||
                string.IsNullOrEmpty(AppConstants.ListenConnectionString))
            {
                throw new Exception(
                    "AppConstants.cs must be configured with the correct Azure Notification Hub settings.");
            }

            _hub = new NotificationHub(AppConstants.NotificationHubName,
                AppConstants.ListenConnectionString, this);

            var regId = _hub.Register(token).RegistrationId;

            Log.Debug(Tag, $"Successful registration of ID {regId}");
        }
    }
}