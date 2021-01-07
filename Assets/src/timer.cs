using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class timer : MonoBehaviour
{
    public GameObject TIME_TEXT;
    public int[] TIME_AVAILABLE = new int[2] {0, 45};
    public TMPro.TextMeshProUGUI text;

    System.TimeSpan TAV;
    bool ENABLED = false;
    bool finished = false;
    public float time0;


    void Start()
    {
        TIME_AVAILABLE[0] = 0;
        TIME_AVAILABLE[1] = 35;
        text = TIME_TEXT.GetComponent<TMPro.TextMeshProUGUI>();
        TAV = new System.TimeSpan(0, TIME_AVAILABLE[0], TIME_AVAILABLE[1]);   

        this.toggle();
    }

    string handle_str(string str) {
        if (str.Length == 1)
            return '0' + str;
        return str;
    }

    void toggle()
    {
        this.ENABLED = true;
        TIME_AVAILABLE[0] = 0;
        TIME_AVAILABLE[1] = 35;
        time0 = Time.time;
    }

    void stop()
    {
        this.ENABLED = false;
    }


    // Update is called once per frame
    void Update()
    { 
        if (!this.ENABLED || this.finished) return;
        System.TimeSpan time = TAV -  System.TimeSpan.FromSeconds(Time.time - time0);
        if (time.Seconds == 0 && time.Minutes == 0) {this.finished = true; return; }
        string seconds = handle_str((time.Seconds).ToString());
        string minutes = handle_str((time.Minutes).ToString());
        text.text = $"{minutes}:{seconds}";
    }
}
