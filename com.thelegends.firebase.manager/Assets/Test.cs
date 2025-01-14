using System.Collections;
using System.Collections.Generic;
using TheLegends.Base.Firebase;
using UnityEngine;

public class Test : MonoBehaviour
{
    // Start is called before the first frame update
    private IEnumerator Start()
    {
        var defaultRemoteConfig = new Dictionary<string, object>
            {
                {"testBool" , false },
                {"testFloat" , 1.0f },
                {"testInt" , 2 },
                {"testString" , "test" }
            };
        yield return FirebaseManager.Instance.Init(defaultRemoteConfig);

        FirebaseManager.Instance.FetchRemoteData(() =>
        {
            var testBool = FirebaseManager.Instance.RemoteGetValueBoolean("testBool", false);
            var testFloat = FirebaseManager.Instance.RemoteGetValueFloat("testFloat", 1.0f);
            var testInt = FirebaseManager.Instance.RemoteGetValueInt("testInt", 2);
            var testString = FirebaseManager.Instance.RemoteGetValueString("testString", "test");
        });


    }

}
