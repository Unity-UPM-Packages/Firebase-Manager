using System;
using System.Collections;
using System.Collections.Generic;
using Firebase;
using Firebase.Analytics;
using Firebase.Extensions;
using Firebase.RemoteConfig;
using TheLegends.Base.UnitySingleton;
using UnityEngine;

namespace TheLegends.Base.Firebase
{
    public class FirebaseManager : PersistentMonoSingleton<FirebaseManager>
    {
        private static string TAG = "Firebase";
        private FirebaseStatus _status = FirebaseStatus.None;

        public FirebaseStatus Status
        {
            get => _status;
            set
            {
                if (value != _status)
                {
                    _status = value;
                    Debug.Log("Firebase Status: " + value);
                }
            }
        }

        public IEnumerator Init(Dictionary<string, object> remoteDefaultConfig = null)
        {
            if (Status == FirebaseStatus.Initializing)
            {
                yield break;
            }

            Status = FirebaseStatus.Initializing;

            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                if (task != null)
                {
                    if (task.Result == DependencyStatus.Available)
                    {
                        Status = FirebaseStatus.Available;
                        Debug.Log(TAG + "CheckDependencies: " + task.Result);
                    }
                    else
                    {
                        Status = FirebaseStatus.InitializeFailed;
                        Debug.LogError(TAG + "CheckDependencies: " + task.Result);
                    }
                }
                else
                {
                    Status = FirebaseStatus.InitializeFailed;
                    Debug.LogError(TAG + "CheckDependencies: task NULL");
                }
            });

            while (Status == FirebaseStatus.Initializing)
            {
                yield return null;
            }

            if (Status == FirebaseStatus.Available)
            {
                //Initialing Firebase Analytics

                Debug.Log(TAG + "Analytics " + "Initialing");

                try
                {
                    FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
                    Debug.Log(TAG + "Analytics " + "Initialized");
                }
                catch (FirebaseException ex)
                {
                    Debug.LogError(TAG + "Analytics " + "Initialized " + ex.Message + "\n" + ex.StackTrace);
                }
                catch (Exception ex)
                {
                    Debug.LogError(TAG + "Analytics " + "Initialized " + ex.Message + "\n" + ex.StackTrace);
                }


                //Initialing Firebase Remote

                if (remoteDefaultConfig != null && remoteDefaultConfig.Count > 0)
                {
                    Debug.Log(TAG + "Remote Config " + "Initialing");

                    try
                    {
                        FirebaseRemoteConfig.DefaultInstance.SetDefaultsAsync(remoteDefaultConfig)
                            .ContinueWithOnMainThread(task => { Debug.Log(TAG + "RemoteConfig " + "Initialized"); });
                    }
                    catch (FirebaseException ex)
                    {
                        Debug.LogError(TAG + "RemoteConfig " + "Initialized " + ex.Message + "\n" + ex.StackTrace);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError(TAG + "RemoteConfig " + "Initialized " + ex.Message + "\n" + ex.StackTrace);
                    }
                }

                Status = FirebaseStatus.Initialized;
            }
        }
    }

    public enum FirebaseStatus
    {
        None = 0,
        Available = 1,
        Initializing = 2,
        Initialized = 3,
        InitializeFailed = 4
    }
}
