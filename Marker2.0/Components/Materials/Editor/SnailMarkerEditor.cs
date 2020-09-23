using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class SnailMarkerEditor : ShaderGUI
{
    MaterialProperty mainTexture;
    MaterialProperty color;
    MaterialProperty invisible;
    MaterialProperty mode;
    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {

        { //Find Properties
            mainTexture = FindProperty("_MainTex", properties);
            color = FindProperty("_Color", properties);
            mode = FindProperty("_Mode", properties);
            invisible = FindProperty("_Invisible", properties);
        }
        materialEditor.ShaderProperty(invisible, "Invisible distance", 0);
        materialEditor.ShaderProperty(mode, "Coloring mode");
        Material material = materialEditor.target as Material;
        EditorGUI.BeginChangeCheck();
        switch((int) mode.floatValue)
        {
            case 0: // Solid Color
                materialEditor.ColorProperty(color, "Solid color");
                break;
            case 1: // Vertex Color
                GUILayout.Label("Use the 'Color' property on the trail render.");
                break;
            case 2: // Texture
                materialEditor.TextureProperty(mainTexture, "Main Texture");
                break;
        }
    }
}