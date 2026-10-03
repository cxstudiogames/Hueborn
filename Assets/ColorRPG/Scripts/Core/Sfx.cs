// =====================================================================
//  Sfx.cs - sunete (Kenney CC0 + sunete proprii sintetizate, in Resources/Audio/<Categorie>/)
//  + muzica per harta / piata / boss (Resources/Audio/Music) + ambient in bucla (Resources/Audio/Ambient).
// =====================================================================
using System.Collections.Generic;
using UnityEngine;

namespace ColorRPG
{
    public class Sfx : MonoBehaviour
    {
        public static Sfx I;

        readonly Dictionary<string, AudioClip[]> banks = new Dictionary<string, AudioClip[]>();
        readonly List<AudioSource> pool = new List<AudioSource>();
        readonly Dictionary<string, float> lastPlay = new Dictionary<string, float>();
        AudioSource music;
        readonly Dictionary<int, AudioClip> songs = new Dictionary<int, AudioClip>();
        int songIdx = -1;
        float musicTarget = 0.32f;

        public bool Muted { get; private set; }
        public float SfxVol = 0.8f;
        const float MusicBase = 0.32f;

        /// <summary>Volumul muzicii ambientale (0..1), din Settings.</summary>
        public void SetMusicVolume(float v)
        {
            musicTarget = MusicBase * Mathf.Clamp01(v);
            StopCoroutine("DuckRoutine");
            if (music != null && music.isPlaying) music.volume = musicTarget;
        }

        public static Sfx Create()
        {
            var go = new GameObject("Sfx");
            go.transform.SetParent(SceneGroups.Get("Systems"), false);
            I = go.AddComponent<Sfx>();
            return I;
        }

        void Awake()
        {
            I = this;
            for (int i = 0; i < 14; i++)
            {
                var s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 0f;
                pool.Add(s);
            }
            music = gameObject.AddComponent<AudioSource>();
            music.loop = true;
            music.playOnAwake = false;
            music.volume = 0f;
            amb = gameObject.AddComponent<AudioSource>();
            amb.loop = true;
            amb.playOnAwake = false;
            amb.volume = 0f;
            Muted = PlayerPrefs.GetInt("crpg_muted", 0) == 1;
            FadeIn(6f);   // la pornirea jocului sunetul nu porneste brusc (pe PC si pe telefon)
        }

        // inainte de prima scena: liniste (altfel primul cadru / ecranul de incarcare porneste la volum maxim)
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void StartQuiet() { AudioListener.volume = 0f; }

        // ---- volum general care creste treptat (intrare in joc / schimbare de harta): 10% -> 100% in cateva secunde
        float fadeK = 1f, fadeDur = 4f;
        /// <summary>Sunetul porneste incet (de la ~10%) si creste treptat pana la volumul normal.</summary>
        public void FadeIn(float seconds = 4f)
        {
            fadeK = 0.1f;
            fadeDur = Mathf.Max(0.5f, seconds);
            AudioListener.volume = Muted ? 0f : fadeK;
        }

        public void ToggleMute()
        {
            Muted = !Muted;
            AudioListener.volume = Muted ? 0f : fadeK;
            PlayerPrefs.SetInt("crpg_muted", Muted ? 1 : 0);
        }

        AudioClip[] Bank(string cat)
        {
            AudioClip[] b;
            if (banks.TryGetValue(cat, out b)) return b;
            b = Resources.LoadAll<AudioClip>("Audio/" + cat);
            banks[cat] = b;
            return b;
        }

        /// <summary>Reda un sunet aleator din categoria data. minGap = anti-spam (secunde).</summary>
        public static void Play(string cat, float vol = 1f, float pitch = 1f, float pitchVar = 0.08f, float minGap = 0.03f)
        {
            if (I == null) return;
            I.PlayInternal(cat, vol, pitch, pitchVar, minGap);
        }

        void PlayInternal(string cat, float vol, float pitch, float pitchVar, float minGap)
        {
            float t;
            if (lastPlay.TryGetValue(cat, out t) && Time.unscaledTime - t < minGap) return;
            var bank = Bank(cat);
            if (bank == null || bank.Length == 0) return;
            lastPlay[cat] = Time.unscaledTime;
            AudioSource src = null;
            foreach (var s in pool) if (!s.isPlaying) { src = s; break; }
            if (src == null) src = pool[Random.Range(0, pool.Count)];
            src.clip = bank[Random.Range(0, bank.Length)];
            src.volume = vol * SfxVol;
            src.pitch = pitch * (1f + Random.Range(-pitchVar, pitchVar));
            src.Play();
        }

