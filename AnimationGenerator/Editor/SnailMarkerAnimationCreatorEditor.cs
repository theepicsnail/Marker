using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SnailMarkerAnimationCreator))]
public class SnailMarkerAnimationCreatorEditor : Editor
{
    public static readonly string TEMPLATE_PATH = "Assets/VRCSDK/Examples/Sample Assets/Animation/AvatarControllerTemplate.controller";
    SnailMarkerAnimationCreator obj;
    public void OnEnable()
    {
        obj = (SnailMarkerAnimationCreator)target;
    }
    public override void OnInspectorGUI()
    {
        if (!GUILayout.Button("Do everything"))
            return;
        DoEverything();
    }


    Transform avatar = null;
    string animationPath;
    string exportPath;

    private bool findAvatarAndAnimationPath(Transform cur)
    {
        // Find the avatar root and record the animation path along the way.
        string path = "";
        do
        {
            if (cur.GetComponent<VRCSDK2.VRC_AvatarDescriptor>() != null)
            {
                avatar = cur;
                break;
            }
            if(path.Length > 0 )
                path = cur.name + "/" + path;
            else
                path = cur.name;
            cur = cur.parent;
        } while (cur != null);

        if (avatar != null)
        {
            animationPath = path;
            Debug.Log("Animation path:" + animationPath);
            return true;
        }
        return false;
    }

    private bool getExportPath()
    {
        exportPath = EditorUtility.OpenFolderPanel("Save Generated Animations", "", "");
        // "c:/Users/snail/Downloads/VR Chat/Assets/Snail/Marker";

        if (exportPath.Length == 0)
            return false;
        int pathSplitPos = exportPath.IndexOf("/Assets");
        if (pathSplitPos == -1)
        {
            Debug.LogError("'/Assets' not found in path. Export path needs to be inside your project.");
            return false;
        }
        // Make exportPath have the form "Assets/..../"
        exportPath = exportPath.Substring(pathSplitPos + 1) + "/";
        return true;
    }


    public void DoEverything()
    {
        if (!findAvatarAndAnimationPath(obj.transform))
        {
            // We need the avatar descriptor for overrides.
            // Animations are also relative paths from the avatar descriptor.
            Debug.LogError("Could not find Avatar Descriptor component.");
        }

        if (!getExportPath())
        {
            // We have to write the animation files and overrides somewhere.
            Debug.LogError("Could not get a valid export path.");
            return;
        }

        DuplicateMaterial();
        WriteAnimations();
        SetupOverrides();
        Cleanup();
    }

    private void DuplicateMaterial()
    {
        // Duplicating the material so that the user can have different colors
        // across different instances of the marker.
        string materialPath = exportPath + "Ink.mat";
        TrailRenderer r = obj.gameObject.GetComponent<TrailRenderer>();
        WriteAsset(r.material, materialPath);
        r.material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
    }

    private void WriteAnimations()
    {
        float keyframe = 1F / 60;
        // Curve that sets a property to 0 over the course of 1 frame.
        AnimationCurve zeroCurve = AnimationCurve.Linear(0, 0, keyframe, 0);
        zeroCurve.AddKey(new Keyframe(keyframe, 0));
        AnimationClip erase = new AnimationClip();
        erase.SetCurve(animationPath, typeof(TrailRenderer), "m_Time", zeroCurve);
        WriteAsset(erase, exportPath + "EraseAll.anim");

        AnimationClip draw = new AnimationClip();
        draw.SetCurve(animationPath, typeof(Transform), "m_LocalPosition.x", zeroCurve);
        draw.SetCurve(animationPath, typeof(Transform), "m_LocalPosition.y", zeroCurve);
        draw.SetCurve(animationPath, typeof(Transform), "m_LocalPosition.z", zeroCurve);
        WriteAsset(draw, exportPath + "Drawing.anim");
    }

    private void SetupOverrides()
    {
        AnimatorOverrideController o = new AnimatorOverrideController();
        o.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(TEMPLATE_PATH);
        AssetDatabase.CreateAsset(o, exportPath + "Overrides.overrideController");

        VRCSDK2.VRC_AvatarDescriptor descriptor = avatar.gameObject.GetComponent<VRCSDK2.VRC_AvatarDescriptor>();
        descriptor.CustomSittingAnims = o;
        descriptor.CustomStandingAnims = o;

        Selection.activeObject = o;
        EditorGUIUtility.PingObject(o);
    }

    private void Cleanup()
    {
      
        // Remove this script from the avatar so that VRC is happy.
        DestroyImmediate(obj.gameObject.GetComponent<SnailMarkerAnimationCreator>());
    }

    private void WriteAsset(Object asset, string path)
    {
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(asset, path);
    }
}
