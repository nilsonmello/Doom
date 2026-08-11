using System.Collections;
using UnityEngine;

public class HitStopManager : MonoBehaviour
{
    private static HitStopManager instance;

    private Coroutine activeRoutine;
    private float previousTimeScale = 1f;

    public static void Request(float duration)
    {
        if (duration <= 0f) return;

        if (instance == null)
        {
            var go = new GameObject("HitStopManager");
            instance = go.AddComponent<HitStopManager>();
            DontDestroyOnLoad(go);
        }

        instance.RequestInternal(duration);
    }

    private void RequestInternal(float duration)
    {
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
        }
        else
        {
            previousTimeScale = Time.timeScale;
        }

        activeRoutine = StartCoroutine(DoHitStop(duration));
    }

    private IEnumerator DoHitStop(float duration)
    {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = previousTimeScale;
        activeRoutine = null;
    }
}
