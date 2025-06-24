//----------------------------------------------
//            Realistic Car Controller
//
// Copyright © 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using System;
using System.Collections.Generic;
using System.Linq;

public class RCC_SetScriptingSymbol {

    public static void SetEnabled(string defineName, bool enable) {

        foreach (BuildTarget buildTarget in Enum.GetValues(typeof(BuildTarget))) {

            BuildTargetGroup group = BuildPipeline.GetBuildTargetGroup(buildTarget);
            if (group == BuildTargetGroup.Unknown)
                continue;

            // Gunakan NamedBuildTarget
            NamedBuildTarget namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(group);

            // Ambil define symbols
            var curDefineSymbols = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget)
                                                 .Split(';')
                                                 .Select(d => d.Trim())
                                                 .ToList();

            if (enable) {
                if (!curDefineSymbols.Contains(defineName))
                    curDefineSymbols.Add(defineName);
            } else {
                if (curDefineSymbols.Contains(defineName))
                    curDefineSymbols.Remove(defineName);
            }

            try {
                PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, string.Join(";", curDefineSymbols));
            } catch (Exception e) {
                Debug.Log($"Could not set {defineName} scripting define symbol for build target: {buildTarget}, group: {group}, error: {e}");
            }
        }

    }

}
