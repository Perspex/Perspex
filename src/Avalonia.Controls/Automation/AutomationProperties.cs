using System;
using Avalonia.Controls;

namespace Avalonia.Automation
{
    public static class AutomationProperties
    {
        internal const int AutomationPositionInSetDefault = -1;
        internal const int AutomationSizeOfSetDefault = -1;

        /// <summary>
        /// Defines the AutomationProperties.AcceleratorKey attached property.
        /// </summary>
        public static readonly AttachedProperty<string> AcceleratorKeyProperty =
            AvaloniaProperty.RegisterAttached<StyledElement, string>(
                "AcceleratorKey",
                typeof(AutomationProperties));

        /// <summary>
        /// Defines the AutomationProperties.AccessKey attached property
        /// </summary>
        public static readonly AttachedProperty<string> AccessKeyProperty =
            AvaloniaProperty.RegisterAttached<StyledElement, string>(
                "AccessKey",
                typeof(AutomationProperties));

        /// <summary>
        /// Defines the AutomationProperties.AutomationId attached property.
        /// </summary>
        public static readonly AttachedProperty<string> AutomationIdProperty =
            AvaloniaProperty.RegisterAttached<StyledElement, string>(
                "AutomationId",
                typeof(AutomationProperties));

        /// <summary>
        /// Defines the AutomationProperties.HelpText attached property.
        /// </summary>
        public static readonly AttachedProperty<string> HelpTextProperty =
            AvaloniaProperty.RegisterAttached<StyledElement, string>(
                "HelpText",
                typeof(AutomationProperties));

        /// <summary>
        /// Defines the AutomationProperties.IsColumnHeader attached property.
        /// </summary>
        public static readonly AttachedProperty<bool> IsColumnHeaderProperty =
            AvaloniaProperty.RegisterAttached<StyledElement, bool>(
                "IsColumnHeader",
                typeof(AutomationProperties),
                false);

        /// <summary>
        /// Defines the AutomationProperties.IsRequiredForForm attached property.
        /// </summary>
        public static readonly AttachedProperty<bool> IsRequiredForFormProperty =
            AvaloniaProperty.RegisterAttached<StyledElement, bool>(
                "IsRequiredForForm",
                typeof(AutomationProperties),
                false);

        /// <summary>
        /// Defines the AutomationProperties.IsRowHeader attached property.
        /// </summary>
        public static readonly AttachedProperty<bool> IsRowHeaderProperty =
            AvaloniaProperty.RegisterAttached<StyledElement, bool>(
                "IsRowHeader",
                typeof(AutomationProperties),
                false);

        /// <summary>
        /// Defines the AutomationProperties.IsOffscreenBehavior attached property.
        /// </summary>
        public static readonly AttachedProperty<IsOffscreenBehavior> IsOffscreenBehaviorProperty =
            AvaloniaProperty.RegisterAttached<StyledElement, IsOffscreenBehavior>(
                "IsOffscreenBehavior",
                typeof(AutomationProperties),
                IsOffscreenBehavior.Default);

        /// <summary>
        /// Defines the AutomationProperties.ItemStatus attached property.
        /// </summary>
        public static readonly AttachedProperty<string> ItemStatusProperty =
            AvaloniaProperty.RegisterAttached<StyledElement, string>(
                "ItemStatus",
                typeof(AutomationProperties));

        /// <summary>
        /// Defines the AutomationProperties.ItemType attached property.
        /// </summary>
        public static readonly AttachedProperty<string> ItemTypeProperty =
            AvaloniaProperty.RegisterAttached<StyledElement, string>(
                "ItemType",
                typeof(AutomationProperties));

        /// <summary>
        /// Defines the AutomationProperties.LabeledBy attached property.
        /// </summary>
        public static readonly AttachedProperty<IControl> LabeledByProperty =
            AvaloniaProperty.RegisterAttached<StyledElement, IControl>(
                "LabeledBy",
                typeof(AutomationProperties));

        /// <summary>
        /// Defines the AutomationProperties.LiveSetting attached property.
        /// </summary>
        public static readonly AttachedProperty<AutomationLiveSetting> LiveSettingProperty =
            AvaloniaProperty.RegisterAttached<StyledElement, AutomationLiveSetting>(
                "LiveSetting",
                typeof(AutomationProperties),
                AutomationLiveSetting.Off);

