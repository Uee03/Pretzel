using System.Collections.Generic;
using System.IO;
using Pretzel.Audio;
using SpaceInvaders.Audio;
using UnityEditor;
using UnityEngine;

namespace SpaceInvaders.Editor
{
    /// <summary>Renders the placeholder sound effects to WAV files, wraps them in Pretzel audio cues and fills the sound bank.</summary>
    internal static class SoundBankBuilder
    {
        public const string SfxFolder = "Assets/_Project/Audio/SFX";
        public const string SoundBankPath = "Assets/_Project/ScriptableObjects/SoundBank.asset";

        private static readonly float[] _marchNotes = { 110f, 98f, 87.3f, 82.4f };

        public static SoundBank Build()
        {
            BuilderUtil.EnsureFolder(SfxFolder);
            BuilderUtil.EnsureFolder("Assets/_Project/ScriptableObjects");

            AudioCue shot = BuildCue("PlayerShot", SfxSynth.Shot(), 0.9f, 1.08f);
            AudioCue invader = BuildCue("InvaderDestroyed", SfxSynth.InvaderDestroyed(), 0.92f, 1.1f);
            AudioCue hit = BuildCue("PlayerHit", SfxSynth.PlayerHit(), 1f, 1f);
            AudioCue ufo = BuildCue("UfoDestroyed", SfxSynth.UfoDestroyed(), 1f, 1f);
            AudioCue wave = BuildCue("WaveCleared", SfxSynth.WaveCleared(), 1f, 1f);
            AudioCue over = BuildCue("GameOver", SfxSynth.GameOver(), 1f, 1f);

            List<Object> march = new();
            for (int i = 0; i < _marchNotes.Length; i++)
            {
                march.Add(WriteClip("March" + (i + 1), SfxSynth.MarchNote(_marchNotes[i])));
            }

            SoundBank bank = BuilderUtil.LoadOrCreate<SoundBank>(SoundBankPath);
            BuilderUtil.Edit(bank, serialized =>
            {
                BuilderUtil.Property(serialized, "_playerShot").objectReferenceValue = shot;
                BuilderUtil.Property(serialized, "_invaderDestroyed").objectReferenceValue = invader;
                BuilderUtil.Property(serialized, "_playerHit").objectReferenceValue = hit;
                BuilderUtil.Property(serialized, "_ufoDestroyed").objectReferenceValue = ufo;
                BuilderUtil.Property(serialized, "_waveCleared").objectReferenceValue = wave;
                BuilderUtil.Property(serialized, "_gameOver").objectReferenceValue = over;
            });
            BuilderUtil.SetObjects(bank, "_march", march);
            return bank;
        }

        private static AudioCue BuildCue(string name, float[] samples, float minPitch, float maxPitch)
        {
            AudioClip clip = WriteClip(name, samples);
            AudioCue cue = BuilderUtil.LoadOrCreate<AudioCue>($"{SfxFolder}/{name}Cue.asset");
            cue.SetClips(clip);
            BuilderUtil.SetVector2(cue, "_pitch", new Vector2(minPitch, maxPitch));
            return cue;
        }

        private static AudioClip WriteClip(string name, float[] samples)
        {
            string path = $"{SfxFolder}/{name}.wav";
            File.WriteAllBytes(path, SfxSynth.ToWav(samples));
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
    }
}
