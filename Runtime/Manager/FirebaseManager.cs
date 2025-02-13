using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
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

        public void Init()
        {
            StartCoroutine(DoInit());
        }

        public IEnumerator DoInit(Dictionary<string, object> remoteDefaultConfig = null)
        {
#if USE_FIREBASE
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
                        Debug.Log(TAG + " CheckDependencies: " + task.Result);
                    }
                    else
                    {
                        Status = FirebaseStatus.InitializeFailed;
                        Debug.LogError(TAG + " CheckDependencies: " + task.Result);
                    }
                }
                else
                {
                    Status = FirebaseStatus.InitializeFailed;
                    Debug.LogError(TAG + " CheckDependencies: task NULL");
                }
            });

            while (Status == FirebaseStatus.Initializing)
            {
                yield return null;
            }

            if (Status == FirebaseStatus.Available)
            {
                //Initialing Firebase Analytics

                Debug.Log(TAG + " Analytics " + "Initialing");

                try
                {
                    FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
                    Debug.Log(TAG + " Analytics " + "Initialized");
                }
                catch (FirebaseException ex)
                {
                    Debug.LogError(TAG + " Analytics " + "Initialized " + ex.Message + "\n" + ex.StackTrace);
                }
                catch (Exception ex)
                {
                    Debug.LogError(TAG + " Analytics " + "Initialized " + ex.Message + "\n" + ex.StackTrace);
                }


                //Initialing Firebase Remote

                if (remoteDefaultConfig != null && remoteDefaultConfig.Count > 0)
                {
                    Status = FirebaseStatus.Fetching;

                    Debug.Log(TAG + " Remote Config " + "Initialing");

                    try
                    {
                        FirebaseRemoteConfig.DefaultInstance.SetDefaultsAsync(remoteDefaultConfig)
                            .ContinueWithOnMainThread(task =>
                            {
                                Debug.Log(TAG + " RemoteConfig " + "Initialized");
                                Status = FirebaseStatus.Fetched;
                            });
                    }
                    catch (FirebaseException ex)
                    {
                        Debug.LogError(TAG + " RemoteConfig " + "Initialized " + ex.Message + "\n" + ex.StackTrace);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError(TAG + " RemoteConfig " + "Initialized " + ex.Message + "\n" + ex.StackTrace);
                    }
                }

                while (Status == FirebaseStatus.Fetching)
                {
                    yield return null;
                }

                Status = FirebaseStatus.Initialized;
            }
#else
            yield return null;