        /// <summary>
        /// Defines the AutomationProperties.Name attached attached property.
        /// </summary>
        public static readonly AttachedProperty<string> NameProperty =
            AvaloniaProperty.RegisterAttached<StyledElement, string>(
                "Name",
                typeof(AutomationProperties));

        /// <summary>
        /// Defines the AutomationProperties.PositionInSet attached property.
        /// </summary>
        /// <remarks>
        /// The PositionInSet property describes the ordinal location of the element within a set
        /// of elements which are considered to be siblings. PositionInSet works in coordination
        /// with the SizeOfSet property to describe the ordinal location in the set.
        /// </remarks>
        public static readonly AttachedProperty<int> PositionInSetProperty =
            AvaloniaProperty.RegisterAttached<StyledElement, int>(
                "PositionInSet",
                typeof(AutomationProperties),
                AutomationPositionInSetDefault);

        /// <summary>
        /// Defines the AutomationProperties.SizeOfSet attached property.
        /// </summary>
        /// <remarks>
        /// The SizeOfSet property describes the count of automation elements in a group or set
        /// that are considered to be siblings. SizeOfSet works in coordination with the PositionInSet
        /// property to describe the count of items in the set.
        /// </remarks>
        public static readonly AttachedProperty<int> SizeOfSetProperty =
            AvaloniaProperty.RegisterAttached<StyledElement, int>(
                "SizeOfSet",
                typeof(AutomationProperties),
                AutomationSizeOfSetDefault);

        /// <summary>
        /// Helper for setting AcceleratorKey property on a StyledElement. 
        /// </summary>
        public static void SetAcceleratorKey(StyledElement element, string value)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            element.SetValue(AcceleratorKeyProperty, value);
        }

        /// <summary>
        /// Helper for reading AcceleratorKey property from a StyledElement.
        /// </summary>
        public static string GetAcceleratorKey(StyledElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            return ((string)element.GetValue(AcceleratorKeyProperty));
        }

