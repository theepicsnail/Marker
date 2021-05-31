using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using static VRC.SDK3.Avatars.Components.VRCAvatarDescriptor;
using VRC.SDK3.Avatars.Components;
using System.IO;
using VRC.SDK3.Avatars.ScriptableObjects;
using static VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control;

[CustomEditor(typeof(SnailMarker3AnimationCreator))]
public class SnailMarker3AnimationCreatorEditor : Editor
{
    private SnailMarker3AnimationCreator obj;
    private GUIStyle errorStyle = new GUIStyle();
    private CustomAnimLayer fxLayer;
    private AnimatorController fxController;
    private VRCExpressionsMenu expressionMenu;
    private VRCExpressionParameters expressionParams;

    //animation stuff
    private AnimationClip idleClip;
    private AnimationClip eraseOnClip;
    private AnimationClip drawOnClip;
    private AnimationClip toggleOnClip;
    private AnimationClip toggleOffClip;

    //paths:
    private string scriptPath = "Assets\\Snail\\Marker3.0\\AnimationGenerator\\Editor";
    private string rootPath = "Assets\\Snail\\Marker3.0\\";
    private string generatedPath = "Assets\\Snail\\Marker3.0\\Generated\\";
    private string templatesPath = "Assets\\Snail\\Marker3.0\\Templates\\";

    //Configuration Paramters:
    public enum Hand { Left, Right }
    public enum VRCGesture { Neutral, Fist, HandOpen, FingerPoint, Victory, RockNRoll, HandGun, ThumbsUp }
    private Hand hand = Hand.Right;
    private VRCGesture activateGesture = VRCGesture.FingerPoint;
    private VRCGesture resetGesture = VRCGesture.HandOpen;

    public void OnEnable()
    {
        errorStyle.normal.textColor = Color.red;
        obj = target as SnailMarker3AnimationCreator;

        if (!findAvatarAndAnimationPath(obj.transform)) return;
        findComponents();
    }
    public override void OnInspectorGUI()
    {
        if (avatarDescriptor == null)
        {
            GUILayout.Label("Could not find VRC Avatar Descriptor on avatar.", errorStyle);
            return;
        }

        if (fxController == null || expressionParams == null || expressionMenu == null)
        {
            GUILayout.Label("Avatar is missing some 3.0 stuff.");
            if (GUILayout.Button("Setup 3.0 defaults"))
            {
                updatePaths();
                ensureDefaults();
            }
        }
        else
        {
            GUILayout.Label("Select a location for the marker:");
            hand = (Hand)EditorGUILayout.EnumPopup("Hand:", hand);
            activateGesture = (VRCGesture)EditorGUILayout.EnumPopup("Activate Gesture:", activateGesture);
            resetGesture = (VRCGesture)EditorGUILayout.EnumPopup("Reset Gesture:", resetGesture);

            GUILayout.Label("Select a location for the marker:");
            ShowMenuFoldout(avatarDescriptor.expressionsMenu, "Expressions Menu");
        }
    }

