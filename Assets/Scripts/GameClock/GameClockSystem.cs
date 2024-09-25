using Unity.Entities;
using UnityEngine;
using TMPro;
using Unity.Mathematics;

[UpdateInGroup(typeof(Init1), OrderFirst = true)]
public partial class GameClockSystem : SystemBase {
    public static double elapsedTime;
    double timeAtGameStart;
    int previousSeconds;
    
    TextMeshProUGUI textMesh;
    readonly char[] chars = new[] { '\u200b','0',':','0','0' };

    bool _initialized;

    protected override void OnCreate() {
        base.OnCreate();

        var clockgo = GameObject.Find("Clock Text");
        Debug.Assert(clockgo != null);

        textMesh = clockgo.GetComponent<TextMeshProUGUI>();
        textMesh.SetText(chars);
        
        elapsedTime = 0;
        previousSeconds = 0;
        timeAtGameStart = UnityEngine.Time.timeAsDouble;
    }

    protected override void OnStartRunning() {
        base.OnStartRunning();

        // TODO(Hack): StageManager is running before GameClockSystem even though the order dictates otherwise. To prevent
        // the timeAtGameStart from resetting everytime the Systems pause and unpause (For instance, on player level up),
        // we use the _initialized flag here.
        if(!_initialized) {
            timeAtGameStart = UnityEngine.Time.timeAsDouble;
            _initialized = true;
        }
    }

    protected override void OnUpdate() {
        var timeNow = UnityEngine.Time.timeAsDouble;
        elapsedTime = math.max(0, timeNow - timeAtGameStart);
        var seconds = (int)elapsedTime;
        var diff = seconds - previousSeconds;

        if(diff > 0 ) {

            previousSeconds = seconds;

            // TODO: This will be correct up until 59:59
            chars[4]++;

            if(chars[4] == 58) { // ":" or "10"
                chars[4] = '0';
                chars[3]++;
            }

            if(chars[3] == '6') {
                chars[3] = chars[4] = '0';
                chars[1]++;
            }

            if(chars[1] == 58) { // ":" or "10"
                if(chars[0] == '\u200b')
                    chars[0] = '1';
                else
                    chars[0]++;

                chars[1] = chars[3] = chars[4] = '0';
            }
            textMesh.SetText(chars);
        }
    }

    public static int GetMinute() {
        return ((int)(elapsedTime / 60)) % 60;
    }

    public static string GetTimeString(double someTime) {
        if(someTime < 0) {
            return "0:00";
        }
        
        var hours   = (int)(someTime / 60) / 60;
        var minutes = ((int)(someTime / 60)) % 60;
        var seconds = (int)(someTime % 60);
        var time = "";

        if(hours > 0) {
            time += hours + ":";
            if(minutes < 10) time += "0" + minutes + ":";
            else time += minutes + ":";
        }
        else time += minutes + ":";

        if(seconds >= 10) time += seconds;
        else time += "0" + seconds.ToString();

        return time;
    }
}