        /// <summary>Sunet doar daca jucatorul e aproape de pozitie (ex. un boss care apare).</summary>
        public static void PlayNear(string cat, Vector3 pos, float maxDist, float vol = 1f)
        {
            var g = Game.I;
            if (I == null || g == null || g.player == null) return;
            float d = Vector3.Distance(g.player.transform.position, pos);
            if (d > maxDist) return;
            Play(cat, vol * Mathf.Lerp(1f, 0.35f, d / maxDist), 1f, 0.03f);
        }

        // ---------------------------------------------------------------
        //  Muzica (piese pre-randate in Resources/Audio/Music, fallback: generator chiptune)
        //  + sunet ambiental in bucla per harta (Resources/Audio/Ambient)
        //  + tema de boss cand un boss e aproape
        // ---------------------------------------------------------------
        AudioSource amb;
        readonly Dictionary<string, AudioClip> clipCache = new Dictionary<string, AudioClip>();
        string curKey = "", ambKey = "";
        bool bossMode;
        float bossCheckT, bossLostT;
        const float AmbBase = 0.42f;

        AudioClip LoadClip(string path)
        {
            AudioClip c;
            if (clipCache.TryGetValue(path, out c)) return c;
            c = Resources.Load<AudioClip>(path);
            clipCache[path] = c;
            return c;
        }

        static string ZoneName(int zone) { return zone == GameData.PlazaZone ? "Plaza" : "Map" + zone; }

        public void PlaySong(int mapIdx)
        {
            if (songIdx == mapIdx && curKey != "") return;
            songIdx = mapIdx;
            bossMode = false;
            SwitchMusic(ZoneName(mapIdx));
            SetAmbient(ZoneName(mapIdx));
        }

        void SwitchMusic(string key)
        {
            if (curKey == key) return;
            curKey = key;
            AudioClip c = LoadClip("Audio/Music/" + key);
            if (c == null)
            {
                int idx = key.StartsWith("Boss") ? 3 : songIdx;
                if (!songs.TryGetValue(idx, out c)) { c = MusicGen.Make(idx); songs[idx] = c; }
            }
            StopCoroutine("SwapSong");
            StartCoroutine("SwapSong", c);
        }

        void SetAmbient(string key)
        {
            if (ambKey == key) return;
            ambKey = key;
            var c = LoadClip("Audio/Ambient/" + key);
            StopCoroutine("SwapAmbient");
            StartCoroutine("SwapAmbient", c);
        }

        // in Trade Plaza ambientul (apa fantanii) e foarte incet
        float AmbTarget { get { return AmbBase * Mathf.Clamp01(SfxVol / 0.8f) * (ambKey == "Plaza" ? 0.12f : 1f); } }

        System.Collections.IEnumerator SwapAmbient(AudioClip c)
        {
            while (amb.isPlaying && amb.volume > 0.01f)
            {
                amb.volume = Mathf.MoveTowards(amb.volume, 0f, Time.unscaledDeltaTime * 0.6f);
                yield return null;
            }
            if (c == null) { amb.Stop(); yield break; }
            amb.clip = c;
            amb.time = Random.Range(0f, c.length * 0.9f);
            amb.Play();
            while (amb.volume < AmbTarget - 0.001f)
            {
                amb.volume = Mathf.MoveTowards(amb.volume, AmbTarget, Time.unscaledDeltaTime * 0.3f);
                yield return null;
            }
        }

        System.Collections.IEnumerator SwapSong(AudioClip c)
        {
            while (music.isPlaying && music.volume > 0.01f)
            {
                music.volume = Mathf.MoveTowards(music.volume, 0f, Time.unscaledDeltaTime * (bossMode ? 2.5f : 0.8f));
                yield return null;
            }
            music.clip = c;
            music.Play();
            while (music.volume < musicTarget - 0.001f)
            {
                music.volume = Mathf.MoveTowards(music.volume, musicTarget, Time.unscaledDeltaTime * (bossMode ? 1.2f : 0.4f));
                yield return null;
            }
        }