    private HashSet<VRCExpressionsMenu> expandedMenus = new HashSet<VRCExpressionsMenu>();
    private void ShowMenuFoldout(VRCExpressionsMenu menu, string title)
    {
        if (menu == null) return;

        bool b = expandedMenus.Contains(menu);
        // Remove before we go any further, prevents infinite recursion on cyclic menus.
        expandedMenus.Remove(menu);


        if (!EditorGUILayout.Foldout(b, title, true))
        {
            return;
        }

        EditorGUILayout.BeginVertical();
        GUILayout.BeginHorizontal();
        GUILayout.Space(EditorGUI.indentLevel * 17);

        int size = 0;
        for (int i = 0; i < expressionParams.parameters.Length; i++)
        {
            size += VRCExpressionParameters.TypeCost(expressionParams.parameters[i].valueType);
        }

        if ( menu.controls.Count < expressionParams.parameters.Length )
        {
            if (GUILayout.Button("Add Marker Here", GUILayout.Width(130)))
            {
                updatePaths();
                InstallMarker(ref menu);
            }
        }
        else
        {
            if (size < VRCExpressionParameters.MAX_PARAMETER_COST)
            {
                if (GUILayout.Button("enlarge parameters", GUILayout.Width(130)))
                {
                    updatePaths();
                    if (size < VRCExpressionParameters.MAX_PARAMETER_COST)
                    {
                        VRCExpressionParameters.Parameter[] newParams = new VRCExpressionParameters.Parameter[expressionParams.parameters.Length + 1];
                        for (int i = 0; i < expressionParams.parameters.Length; i++)
                        {
                            newParams[i] = expressionParams.parameters[i];
                        }
                        newParams[expressionParams.parameters.Length] = new VRCExpressionParameters.Parameter();
                        expressionParams.parameters = newParams;
                        EditorUtility.SetDirty(expressionParams);
                    }
                }
            }
            else
            {
                GUILayout.Label("No room.", GUILayout.Width(130));
            }
        }
        GUILayout.EndHorizontal();

        EditorGUI.indentLevel++;
        foreach (var child in menu.controls)
        {
            if (child.type == ControlType.SubMenu)
            {
                ShowMenuFoldout(child.subMenu, child.name);
            }
        }
        EditorGUI.indentLevel--;
        EditorGUILayout.EndVertical();

        // Okay, it's safe to add the menu back.
        expandedMenus.Add(menu);
    }

    //menu in this case is the chosen menu or submenu, in wich to add the marker.
    private void InstallMarker(ref VRCExpressionsMenu menu)
    {
        ConfigureAnimationController();
        AddParameter();
        AddMarkerToMenu(ref menu);
        //Finally destroy and cleanup.
        Cleanup();
    }

    private void AddParameter()
    {
        for (int i = 0; i < expressionParams.parameters.Length; i++)
        {
            if (expressionParams.parameters[i].name == "ToggleMarker" ||
                expressionParams.parameters[i].name.Trim().Length == 0)
            {
                expressionParams.parameters[i].name = "ToggleMarker";
                expressionParams.parameters[i].valueType = VRCExpressionParameters.ValueType.Bool;
                EditorUtility.SetDirty(expressionParams);
                return;
            }
        }
        Debug.LogError("Could not create Avatar 3.0 parameter. You may be out of paramters.");
        return;
    }
    private void AddMarkerToMenu(ref VRCExpressionsMenu menu)
    {
        var control = new VRCExpressionsMenu.Control();
        control.type = ControlType.Toggle;
        control.name = "Toggle Marker";
        control.parameter = new VRCExpressionsMenu.Control.Parameter();
        control.parameter.name = "ToggleMarker";
        control.value = 1;
        // control.icon // TODO
        menu.controls.Add(control);
        //avatarDescriptor.expressionsMenu
        EditorUtility.SetDirty(avatarDescriptor.expressionsMenu);

    }

    Transform avatarTransform = null;
    VRCAvatarDescriptor avatarDescriptor = null;
    string animationPath;

    private bool findAvatarAndAnimationPath(Transform cur)
    {
        // Find the avatar root and record the animation path along the way.
        avatarDescriptor = null;
        avatarTransform = null;
        string path = "";
        do
        {
            avatarDescriptor = cur.GetComponent<VRCAvatarDescriptor>();
            if (avatarDescriptor != null)
            {
                avatarTransform = cur;
                break;
            }
            if (path.Length > 0)
                path = cur.name + "/" + path;
            else
                path = cur.name;
            cur = cur.parent;
        } while (cur != null);

        if (avatarTransform != null)
        {
            animationPath = path;
            return true;
        }

        return false;
    }

