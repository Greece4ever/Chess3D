using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine;

public class settingsApplier : MonoBehaviour
{
    public PostProcessVolume volume1;
    public PostProcessVolume volume2;

    void begin(PostProcessVolume volume, IDictionary<string, float> SETTINGS) {
        AmbientOcclusion s0;
        Bloom s1;
        ColorGrading s2;
        DepthOfField s3;


        volume.profile.TryGetSettings<AmbientOcclusion>(out s0);
        volume.profile.TryGetSettings<Bloom>(           out s1);
        volume.profile.TryGetSettings<ColorGrading>(    out s2);
        volume.profile.TryGetSettings<DepthOfField>(    out s3);

    

        s0.intensity.value     = SETTINGS["Ambient Occulusion"];           
        s1.intensity.value     = SETTINGS["Bloom Effect"];
        s2.temperature.value   = SETTINGS["Color Grading"];
        s3.focusDistance.value = SETTINGS["Depth of Field"];
        
    }

    public void main(IDictionary<string, float> SETTINGS) {
        this.begin(volume1, SETTINGS);
        this.begin(volume2, SETTINGS);
    }
}
