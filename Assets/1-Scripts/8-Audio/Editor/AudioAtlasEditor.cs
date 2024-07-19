using System;
using System.Collections;
using System.Collections.Generic;
using FishNet.Component.Prediction;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

[CustomEditor(typeof(AudioAtlas))]
public class AudioAtlasEditor : Editor
{
    private Vector2 scrollPos;
    private bool foldOut;
    private Vector2 innerScrollPos;

    public override void OnInspectorGUI() 
    {
        AudioAtlas targ = (AudioAtlas) target;
        
        if(targ.clips == null) {
            targ.clips = new AudioClipPackage[Enum.GetValues(typeof(AudioFile)).Length];
        }

        // Handle new enum values being added
        if(targ.clips.Length != Enum.GetValues(typeof(AudioFile)).Length) {
            AudioClipPackage[] newClips = new AudioClipPackage[Enum.GetValues(typeof(AudioFile)).Length];
            for(int i = 0; i < targ.clips.Length; i++) {
                newClips[i] = targ.clips[i];
            }
            targ.clips = newClips;
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        foldOut = EditorGUILayout.Foldout(foldOut, "Audio Files (Click to expand)", true);

        if (foldOut) {
            innerScrollPos = EditorGUILayout.BeginScrollView(innerScrollPos);
            EditorGUILayout.BeginVertical();
            
            GUILayout.Label("SP - Spatial blend, 0-1 where higher values use spatial sound");

            foreach(AudioFile file in Enum.GetValues(typeof(AudioFile))) {
                if(file == AudioFile.NONE)
                    continue;
                AudioClipPackage acp = targ.clips[(int)file];

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(file.ToString(), new GUILayoutOption[] {GUILayout.Width(200)});
                acp.audioClip = (AudioClip) EditorGUILayout.ObjectField(acp.audioClip, typeof(AudioClip), false, new GUILayoutOption[] {GUILayout.Width(150)});
                GUILayout.Label("SP:", new GUILayoutOption[] {GUILayout.Width(25)});
                acp.spatialBlend = EditorGUILayout.FloatField(acp.spatialBlend, new GUILayoutOption[] {GUILayout.Width(30)});
                EditorGUILayout.EndHorizontal();

                targ.clips[(int)file] = acp;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        if (targ.prefixAudioMixerGroups == null) {
            targ.prefixAudioMixerGroups = new List<PrefixAudioMixerGroup>();
        }

        GUILayout.Label($"Audio mixer groups ({targ.prefixAudioMixerGroups.Count}):");

        // Audio mixer group rows
        for (int i = 0; i < targ.prefixAudioMixerGroups.Count; i++) {
            EditorGUILayout.BeginHorizontal();

            PrefixAudioMixerGroup pamg = targ.prefixAudioMixerGroups[i];
            GUILayout.Label("Group:", new GUILayoutOption[] {GUILayout.Width(50)});
            pamg.group = EditorGUILayout.TextField(pamg.group, new GUILayoutOption[] {GUILayout.Width(100)});
            pamg.audioMixerGroup = (AudioMixerGroup) EditorGUILayout.ObjectField("Audio Mixer Group:", pamg.audioMixerGroup, typeof(AudioMixerGroup), false);
            
            targ.prefixAudioMixerGroups[i] = pamg;
            EditorGUILayout.EndHorizontal();
        }

        // Add/remove buttons
        if (GUILayout.Button("Add Audio Mixer Group"))
            targ.prefixAudioMixerGroups.Add(new());

        if (targ.prefixAudioMixerGroups.Count > 0 && GUILayout.Button("Remove Last Audio Mixer Group"))
            targ.prefixAudioMixerGroups.RemoveAt(targ.prefixAudioMixerGroups.Count - 1);

        EditorGUILayout.EndScrollView();


        if (GUI.changed)
            EditorUtility.SetDirty(targ);
    }
}