    private void findComponents()
    {
        var obj = new SerializedObject(avatarDescriptor);
        var baseAnimationLayers = obj.FindProperty("baseAnimationLayers.Array");
        var size = baseAnimationLayers.arraySize;
        for (int i = 0; i < size; i++)
        {
            var layer = baseAnimationLayers.GetArrayElementAtIndex(i);
            if ((AnimLayerType)(layer.FindPropertyRelative("type").enumValueIndex) != AnimLayerType.FX)
                continue;

            var controllerProp = layer.FindPropertyRelative("animatorController");
            fxController = controllerProp.objectReferenceValue as AnimatorController;
        }

        expressionMenu = avatarDescriptor.expressionsMenu;
        expressionParams = avatarDescriptor.expressionParameters;
    }

    private void ensureDefaults()
    {
        var obj = new SerializedObject(avatarDescriptor);

        obj.FindProperty("customizeAnimationLayers").boolValue = true;
        obj.FindProperty("customExpressions").boolValue = true;

        // Find and set the FX property if necessary.
        var baseAnimationLayers = obj.FindProperty("baseAnimationLayers.Array");
        var size = baseAnimationLayers.arraySize;
        for (int i = 0; i < size; i++)
        {
            var layer = baseAnimationLayers.GetArrayElementAtIndex(i);
            if ((AnimLayerType)(layer.FindPropertyRelative("type").enumValueIndex) != AnimLayerType.FX)
                continue;

            var controllerProp = layer.FindPropertyRelative("animatorController");
            if (controllerProp.objectReferenceValue == null)
            {
                controllerProp.objectReferenceValue = CreateAssetFromTemplate<AnimatorController>("DefaultAnimatorController.controller");
            }

            layer.FindPropertyRelative("isDefault").boolValue = false;
            break;
        }

        var menu = obj.FindProperty("expressionsMenu");
        if (menu.objectReferenceValue == null)
        {
            menu.objectReferenceValue = CreateAssetFromTemplate<VRCExpressionsMenu>("DefaultMenu.asset");
        }

        var parameters = obj.FindProperty("expressionParameters");
        if (parameters.objectReferenceValue == null)
        {
            parameters.objectReferenceValue = CreateAssetFromTemplate<VRCExpressionParameters>("DefaultParams.asset");
        }

        obj.ApplyModifiedProperties();

        // revalidate
        OnEnable();
    }

    /*******************************
     * Path and Asset Generation.
    *******************************/
    private void updatePaths()
    {
        MonoScript ms = MonoScript.FromScriptableObject(this);
        scriptPath = AssetDatabase.GetAssetPath(ms).Replace("SnailMarker3AnimationCreatorEditor.cs", "");
        rootPath = scriptPath.Replace("AnimationGenerator/Editor/", "");
        generatedPath = rootPath + "Generated/" + avatarTransform.name;
        templatesPath = rootPath + "Templates";

        scriptPath = scriptPath.Replace("/", "\\");
        rootPath = rootPath.Replace("/", "\\");
        generatedPath = generatedPath.Replace("/", "\\");
        templatesPath = templatesPath.Replace("/", "\\");
    }
    private string generatedAssetPath(string name)
    {
        return Path.Combine( generatedPath, name);
    }
    private string templateAssetPath(string name)
    {
        return Path.Combine( templatesPath, name);
    }
    private string generatedGestureName()
    {
        return (hand == Hand.Right) ? "GestureRight" : "GestureLeft";
    }

