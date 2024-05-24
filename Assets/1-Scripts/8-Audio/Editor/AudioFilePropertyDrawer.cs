using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

// IngredientDrawerUIE
[CustomPropertyDrawer(typeof(AudioFile))]
public class AudioFilePropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Create property container element.
        var container = new VisualElement();
        container.Add(new PropertyField(property));

        EditorGUI.BeginProperty(position, label, property);

        int trueValue = property.enumValueIndex;
        int sortedIndex = AudioAtlas.AudioFile_SortedIndex[(AudioFile)trueValue];
        int selectedIndex = EditorGUI.Popup(position, label.text, sortedIndex, AudioAtlas.FormattedEnums);
        property.enumValueIndex = (int)AudioAtlas.AudioFile_SortedIndex.FirstOrDefault(entry => entry.Value == selectedIndex).Key;

        EditorGUI.EndProperty();
    }
}