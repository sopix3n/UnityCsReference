// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor.UIElements.StyleSheets;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UI.Builder
{
    internal static class StyleSheetUtilities
    {
        public static readonly PropertyInfo[] ComputedStylesFieldInfos =
            typeof(ComputedStyle).GetProperties(BindingFlags.Instance | BindingFlags.Public);

        public static readonly PropertyInfo[] StylesFieldInfos =
            typeof(IStyle).GetProperties(BindingFlags.Instance | BindingFlags.Public);

        readonly static StyleSheetImporterImpl s_StyleSheetImporter = new StyleSheetImporterImpl();

        public static StyleSheet CreateInstance()
        {
            var newStyleSheet = ScriptableObject.CreateInstance<StyleSheet>();
            newStyleSheet.hideFlags = HideFlags.DontUnloadUnusedAsset | HideFlags.DontSaveInEditor;

            // Initialize all defaults.
            s_StyleSheetImporter.Import(newStyleSheet, "");

            return newStyleSheet;
        }

        public static StyleValueKeyword ConvertStyleKeyword(StyleKeyword keyword)
        {
            switch (keyword)
            {
                case StyleKeyword.Auto:
                    return StyleValueKeyword.Auto;
                case StyleKeyword.None:
                    return StyleValueKeyword.None;
                case StyleKeyword.Initial:
                    return StyleValueKeyword.Initial;
            }

            return StyleValueKeyword.Auto;
        }

        public static void AddFakeSelector(VisualElement selectorElement)
        {
            if (selectorElement == null)
                return;

            var styleSheet = selectorElement.GetClosestStyleSheet();

            if (styleSheet == null)
                return;

            StyleComplexSelector complexSelector = selectorElement.GetProperty(BuilderConstants.ElementLinkedStyleSelectorVEPropertyName) as StyleComplexSelector;
            var fakeSelectorString = BuilderConstants.UssSelectorNameSymbol + selectorElement.name;

            var fakeSelector = styleSheet.FindSelector(fakeSelectorString); // May already exist because of Undo/Redo

            if (fakeSelector == null)
                fakeSelector = styleSheet.AddSelector(fakeSelectorString);

            fakeSelector.rule = complexSelector.rule;
            fakeSelector.ruleIndex = complexSelector.ruleIndex; // shared index
            selectorElement.SetProperty(BuilderConstants.ElementLinkedFakeStyleSelectorVEPropertyName, fakeSelector);
            // To ensure that the fake selector is removed from the stylesheet if the builder gets closed with a selector still selected
            selectorElement.RegisterCallback<DetachFromPanelEvent>(OnSelectorElementDetachedFromPanel);
            selectorElement.IncrementVersion(VersionChangeType.Styles | VersionChangeType.StyleSheet);
        }

        public static void RemoveFakeSelector(VisualElement selectorElement)
        {
            if (selectorElement == null)
                return;

            var styleSheet = selectorElement.GetClosestStyleSheet();

            if (styleSheet == null)
                return;

            StyleComplexSelector fakeSelector = selectorElement.GetProperty(BuilderConstants.ElementLinkedFakeStyleSelectorVEPropertyName) as StyleComplexSelector;

            if (fakeSelector != null)
            {
                selectorElement.SetProperty(BuilderConstants.ElementLinkedFakeStyleSelectorVEPropertyName, null);
                styleSheet.RemoveSelector(fakeSelector);
                selectorElement.UnregisterCallback<DetachFromPanelEvent>(OnSelectorElementDetachedFromPanel);
            }
        }

        static void OnSelectorElementDetachedFromPanel(DetachFromPanelEvent e)
        {
            RemoveFakeSelector(e.elementTarget);
        }

        public static string GetCleanVariableName(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            var cleanName = Regex.Replace(value.Trim(), BuilderConstants.USSVariablePattern, BuilderConstants.USSVariableInvalidCharFiller);

            if (!cleanName.StartsWith(BuilderConstants.UssVariablePrefix))
                cleanName = BuilderConstants.UssVariablePrefix + cleanName;

            return cleanName;
        }
    }
}