    private Object CreateAsset(Object asset, string name)
    {
        ensureGeneratedDirectory();
        string diskFile = generatedAssetPath(name);
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

    private void ensureGeneratedDirectory()
    {
        if (!Directory.Exists(generatedPath))
        {
            Directory.CreateDirectory(generatedPath);
        }
    }

    private void Cleanup()
    {
        // Remove this script from the avatar so that VRC is happy.
        DestroyImmediate(obj.gameObject.GetComponent<SnailMarker3AnimationCreator>());
    }

    /*******************************
    * Animations
    *******************************/
    private void ConfigureAnimationController()
    {
        //Check and build the animation controller:
        CreateParameters();
        CreateGestureLayer();
        CreateToggleLayer();
    }

    private void CreateParameters()
    {
        //find or create gestureRight
        bool gestureParamFound = false;
        for (int i = 0; i < fxController.parameters.Length; i++)
        {
            if (fxController.parameters[i].name == generatedGestureName())
            {
                gestureParamFound = true;
            }
        }
        if (!gestureParamFound)
        {
            fxController.AddParameter(generatedGestureName(), AnimatorControllerParameterType.Int);
        }

        //find or create markerToggle
        bool toggleMarkerParamFound = false;
        for (int i = 0; i < fxController.parameters.Length; i++)
        {
            if (fxController.parameters[i].name == "ToggleMarker")
            {
                toggleMarkerParamFound = true;
            }
        }
        if (!toggleMarkerParamFound)
        {
            fxController.AddParameter("ToggleMarker", AnimatorControllerParameterType.Bool);
        }
    }
    private AnimatorControllerLayer FindLayer(string name)
    {
        AnimatorControllerLayer layer = null;
        for (int i = 0; i < fxController.layers.Length; i++)
        {
            if (fxController.layers[i].name == name)
            {
                return fxController.layers[i];
            }
        }
        return layer;
    }

    private void SetGestureTransitionDefaults(AnimatorStateTransition transition)
    {
        transition.hasExitTime = false;
        transition.hasFixedDuration = false;
        transition.duration = .01f;
        transition.interruptionSource = TransitionInterruptionSource.Destination;
        transition.canTransitionToSelf = false;
    }
    private void CreateGestureLayer()
    {
        string layerName = generatedGestureName() + "Maker";
        AnimatorControllerLayer layer = FindLayer(layerName);
        if (layer == null)
        {
            layer = new AnimatorControllerLayer();
            layer.name = layerName;
            layer.defaultWeight = 1.0f;
            layer.stateMachine = new AnimatorStateMachine();
            layer.stateMachine.name = layerName;
            layer.stateMachine.hideFlags = HideFlags.HideInHierarchy;
            if (AssetDatabase.GetAssetPath(fxController) != "")
                AssetDatabase.AddObjectToAsset(layer.stateMachine, AssetDatabase.GetAssetPath(fxController));

            AnimatorState idleState = layer.stateMachine.AddState("Idle");
            AnimatorState activateMarkerState = layer.stateMachine.AddState("Activate Marker");
            AnimatorState eraseAllState = layer.stateMachine.AddState("Erase All");

            //set the idle transition
            for (int i = 0; i < 8; i++)
            {
                if (i == (int)activateGesture)
                {
                    AnimatorStateTransition transition;
                    transition = layer.stateMachine.AddAnyStateTransition(activateMarkerState);
                    SetGestureTransitionDefaults(transition);
                    transition.AddCondition(AnimatorConditionMode.Equals, i, generatedGestureName());
                }
                else if (i == (int)resetGesture)
                {
                    AnimatorStateTransition transition;
                    transition = layer.stateMachine.AddAnyStateTransition(eraseAllState);
                    SetGestureTransitionDefaults(transition);
                    transition.AddCondition(AnimatorConditionMode.Equals, i, generatedGestureName());
                }
                else
                {
                    AnimatorStateTransition transition;
                    transition = layer.stateMachine.AddAnyStateTransition(idleState);
                    SetGestureTransitionDefaults(transition);
                    transition.AddCondition(AnimatorConditionMode.Equals, i, generatedGestureName());
                }
            }

            WriteGestureAnimations();
            idleState.motion = idleClip;
            activateMarkerState.motion = drawOnClip;
            eraseAllState.motion = eraseOnClip;

            fxController.AddLayer(layer);
        }
    }

    private void CreateToggleLayer()
    {
        string layerName = "ToggleMarker";
        AnimatorControllerLayer layer = FindLayer(layerName);
        if (layer == null)
        {
            layer = new AnimatorControllerLayer();
            layer.name = layerName;
            layer.stateMachine = new AnimatorStateMachine();
            layer.stateMachine.name = layerName;
            layer.stateMachine.hideFlags = HideFlags.HideInHierarchy;
            if (AssetDatabase.GetAssetPath(fxController) != "")
                AssetDatabase.AddObjectToAsset(layer.stateMachine, AssetDatabase.GetAssetPath(fxController));

            layer.blendingMode = AnimatorLayerBlendingMode.Override;
            layer.defaultWeight = 1.0f;

            AnimatorState MarkerOffState = layer.stateMachine.AddState("MarkerOff");
            AnimatorState MarkerOnState = layer.stateMachine.AddState("MarkerOn");

            AnimatorStateTransition transition = MarkerOffState.AddTransition(MarkerOnState);
            transition.AddCondition(AnimatorConditionMode.If, 0, "ToggleMarker");
            transition.hasExitTime = false;
            transition.hasFixedDuration = false;
            transition.duration = 0;
            transition.interruptionSource = TransitionInterruptionSource.Destination;
            transition.canTransitionToSelf = false;

            transition = MarkerOnState.AddTransition(MarkerOffState);
            transition.AddCondition(AnimatorConditionMode.IfNot, 0, "ToggleMarker");
            transition.hasExitTime = false;
            transition.hasFixedDuration = false;
            transition.duration = 0;
            transition.interruptionSource = TransitionInterruptionSource.Destination;
            transition.canTransitionToSelf = false;

            WriteToggleAnimations();

            MarkerOffState.motion = toggleOffClip;
            MarkerOnState.motion = toggleOnClip;
            fxController.AddLayer(layer);
        }
    }
    private void WriteToggleAnimations()
    {
        float keyframe = 1F / 60;

        // Curve that sets a property to 1 over the course of 1 frame.
        AnimationCurve curveOff = AnimationCurve.Linear(0, 0, keyframe, 0);
        toggleOffClip = new AnimationClip();
        toggleOffClip.SetCurve(animationPath, typeof(TrailRenderer), "m_Enabled", curveOff);
        CreateAsset(toggleOffClip, "MarkerOff.anim");

        // Curve that sets a property to 0 over the course of 1 frame.
        AnimationCurve curveOn = AnimationCurve.Linear(0, 1, keyframe, 1);
        toggleOnClip = new AnimationClip();
        toggleOnClip.SetCurve(animationPath, typeof(TrailRenderer), "m_Enabled", curveOn);
        CreateAsset(toggleOnClip, "MarkerOn.anim");

    }
    private void WriteGestureAnimations()
    {
        float keyframe = 1F / 60;

        // Curve that sets a property to 0 over the course of 1 frame.
        AnimationCurve eraseOnCurve = AnimationCurve.Linear(0, 0, keyframe, 0);
        AnimationClip eraseOn = new AnimationClip();
        eraseOn.SetCurve(animationPath, typeof(TrailRenderer), "m_Time", eraseOnCurve);
        CreateAsset(eraseOn, "EraseAll.anim");

        // Curve that sets a property to 1 over the course of 1 frame.
        AnimationCurve drawOnCurve = AnimationCurve.Linear(0, 1, keyframe, 1);
        AnimationClip drawOn = new AnimationClip();
        drawOn.SetCurve(animationPath, typeof(TrailRenderer), "m_Emitting", drawOnCurve);
        CreateAsset(drawOn, "DrawingOn.anim");

        AnimationCurve drawOffCurve = AnimationCurve.Linear(0, 0, keyframe, 0);
        AnimationCurve eraseOffCurve = AnimationCurve.Linear(0, 120, keyframe, 120);
        AnimationClip idle = new AnimationClip();
        idle.SetCurve(animationPath, typeof(TrailRenderer), "m_Emitting", drawOffCurve);
        idle.SetCurve(animationPath, typeof(TrailRenderer), "m_Time", eraseOffCurve);
        CreateAsset(idle, "Idle.anim");

        eraseOnClip = eraseOn;
        drawOnClip = drawOn;
        idleClip = idle;
    }
    /************************
        End Animations
    *************************/
}