        /// <summary>
        /// Helper for setting AccessKey property on a StyledElement. 
        /// </summary>
        public static void SetAccessKey(StyledElement element, string value)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            element.SetValue(AccessKeyProperty, value);
        }

        /// <summary>
        /// Helper for reading AccessKey property from a StyledElement.
        /// </summary>
        public static string GetAccessKey(StyledElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            return ((string)element.GetValue(AccessKeyProperty));
        }

        /// <summary>
        /// Helper for setting AutomationId property on a StyledElement. 
        /// </summary>
        public static void SetAutomationId(StyledElement element, string value)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            element.SetValue(AutomationIdProperty, value);
        }

        /// <summary>
        /// Helper for reading AutomationId property from a StyledElement.
        /// </summary>
        public static string GetAutomationId(StyledElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            return element.GetValue(AutomationIdProperty);
        }

        /// <summary>
        /// Helper for setting HelpText property on a StyledElement. 
        /// </summary>
        public static void SetHelpText(StyledElement element, string value)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            element.SetValue(HelpTextProperty, value);
        }

        /// <summary>
        /// Helper for reading HelpText property from a StyledElement.
        /// </summary>
        public static string GetHelpText(StyledElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            return ((string)element.GetValue(HelpTextProperty));
        }

        /// <summary>
        /// Helper for setting IsColumnHeader property on a StyledElement. 
        /// </summary>
        public static void SetIsColumnHeader(StyledElement element, bool value)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            element.SetValue(IsColumnHeaderProperty, value);
        }

        /// <summary>
        /// Helper for reading IsColumnHeader property from a StyledElement.
        /// </summary>
        public static bool GetIsColumnHeader(StyledElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            return ((bool)element.GetValue(IsColumnHeaderProperty));
        }

        /// <summary>
        /// Helper for setting IsRequiredForForm property on a StyledElement. 
        /// </summary>
        public static void SetIsRequiredForForm(StyledElement element, bool value)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            element.SetValue(IsRequiredForFormProperty, value);
        }

        /// <summary>
        /// Helper for reading IsRequiredForForm property from a StyledElement.
        /// </summary>
        public static bool GetIsRequiredForForm(StyledElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            return ((bool)element.GetValue(IsRequiredForFormProperty));
        }

        /// <summary>
        /// Helper for reading IsRowHeader property from a StyledElement.
        /// </summary>
        public static bool GetIsRowHeader(StyledElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            return ((bool)element.GetValue(IsRowHeaderProperty));
        }

        /// <summary>
        /// Helper for setting IsRowHeader property on a StyledElement. 
        /// </summary>
        public static void SetIsRowHeader(StyledElement element, bool value)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            element.SetValue(IsRowHeaderProperty, value);
        }

        /// <summary>
        /// Helper for setting IsOffscreenBehavior property on a StyledElement. 
        /// </summary>
        public static void SetIsOffscreenBehavior(StyledElement element, IsOffscreenBehavior value)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            element.SetValue(IsOffscreenBehaviorProperty, value);
        }

        /// <summary>
        /// Helper for reading IsOffscreenBehavior property from a StyledElement.
        /// </summary>
        public static IsOffscreenBehavior GetIsOffscreenBehavior(StyledElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            return ((IsOffscreenBehavior)element.GetValue(IsOffscreenBehaviorProperty));
        }

        /// <summary>
        /// Helper for setting ItemStatus property on a StyledElement. 
        /// </summary>
        public static void SetItemStatus(StyledElement element, string value)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            element.SetValue(ItemStatusProperty, value);
        }

        /// <summary>
        /// Helper for reading ItemStatus property from a StyledElement.
        /// </summary>
        public static string GetItemStatus(StyledElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            return ((string)element.GetValue(ItemStatusProperty));
        }

        /// <summary>
        /// Helper for setting ItemType property on a StyledElement. 
        /// </summary>
        public static void SetItemType(StyledElement element, string value)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            element.SetValue(ItemTypeProperty, value);
        }

        /// <summary>
        /// Helper for reading ItemType property from a StyledElement.
        /// </summary>
        public static string GetItemType(StyledElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            return ((string)element.GetValue(ItemTypeProperty));
        }

        /// <summary>
        /// Helper for setting LabeledBy property on a StyledElement. 
        /// </summary>
        public static void SetLabeledBy(StyledElement element, IControl value)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            element.SetValue(LabeledByProperty, value);
        }

        /// <summary>
        /// Helper for reading LabeledBy property from a StyledElement.
        /// </summary>
        public static IControl GetLabeledBy(StyledElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            return element.GetValue(LabeledByProperty);
        }

        /// <summary>
        /// Helper for setting LiveSetting property on a StyledElement. 
        /// </summary>
        public static void SetLiveSetting(StyledElement element, AutomationLiveSetting value)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            element.SetValue(LiveSettingProperty, value);
        }

        /// <summary>
        /// Helper for reading LiveSetting property from a StyledElement.
        /// </summary>
        public static AutomationLiveSetting GetLiveSetting(StyledElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            return ((AutomationLiveSetting)element.GetValue(LiveSettingProperty));
        }

        /// <summary>
        /// Helper for setting Name property on a StyledElement. 
        /// </summary>
        public static void SetName(StyledElement element, string value)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            element.SetValue(NameProperty, value);
        }

        /// <summary>
        /// Helper for reading Name property from a StyledElement.
        /// </summary>
        public static string GetName(StyledElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            return ((string)element.GetValue(NameProperty));
        }

        /// <summary>
        /// Helper for setting PositionInSet property on a StyledElement. 
        /// </summary>
        public static void SetPositionInSet(StyledElement element, int value)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            element.SetValue(PositionInSetProperty, value);
        }

        /// <summary>
        /// Helper for reading PositionInSet property from a StyledElement.
        /// </summary>
        public static int GetPositionInSet(StyledElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            return ((int)element.GetValue(PositionInSetProperty));
        }

        /// <summary>
        /// Helper for setting SizeOfSet property on a StyledElement. 
        /// </summary>
        public static void SetSizeOfSet(StyledElement element, int value)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            element.SetValue(SizeOfSetProperty, value);
        }

        /// <summary>
        /// Helper for reading SizeOfSet property from a StyledElement.
        /// </summary>
        public static int GetSizeOfSet(StyledElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            return ((int)element.GetValue(SizeOfSetProperty));
        }
    }
}