        void Update()
        {
            if (fadeK < 1f)
            {
                // cat timp e ecranul de incarcare / conectare: ramane la 10%; apoi urca lent (10% -> 30%) si mai repede pana la 100%.
                // pasul e limitat (un cadru lung la incarcare nu mai sare direct la volum maxim)
                bool hold = LoadingScreen.Active || SignInScreen.Open;
                if (!hold) fadeK = Mathf.Min(1f, fadeK + Mathf.Min(Time.unscaledDeltaTime, 0.05f) / fadeDur * (0.35f + fadeK));
                AudioListener.volume = Muted ? 0f : fadeK;
            }
            // volumul ambiental urmeaza volumul efectelor
            if (amb != null && amb.isPlaying && Mathf.Abs(amb.volume - AmbTarget) > 0.02f && amb.volume > 0.05f)
                amb.volume = Mathf.MoveTowards(amb.volume, AmbTarget, Time.unscaledDeltaTime * 0.5f);

            // tema de boss: un boss viu, aproape de jucator, pe harta curenta
            bossCheckT -= Time.unscaledDeltaTime;
            if (bossCheckT > 0f) return;
            bossCheckT = 0.5f;
            var g = Game.I;
            if (g == null || g.player == null || songIdx < 0) return;
            bool near = false;
            if (!g.InPlaza && !g.player.dead)
            {
                Vector3 pp = g.player.transform.position;
                foreach (var e in Enemy.All)
                {
                    if (e == null || !e.boss || e.Dying || e.mapIdx != g.mapIndex) continue;
                    Vector3 d = e.transform.position - pp; d.y = 0;
                    if (d.magnitude < 24f) { near = true; break; }
                }
            }
            if (near) { bossLostT = 4f; if (!bossMode) { bossMode = true; SwitchMusic("Boss" + Mathf.Clamp(songIdx, 0, 3)); } }
            else if (bossMode)
            {
                bossLostT -= 0.5f;
                if (bossLostT <= 0f) { bossMode = false; SwitchMusic(ZoneName(songIdx)); }
            }
        }

        /// <summary>Coboara muzica temporar (ex. la jingle de level up).</summary>
        public void Duck(float seconds)
        {
            StartCoroutine(DuckRoutine(seconds));
        }

        System.Collections.IEnumerator DuckRoutine(float s)
        {
            float old = musicTarget;
            musicTarget = old * 0.35f;
            music.volume = musicTarget;
            yield return new WaitForSecondsRealtime(s);
            musicTarget = old;
            while (music.volume < musicTarget - 0.001f)
            {
                music.volume = Mathf.MoveTowards(music.volume, musicTarget, Time.unscaledDeltaTime * 0.5f);
                yield return null;
            }
        }
    }

    /// <summary>
    /// Generator de muzica chiptune (cod propriu => fara probleme de licenta).
    /// Fiecare harta are tempo, gama si instrument diferite.
    /// </summary>
    public static class MusicGen
    {
        const int SR = 22050;

