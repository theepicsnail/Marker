using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using VRC.SDKBase;
using System.IO;

[CustomEditor(typeof(SnailMarkerAnimationCreator))]
public class SnailMarkerAnimationCreatorEditor : Editor
{
    public static readonly string TEMPLATE_PATH = "Assets/VRCSDK/Examples/Sample Assets/Animation/AvatarControllerTemplate.controller";
    SnailMarkerAnimationCreator obj;
    private GUIStyle errorStyle = new GUIStyle();

    //components required for script
    private AnimatorOverrideController aController;
    private TrailRenderer trailRenderer;
    private VRCSDK2.VRC_AvatarDescriptor avatarDescriptor;

    //animation stuff
    private AnimationClip resetClip;
    private AnimationClip activateClip;

    //Configuration Paramters:
    public enum VRCGesture { Fist, HandOpen, FingerPoint, Victory, RockNRoll, HandGun, ThumbsUp }
    private VRCGesture activateGesture = VRCGesture.FingerPoint;
    private VRCGesture resetGesture = VRCGesture.HandOpen;

    //paths
    string animationPath;
    string exportPath;


    public void OnEnable()
    {
        errorStyle.normal.textColor = Color.red;
        obj = (SnailMarkerAnimationCreator)target;

        FindAvatarDescriptorAndSetPaths();
    }

    private void FindAvatarDescriptorAndSetPaths()
    {
        //descriptor and animation path:
        Transform cur = obj.transform;
        string path = "";
        do
        {
            if (cur.GetComponent<VRCSDK2.VRC_AvatarDescriptor>() != null)
            {
                avatarDescriptor = cur.GetComponent<VRCSDK2.VRC_AvatarDescriptor>();
                break;
            }
            if (path.Length > 0)
                path = cur.name + "/" + path;
            else
                path = cur.name;
            cur = cur.parent;
        } while (cur != null);

        if (avatarDescriptor != null)
        {
            animationPath = path;
            Debug.Log("Animation path:" + animationPath);
        }
    }

    private string generatedAssetPath(string name)
    {
        return Path.Combine("Assets\\Snail\\Marker2.0\\Generated", avatarDescriptor.name, name);
    }
    private string generatedFolderPath()
    {
        return Path.Combine(Application.dataPath, "Snail\\Marker2.0\\Generated\\", avatarDescriptor.name);
    }
    private string generatedFilePath(string name)
    {
        return Path.Combine(generatedFolderPath(), name);
    }
    private string templateAssetPath(string name)
    {
        return Path.Combine("Assets\\Snail\\Marker2.0\\Templates", name);
    }
    private void ensureGeneratedDirectory()
    {
        if (!Directory.Exists(generatedFolderPath()))
        {
            Directory.CreateDirectory(generatedFolderPath());
        }
    }
    private Object CreateAsset(Object asset, string name)
    {
        ensureGeneratedDirectory();
        string diskFile = generatedFilePath(name);
        if (File.Exists(diskFile))
        {
            if (!EditorUtility.DisplayDialog("Existing files", "Overwrite\n" + diskFile, "Yes", "No"))
                throw new IOException("Rejected overwriting " + diskFile);
            Debug.Log("Overwriting " + diskFile);
        }

        AssetDatabase.CreateAsset(asset, generatedAssetPath(name));
        return asset;
    }

    private T CreateAssetFromTemplate<T>(string name) where T : Object
    {
        ensureGeneratedDirectory();
        string assetPath = generatedAssetPath(name);
        string templatePath = templateAssetPath(name);
        if (!AssetDatabase.CopyAsset(templatePath, assetPath))
        {
            Debug.LogError("[Snail] Could not create asset: (" + assetPath + ") from: (" + templatePath + ")");
        }
        return AssetDatabase.LoadAssetAtPath<T>(assetPath);
    }

    public override void OnInspectorGUI()
    {
        if (avatarDescriptor == null)
        {
            GUILayout.Label("Could not find VRC Avatar Descriptor on avatar.", errorStyle);
            return;
        }

        activateGesture = (VRCGesture)EditorGUILayout.EnumPopup("Activate Gesture:", activateGesture);
        resetGesture = (VRCGesture)EditorGUILayout.EnumPopup("Reset Gesture:", resetGesture);

        if (GUILayout.Button("Do everything"))
        {
            DoEverything();
            return;
        }
        
    }

    public void DoEverything()
    {
        ensureGeneratedDirectory();
        SetAnimationController();
        SetAnimations();
        Cleanup();
    }

    private void SetAnimationController()
    {
        if (avatarDescriptor.CustomStandingAnims != null)
        {
            aController = avatarDescriptor.CustomStandingAnims;
        } else
        {
            aController = CreateAssetFromTemplate<AnimatorOverrideController>("overrides.overrideController");
            avatarDescriptor.CustomStandingAnims = aController;
        }
    }

    private AnimationClip GetOrSetAnimation( AnimationClip clip, string name )
    {
        if (clip==null)
        {
            clip = CreateAssetFromTemplate<AnimationClip>(name + ".anim");
        }
        return clip;
    }

    private void SetAnimations()
    {
        List<KeyValuePair<AnimationClip, AnimationClip>> overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        aController.GetOverrides(overrides);
        KeyValuePair<AnimationClip, AnimationClip> activatePair;
        int activatePairIndex = 0;
        KeyValuePair<AnimationClip, AnimationClip> resetPair;
        int resetPairIndex = 0;

        foreach ( KeyValuePair<AnimationClip,AnimationClip> anim in overrides)
        {
            if (activateGesture.ToString().ToUpper() == anim.Key.name)
            {
                activatePair = anim;
                activatePairIndex = overrides.IndexOf(anim);
            }
            if (resetGesture.ToString().ToUpper() == anim.Key.name)
            {
                resetPair = anim;
                resetPairIndex = overrides.IndexOf(anim);
            }
        }

        activateClip = GetOrSetAnimation(activatePair.Value, activateGesture.ToString());
        resetClip = GetOrSetAnimation(activatePair.Value, resetGesture.ToString());
        //activate Clip and 
        ModifyActivateClip();
        ModifyResetClip();

        activatePair = new KeyValuePair<AnimationClip, AnimationClip>(activatePair.Key, activateClip);
        resetPair = new KeyValuePair<AnimationClip, AnimationClip>(resetPair.Key, resetClip);

        overrides[activatePairIndex] = activatePair;
        overrides[resetPairIndex] = resetPair;
        
        aController.ApplyOverrides(overrides);
    }

    private void ModifyActivateClip()
    {
        float keyframe = 1.0f / activateClip.frameRate;
        AnimationCurve curve = AnimationCurve.Linear(0, 1, keyframe, 1);
        activateClip.SetCurve(animationPath, typeof(TrailRenderer), "m_Emitting", curve);
        EditorUtility.SetDirty(activateClip);
    }
    private void ModifyResetClip()
    {
        float keyframe = 1.0f / resetClip.frameRate;
        AnimationCurve curve = AnimationCurve.Linear(0, 0, keyframe, 0);
        resetClip.SetCurve(animationPath, typeof(TrailRenderer), "m_Time", curve);
        EditorUtility.SetDirty(resetClip);
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
