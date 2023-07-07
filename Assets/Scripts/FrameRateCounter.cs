using TMPro;
using UnityEngine;
public class FrameRateCounter : MonoBehaviour {
    public enum DisplayMode { FPS, MS }
    
    [SerializeField]
    private TextMeshProUGUI display;
    [SerializeField]
    private DisplayMode displayMode = DisplayMode.FPS;
    [SerializeField] [Range(0.1f, 2f)]
    private float sampleDuration = 1f;

    private float duration, bestDuration = float.MaxValue, worstDuration;
    private int frames;

    private void Update() {
        float frameDuration = Time.unscaledDeltaTime;
        this.frames += 1;
        this.duration += frameDuration;

        if (frameDuration < this.bestDuration) {
            this.bestDuration = frameDuration;
        }
        if (frameDuration > this.worstDuration) {
            this.worstDuration = frameDuration;
        }

        if (this.duration >= this.sampleDuration) {
            if (this.displayMode == DisplayMode.FPS) {
                this.display.SetText("FPS\n{0:0}\n{1:0}\n{2:0}", 1f / this.bestDuration, this.frames / this.duration, 1f / this.worstDuration);
            }
            else {
                this.display.SetText("MS\n{0:1}\n{1:1}\n{2:1}", 1000f * this.bestDuration, 1000f * this.duration / this.frames, 1000f * this.worstDuration);
            }
            this.frames = 0;
            this.duration = 0f;
            this.bestDuration = float.MaxValue;
            this.worstDuration = 0f;
        }
    }
}