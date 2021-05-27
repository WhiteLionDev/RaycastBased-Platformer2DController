using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEditorInternal;
using System.Reflection;

public class ConstantsBuilder : EditorWindow
{
    private const string FOLDER_LOCATION = "APPLICATION/Code/Constants/";
    private const string TAGS_FILE_NAME = "Tags";
    private const string LAYERS_FILE_NAME = "Layers";
    private const string SORTING_LAYERS_FILE_NAME = "SortingLayers";
    private const string SCENES_FILE_NAME = "Scenes";
    private const string SCRIPT_EXTENSION = ".cs";

    [MenuItem("Edit/Rebuild Constants")]
    static void RebuildTagsAndLayersClasses()
    {
        string folderPath = Application.dataPath + "/" + FOLDER_LOCATION;
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }
        File.WriteAllText(folderPath + TAGS_FILE_NAME + SCRIPT_EXTENSION, GetClassContent(TAGS_FILE_NAME, InternalEditorUtility.tags));
        File.WriteAllText(folderPath + LAYERS_FILE_NAME + SCRIPT_EXTENSION, GetLayerClassContent(LAYERS_FILE_NAME, InternalEditorUtility.layers));

        var internalEditorUtilityType = typeof(InternalEditorUtility);
        var sortingLayersProperty = internalEditorUtilityType.GetProperty("sortingLayerNames", BindingFlags.Static | BindingFlags.NonPublic);
        File.WriteAllText(folderPath + SORTING_LAYERS_FILE_NAME + SCRIPT_EXTENSION, GetSortingLayerClassContent(SORTING_LAYERS_FILE_NAME, (string[])sortingLayersProperty.GetValue(null, new object[0])));

        File.WriteAllText(folderPath + SCENES_FILE_NAME + SCRIPT_EXTENSION, GetClassContent(SCENES_FILE_NAME, EditorBuildSettingsScenesToNameStrings(EditorBuildSettings.scenes)));
        AssetDatabase.ImportAsset("Assets/" + FOLDER_LOCATION + TAGS_FILE_NAME + SCRIPT_EXTENSION, ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset("Assets/" + FOLDER_LOCATION + LAYERS_FILE_NAME + SCRIPT_EXTENSION, ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset("Assets/" + FOLDER_LOCATION + SORTING_LAYERS_FILE_NAME + SCRIPT_EXTENSION, ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset("Assets/" + FOLDER_LOCATION + SCENES_FILE_NAME + SCRIPT_EXTENSION, ImportAssetOptions.ForceUpdate);
        Debug.Log("Rebuild Complete");
    }

    private static string[] EditorBuildSettingsScenesToNameStrings(EditorBuildSettingsScene[] scenes)
    {
        string[] sceneNames = new string[scenes.Length];
        for (int n = 0; n < sceneNames.Length; n++)
        {
            sceneNames[n] = System.IO.Path.GetFileNameWithoutExtension(scenes[n].path);
        }
        return sceneNames;
    }

    private static string GetClassContent(string className, string[] labelsArray)
    {
        string output = "";
        output += "public class " + className + "\n";
        output += "{\n";
        foreach (string label in labelsArray)
        {
            output += "\t" + BuildConstVariable(label) + "\n";
        }
        output += "}";
        return output;
    }

    private static string GetLayerClassContent(string className, string[] labelsArray)
    {
        string output = "";
        output += "public class " + className + "\n";
        output += "{\n";
        foreach (string label in labelsArray)
        {
            output += "\t" + BuildConstVariable(label) + "\n";
        }
        output += "\n";

        foreach (string label in labelsArray)
        {
            output += "\t" + "public const int " + ToUpperCaseWithUnderscores(label) + "_INT" + " = " + LayerMask.NameToLayer(label) + ";\n";
        }

        output += "}";
        return output;
    }

    private static string GetSortingLayerClassContent(string className, string[] labelsArray)
    {
        string output = "";
        output += "public class " + className + "\n";
        output += "{\n";
        foreach (string label in labelsArray)
        {
            output += "\t" + BuildConstVariable(label) + "\n";
        }
        output += "\n";

        foreach (string label in labelsArray)
        {
            output += "\t" + "public const int " + ToUpperCaseWithUnderscores(label) + "_INT" + " = " + SortingLayer.GetLayerValueFromName(label) + ";\n";
        }

        output += "}";
        return output;
    }

    private static string BuildConstVariable(string varName)
    {
        return "public const string " + ToUpperCaseWithUnderscores(varName) + " = " + '"' + varName + '"' + ";";
    }

    private static string ToUpperCaseWithUnderscores(string input)
    {
        string output = "" + input[0];

        for (int n = 1; n < input.Length; n++)
        {
            if ((char.IsUpper(input[n]) || input[n] == ' ') && !char.IsUpper(input[n - 1]) && input[n - 1] != '_' && input[n - 1] != ' ')
            {
                output += "_";
            }

            if (input[n] != ' ' && input[n] != '_')
            {
                output += input[n];
            }
        }

        output = output.ToUpper();
        return output;
    }
}