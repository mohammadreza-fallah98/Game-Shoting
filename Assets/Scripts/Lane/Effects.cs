using System.Collections.Generic;
using UnityEngine;

public enum Sfx { Shoot, Hit, Kill, GateGood, GateBad, GameOver, Upgrade }

// Procedural sounds, debris bursts and camera shake. No external assets needed.
public class Effects : MonoBehaviour
{
    public static Effects Instance;

    class Debris
    {
        public Transform t;
        public Renderer r;
        public Vector3 vel;
        public float life, maxLife;
    }

    const int DebrisPoolSize = 300;

    readonly List<Debris> debris = new List<Debris>();
    readonly Dictionary<Sfx, AudioClip> clips = new Dictionary<Sfx, AudioClip>();
    readonly Dictionary<Sfx, float> lastPlayed = new Dictionary<Sfx, float>();
    AudioSource source;
    Transform cam;
    Vector3 camBase;
    float shakeAmp, shakeTime, shakeTotal;

    public void Init(Transform camera)
    {
        Instance = this;
        cam = camera;
        camBase = camera.position;

        source = gameObject.AddComponent<AudioSource>();
        source.spatialBlend = 0f;
        AudioListener.volume = PlayerPrefs.GetFloat("volume", 1f);

        clips[Sfx.Shoot] = MakeClip(900f, 400f, 0.06f, 0.1f);
        clips[Sfx.Hit] = MakeClip(300f, 200f, 0.04f, 0.4f);
        clips[Sfx.Kill] = MakeClip(200f, 60f, 0.15f, 0.5f);
        clips[Sfx.GateGood] = MakeClip(400f, 900f, 0.15f, 0f);
        clips[Sfx.GateBad] = MakeClip(300f, 80f, 0.3f, 0.3f);
        clips[Sfx.GameOver] = MakeClip(200f, 40f, 0.6f, 0.4f);
        clips[Sfx.Upgrade] = MakeClip(500f, 1000f, 0.2f, 0f);

        for (int i = 0; i < DebrisPoolSize; i++)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(go.GetComponent<Collider>());
            GameManager.Paint(go.GetComponent<Renderer>(), Color.white);
            go.transform.localScale = Vector3.one * 0.3f;
            go.SetActive(false);
            debris.Add(new Debris { t = go.transform, r = go.GetComponent<Renderer>() });
        }
    }

    // A short pitch sweep with a decaying envelope, optionally mixed with noise.
    static AudioClip MakeClip(float freqStart, float freqEnd, float duration, float noise)
    {
        const int rate = 44100;
        int n = (int)(rate * duration);
        var data = new float[n];
        float phase = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)n;
            float freq = Mathf.Lerp(freqStart, freqEnd, t);
            phase += 2f * Mathf.PI * freq / rate;
            float env = (1f - t) * (1f - t);
            data[i] = (Mathf.Sin(phase) * (1f - noise) + (Random.value * 2f - 1f) * noise) * env;
        }
        var clip = AudioClip.Create("sfx", n, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    public static void Play(Sfx sfx, float volume = 0.5f)
    {
        if (Instance == null) return;
        // Throttle: many cubes can trigger the same sound in one frame.
        if (Instance.lastPlayed.TryGetValue(sfx, out float last) && Time.unscaledTime - last < 0.04f) return;
        Instance.lastPlayed[sfx] = Time.unscaledTime;
        Instance.source.PlayOneShot(Instance.clips[sfx], volume);
    }

    public static void Burst(Vector3 pos, Color color, int count)
    {
        if (Instance == null) return;
        int spawned = 0;
        foreach (var d in Instance.debris)
        {
            if (spawned >= count) break;
            if (d.t.gameObject.activeSelf) continue;
            d.t.position = pos;
            d.vel = Random.onUnitSphere * Random.Range(4f, 10f);
            d.vel.y = Mathf.Abs(d.vel.y) + 3f;
            d.life = d.maxLife = Random.Range(0.4f, 0.8f);
            d.r.material.color = color;
            d.t.localScale = Vector3.one * 0.3f;
            d.t.gameObject.SetActive(true);
            spawned++;
        }
    }

    public static void Shake(float amplitude, float duration)
    {
        if (Instance == null) return;
        Instance.shakeAmp = Mathf.Max(Instance.shakeAmp, amplitude);
        Instance.shakeTime = Instance.shakeTotal = duration;
    }

    void Update()
    {
        foreach (var d in debris)
        {
            if (!d.t.gameObject.activeSelf) continue;
            d.life -= Time.deltaTime;
            if (d.life <= 0f) { d.t.gameObject.SetActive(false); continue; }
            d.vel.y -= 25f * Time.deltaTime;
            d.t.position += d.vel * Time.deltaTime;
            d.t.localScale = Vector3.one * 0.3f * (d.life / d.maxLife);
        }
    }

    void LateUpdate()
    {
        if (cam == null) return;
        if (shakeTime > 0f)
        {
            shakeTime -= Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(shakeTime / shakeTotal);
            cam.position = camBase + Random.insideUnitSphere * shakeAmp * k;
        }
        else
        {
            shakeAmp = 0f;
            cam.position = camBase;
        }
    }
}
