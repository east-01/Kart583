using System;
using System.Collections;
using System.Collections.Generic;
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
        
        // Handle new enum values being added
        if(targ.clips.Length != Enum.GetValues(typeof(AudioFile)).Length) {
            AudioClip[] newClips = new AudioClip[Enum.GetValues(typeof(AudioFile)).Length];
            for(int i = 0; i < targ.clips.Length; i++) {
                newClips[i] = targ.clips[i];
            }
            targ.clips = newClips;
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        foldOut = EditorGUILayout.Foldout(foldOut, "Audio Files");

        if (foldOut) {
            innerScrollPos = EditorGUILayout.BeginScrollView(innerScrollPos);
            EditorGUILayout.BeginVertical();
            
            foreach(AudioFile file in Enum.GetValues(typeof(AudioFile))) {
                if(file == AudioFile.NONE)
                    continue;
                targ.clips[(int)file] = (AudioClip) EditorGUILayout.ObjectField(file.ToString(), targ.clips[(int)file], typeof(AudioClip), false);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        GUILayout.Label("Audio mixer groups:");
        if (targ.prefixAudioMixerGroups == null) {
            targ.prefixAudioMixerGroups = new List<PrefixAudioMixerGroup>();
        }
        
        for (int i = 0; i < targ.prefixAudioMixerGroups.Count; i++) {
            EditorGUILayout.BeginHorizontal();
            PrefixAudioMixerGroup pamg = targ.prefixAudioMixerGroups[i];
            pamg.group = EditorGUILayout.TextField("Group", pamg.group);
            pamg.audioMixerGroup = (AudioMixerGroup) EditorGUILayout.ObjectField("Audio Mixer Group", pamg.audioMixerGroup, typeof(AudioMixerGroup), false);
            targ.prefixAudioMixerGroups[i] = pamg;
            EditorGUILayout.EndHorizontal();
        }

        if (GUILayout.Button("Add Audio Mixer Group")) {
            targ.prefixAudioMixerGroups.Add(new());
        }

        if (targ.prefixAudioMixerGroups.Count > 0 && GUILayout.Button("Remove Last Audio Mixer Group")) {
            targ.prefixAudioMixerGroups.RemoveAt(targ.prefixAudioMixerGroups.Count - 1);
        }
        EditorGUILayout.EndScrollView();

        if (GUI.changed)
            EditorUtility.SetDirty(targ);
    }
}