        public static AudioClip Make(int map)
        {
            // parametri per harta: 0 = pajiste vesela, 1 = desert exotic, 2 = munte inghetat
            // 3 = vulcan (intunecat), 9 = Trade Plaza (vesel)
            int bpm = map == 0 ? 118 : map == 1 ? 104 : map == 2 ? 92 : map == 3 ? 86 : 128;
            int root = map == 0 ? 60 : map == 1 ? 57 : map == 2 ? 62 : map == 3 ? 52 : 65; // nota MIDI
            int[] scale = map == 1 ? new[] { 0, 1, 4, 5, 7, 8, 10 }      // frigian dominant
                        : map == 2 ? new[] { 0, 2, 3, 5, 7, 9, 10 }      // doric (misterios)
                        : map == 3 ? new[] { 0, 2, 3, 5, 7, 8, 11 }      // minor armonic (dramatic)
                        : new[] { 0, 2, 4, 7, 9 };                       // pentatonic major
            int[] chords = map == 0 ? new[] { 0, 5, 3, 4 } : map == 1 ? new[] { 0, 1, 0, 6 } : map == 2 ? new[] { 0, 3, 4, 3 } : map == 3 ? new[] { 0, 5, 4, 0 } : new[] { 0, 2, 3, 1 };

            float beat = 60f / bpm;
            int bars = 8;
            int stepsPerBar = 8;               // optimi
            float step = beat / 2f;
            int total = bars * stepsPerBar;
            int samples = Mathf.CeilToInt(total * step * SR);
            var data = new float[samples];
            var rng = new System.Random(777 + map * 31);

            // melodie: mers aleator pe gama, repetata cu variatie (A A' B A)
            int[] motif = new int[16];
            int deg = 2;
            for (int i = 0; i < 16; i++)
            {
                deg += rng.Next(-2, 3);
                deg = Mathf.Clamp(deg, 0, scale.Length + 3);
                motif[i] = rng.NextDouble() < 0.2 ? -1 : deg;
            }

            for (int s = 0; s < total; s++)
            {
                int bar = s / stepsPerBar;
                int inBar = s % stepsPerBar;
                int chord = chords[(bar / 2) % chords.Length];
                double t0 = s * step;

                // bas pe timpii 1 si 3
                if (inBar % 4 == 0)
                    Tone(data, t0, step * 1.8f, Midi(root - 24 + Deg(scale, chord)), 0.16f, 2);

                // arpegiu discret
                if (map != 1 || inBar % 2 == 0)
                {
                    int ar = chord + (inBar % 3) * 2;
                    Tone(data, t0, step * 0.9f, Midi(root - 12 + Deg(scale, ar)), 0.045f, 1);
                }

                // melodie
                int mi = ((bar % 4 == 2) ? 8 : 0) + (inBar + (bar % 2) * 8) % 16;
                int md = motif[mi % 16];
                if (md >= 0 && (inBar % 2 == 0 || rng.NextDouble() < 0.5))
                    Tone(data, t0, step * (inBar % 4 == 0 ? 1.6f : 0.95f), Midi(root + Deg(scale, md + chord)), 0.085f, map == 2 ? 1 : 0);

                // tobe
                if (inBar % 4 == 0) Kick(data, t0, 0.22f);
                if (inBar % 4 == 2) Noise(data, t0, 0.08f, 0.09f, rng);
                if (map == 0 && inBar % 2 == 1) Noise(data, t0, 0.025f, 0.035f, rng);
            }

            // soft-clip + normalizare
            float peak = 0.0001f;
            for (int i = 0; i < samples; i++) { data[i] = (float)System.Math.Tanh(data[i] * 1.2f); peak = Mathf.Max(peak, Mathf.Abs(data[i])); }
            float norm = 0.8f / peak;
            for (int i = 0; i < samples; i++) data[i] *= norm;

            var clip = AudioClip.Create("Song_" + map, samples, 1, SR, false);
            clip.SetData(data, 0);
            return clip;
        }

        static int Deg(int[] scale, int d)
        {
            int oct = d >= 0 ? d / scale.Length : (d - scale.Length + 1) / scale.Length;
            int idx = d - oct * scale.Length;
            return scale[idx] + oct * 12;
        }

        static float Midi(int n) { return 440f * Mathf.Pow(2f, (n - 69) / 12f); }

        /// <param name="wave">0 = puls (patrat), 1 = triunghi, 2 = sinus</param>
        static void Tone(float[] d, double start, float dur, float freq, float amp, int wave)
        {
            int a = (int)(start * SR), n = (int)(dur * SR);
            double ph = 0, inc = freq / SR;
            for (int i = 0; i < n && a + i < d.Length; i++)
            {
                float env = Mathf.Min(1f, i / (SR * 0.006f)) * Mathf.Exp(-3.2f * i / (float)n);
                ph += inc; ph -= System.Math.Floor(ph);
                float v;
                if (wave == 0) v = ph < 0.5 ? 0.6f : -0.6f;
                else if (wave == 1) v = (float)(4.0 * System.Math.Abs(ph - 0.5) - 1.0);
                else v = Mathf.Sin((float)(ph * 2.0 * System.Math.PI));
                d[a + i] += v * amp * env;
            }
        }

        static void Kick(float[] d, double start, float amp)
        {
            int a = (int)(start * SR), n = (int)(0.16f * SR);
            double ph = 0;
            for (int i = 0; i < n && a + i < d.Length; i++)
            {
                float k = i / (float)n;
                float f = Mathf.Lerp(140f, 45f, k);
                ph += f / SR;
                d[a + i] += Mathf.Sin((float)(ph * 2 * System.Math.PI)) * amp * (1f - k) * (1f - k);
            }
        }

        static void Noise(float[] d, double start, float dur, float amp, System.Random rng)
        {
            int a = (int)(start * SR), n = (int)(dur * SR);
            for (int i = 0; i < n && a + i < d.Length; i++)
                d[a + i] += (float)(rng.NextDouble() * 2 - 1) * amp * (1f - i / (float)n);
        }
    }
}