#endif
        }

        #region Firebase Analytics

        public void LogEvent(string eventName, Dictionary<string, object> parameters = null)
        {
#if USE_FIREBASE
            if (Status != FirebaseStatus.Initialized)
            {
                Debug.LogWarning("Firebase not initialized");
                return;
            }

            if (string.IsNullOrEmpty(eventName))
            {
                Debug.LogWarning("eventName IsNullOrEmpty");
                return;
            }

            if (eventName.Length >= 32)
            {
                eventName = eventName.Substring(0, 32).ToLower();
            }

            try
            {
                Debug.Log(TAG + " Log Event " + eventName);

                if (parameters != null)
                {
                    var param = parameters.Select(x =>
                    {
                        if (x.Key != null && x.Value != null)
                        {
                            if (x.Value is float)
                            {
                                return new Parameter(x.Key.ToLower(), (float)x.Value);
                            } else if (x.Value is double)
                            {
                                return new Parameter(x.Key.ToLower(), (double)x.Value);
                            } else if (x.Value is long)
                            {
                                return new Parameter(x.Key.ToLower(), (long)x.Value);
                            } else if (x.Value is int)
                            {
                                return new Parameter(x.Key.ToLower(), (int)x.Value);
                            } else if (x.Value is string)
                            {
                                return new Parameter(x.Key.ToLower(), x.Value.ToString());
                            }
                        }
                        return null;
                    }).ToArray();

                    if (param != null)
                    {

                        FirebaseAnalytics.LogEvent(eventName, param);

                        string paramStr = "";

                        foreach (var par in parameters)
                        {
                            paramStr += par.Key + " " + par.Value + " | ";
                        }

                        Debug.Log(TAG + " Param " + paramStr);
                    }
                }
                else
                {
                    FirebaseAnalytics.LogEvent(eventName);
                }
            }
            catch (FirebaseException ex)
            {
                Debug.LogError(TAG + "LogEvent FirebaseException: " + ex.Message);
            }
            catch (Exception ex)
            {
                Debug.LogError(TAG + "LogEvent Exception: " + ex.Message);
            }
#endif

        }

        #endregion

        #region Firebase Remote

        public void FetchRemoteData(Action OnFetchCompleted, int cacheExpirationHours = 12)
        {
#if USE_FIREBASE
            if (Status != FirebaseStatus.Initialized)
            {
                Debug.LogWarning("Firebase not initialized");
                return;
            }

            try
            {
                FirebaseRemoteConfig.DefaultInstance.FetchAsync(TimeSpan.FromHours(cacheExpirationHours)).ContinueWithOnMainThread(task =>
                {
                    if (!task.IsCompleted) {
                        Debug.LogError("Retrieval hasn't finished.");
                        return;
                    }

                    var remoteConfig = FirebaseRemoteConfig.DefaultInstance;
                    var info = remoteConfig.Info;
                    if (info.LastFetchStatus != LastFetchStatus.Success) {
                        Debug.LogError($"{nameof(task)} was unsuccessful\n{nameof(info.LastFetchStatus)}: {info.LastFetchStatus}");
                        return;
                    }

                    // Fetch successful. Parameter values must be activated to use.
                    remoteConfig.ActivateAsync().ContinueWithOnMainThread(task =>
                    {
                        OnFetchCompleted?.Invoke();
                        Debug.Log($"Remote data loaded and ready for use. Last fetch time {info.FetchTime}.");
                    });
                });
            }

            catch (FirebaseException ex)
            {
                Debug.LogError(TAG + "DoFetchRemoteData FirebaseException: " + ex.Message);
            }
            catch (Exception ex)
            {
                Debug.LogError(TAG + "DoFetchRemoteData Exception: " + ex.Message);
            }
#endif

        }

        public string RemoteGetValueString(string title, string defaultValue)
        {
            return RemoteGetValue(title, defaultValue, value => value.StringValue);
        }

        public int RemoteGetValueInt(string title, int defaultValue)
        {
            return RemoteGetValue(title, defaultValue, value => (int)value.LongValue);
        }

        public bool RemoteGetValueBoolean(string title, bool defaultValue)
        {
            return RemoteGetValue(title, defaultValue, value => value.BooleanValue);
        }

        public float RemoteGetValueFloat(string title, float defaultValue)
        {
            return RemoteGetValue(title, defaultValue, value =>
            {
                var style = NumberStyles.Float;
                var culture = CultureInfo.CreateSpecificCulture("en-US");
                if (float.TryParse(value.StringValue, style, culture, out float result))
                {
                    return result;
                }
                return (float)value.DoubleValue;
            });
        }

        private T RemoteGetValue<T>(string title, T defaultValue, Func<ConfigValue, T> getValue)
        {
            if (Status != FirebaseStatus.Initialized)
            {
                Debug.LogWarning("Firebase not initialized");
                return defaultValue;
            }

            try
            {
#if USE_FIREBASE
                if (FirebaseRemoteConfig.DefaultInstance.Keys != null && FirebaseRemoteConfig.DefaultInstance.Keys.Contains(title))
                {
                    var value = FirebaseRemoteConfig.DefaultInstance.GetValue(title);
                    Debug.Log($"-------> {TAG} Remote: {title} | {getValue(value)}");
                    return getValue(value);
                }
                else
                {
                    Debug.LogWarning($"-------> {TAG} Remote: {title} NOT FOUND");
                }
#endif
            }
            catch (Exception ex)
            {
                Debug.LogError($"-------> {TAG} Remote: {title} Exception: {ex.Message}");
                Debug.LogException(ex);
            }
            return defaultValue;
        }



        #endregion
    }

    public enum FirebaseStatus
    {
        None = 0,
        Available = 1,
        Initializing = 2,
        Initialized = 3,
        InitializeFailed = 4,
        Fetching = 5,
        Fetched = 6,
    }
}
