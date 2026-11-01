using System.Collections.Generic;
using Pretzel.Audio;
using Pretzel.Sfx;
using SpaceInvaders.Audio;
using UnityEditor;
using UnityEngine;

namespace SpaceInvaders.Editor
{
    /// <summary>
    /// Wraps the sound files made with SoundCraft (see Art~/build-audio.ps1) in Pretzel audio cues, fills the SfxLibrary the game
    /// plays by id, and puts the four march notes and the three tunes in the sound bank.
    /// </summary>
    internal static class SoundBankBuilder
    {
        public const string SfxFolder = "Assets/_Project/Audio/SFX";
        public const string MusicFolder = "Assets/_Project/Audio/Music";
        public const string SoundBankPath = "Assets/_Project/ScriptableObjects/SoundBank.asset";
        public const string SfxLibraryPath = "Assets/_Project/ScriptableObjects/SfxLibrary.asset";

        // Library id, sound file, the pitch range that keeps repeats lively, the cooldown and the most voices at once.
        private static readonly (string Id, string File, float PitchMin, float PitchMax, float Cooldown, int Voices)[] _sounds =
        {
            (GameSounds.PlayerShot, "laser", 0.94f, 1.06f, 0.05f, 4),
            (GameSounds.InvaderDestroyed, "hit_light", 0.9f, 1.1f, 0.04f, 5),
            (GameSounds.PlayerHit, "explosion", 1f, 1f, 0f, 1),
            (GameSounds.UfoDestroyed, "power_up", 1f, 1f, 0f, 1),
            (GameSounds.WaveCleared, "stinger_win", 1f, 1f, 0f, 1),
            (GameSounds.GameOver, "stinger_lose", 1f, 1f, 0f, 1)
        };

        public static void Build()
        {
            BuilderUtil.EnsureFolder(SfxFolder);
            BuilderUtil.EnsureFolder("Assets/_Project/ScriptableObjects");
            AssetDatabase.Refresh();

            List<SfxEntry> entries = new();
            foreach ((string id, string file, float pitchMin, float pitchMax, float cooldown, int voices) in _sounds)
            {
                AudioCue cue = BuildCue(id, file, pitchMin, pitchMax);
                entries.Add(new SfxEntry(id, cue, cooldown, voices));
            }

            SfxLibrary library = BuilderUtil.LoadOrCreate<SfxLibrary>(SfxLibraryPath);
            library.SetEntries(entries);
            EditorUtility.SetDirty(library);

            List<Object> march = new();
            for (int i = 1; i <= 4; i++)
            {
                march.Add(LoadClip("march_" + i));
            }

            SoundBank bank = BuilderUtil.LoadOrCreate<SoundBank>(SoundBankPath);
            BuilderUtil.SetObjects(bank, "_march", march);
            BuilderUtil.SetObject(bank, "_titleMusic", LoadMusic("title_theme"));
            BuilderUtil.SetObject(bank, "_gameplayMusic", LoadMusic("gameplay_loop"));
            BuilderUtil.SetObject(bank, "_lastStandMusic", LoadMusic("last_stand"));
        }

        // Music streams from disk as Vorbis, so the three WAV files do not sit in memory.
        private static AudioClip LoadMusic(string file)
        {
            string path = $"{MusicFolder}/{file}.wav";
            if (AssetImporter.GetAtPath(path) is AudioImporter importer)
            {
                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.Streaming;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = 0.6f;
                importer.defaultSampleSettings = settings;
                importer.loadInBackground = true;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<AudioClip>(path)
                   ?? throw new System.InvalidOperationException($"Missing music {path}. Run Art~/build-music.ps1.");
        }

        private static AudioCue BuildCue(string id, string file, float pitchMin, float pitchMax)
        {
            AudioCue cue = BuilderUtil.LoadOrCreate<AudioCue>($"{SfxFolder}/{id}Cue.asset");
            cue.SetClips(LoadClip(file));
            BuilderUtil.SetVector2(cue, "_pitch", new Vector2(pitchMin, pitchMax));
            EditorUtility.SetDirty(cue);
            return cue;
        }

        private static AudioClip LoadClip(string file)
        {
            string path = $"{SfxFolder}/{file}.wav";
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path)
                   ?? throw new System.InvalidOperationException($"Missing sound {path}. Run Art~/build-audio.ps1.");
        }
    }
}
